using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.PaymentDtos;
using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace BonddyPlatform.Services.Implements;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly PayPalOptions _payPalOptions;
    private readonly ILogger<PaymentService> _logger;

    // Escrow state machine
    private static readonly Dictionary<PaymentStatus, HashSet<PaymentStatus>> _allowedTransitions = new()
    {
        [PaymentStatus.Pending]  = new() { PaymentStatus.Held, PaymentStatus.Refunded },
        [PaymentStatus.Held]     = new() { PaymentStatus.Released, PaymentStatus.Refunded },
        [PaymentStatus.Released] = new(),
        [PaymentStatus.Refunded] = new()
    };

    public PaymentService(
        IUnitOfWork uow,
        IOptions<PayPalOptions> payPalOptions,
        ILogger<PaymentService> logger)
    {
        _uow = uow;
        _payPalOptions = payPalOptions.Value;
        _logger = logger;
    }

    public async Task<PaymentResponseDto> CreateAsync(PaymentCreateRequestDto dto)
    {
        var existing = await _uow.Payments.GetByBookingIdAsync(dto.BookingId);
        if (existing is not null)
            throw new InvalidOperationException("A payment already exists for this booking.");

        var booking = await _uow.Bookings.GetByIdAsync(dto.BookingId)
            ?? throw new KeyNotFoundException($"Booking {dto.BookingId} not found.");

        var payment = new Payment
        {
            BookingId = dto.BookingId,
            Amount = booking.TotalAmount,
            Method = dto.Method,
            Status = PaymentStatus.Pending,
            ExternalRef = dto.ExternalRef,
            Note = dto.Note,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Payments.AddAsync(payment);
        await _uow.SaveChangesAsync();
        return ToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetByBookingIdAsync(Guid bookingId)
    {
        var payment = await _uow.Payments.GetByBookingIdAsync(bookingId);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<PaymentResponseDto?> GetByIdAsync(Guid id)
    {
        var payment = await _uow.Payments.GetByIdAsync(id);
        return payment is null ? null : ToDto(payment);
    }

    public async Task<PaymentResponseDto?> TransitionAsync(Guid id, PaymentTransitionRequestDto dto)
    {
        var payment = await _uow.Payments.GetByIdAsync(id);
        if (payment is null) return null;

        if (!_allowedTransitions.TryGetValue(payment.Status, out var allowed) || !allowed.Contains(dto.Status))
            throw new InvalidOperationException(
                $"Cannot transition payment from {payment.Status} to {dto.Status}.");

        payment.Status = dto.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        if (dto.ExternalRef is not null) payment.ExternalRef = dto.ExternalRef;
        if (dto.Note is not null) payment.Note = dto.Note;

        switch (dto.Status)
        {
            case PaymentStatus.Held:
                payment.HeldAt = DateTime.UtcNow;
                break;
            case PaymentStatus.Released:
                payment.ReleasedAt = DateTime.UtcNow;
                break;
            case PaymentStatus.Refunded:
                payment.RefundedAt = DateTime.UtcNow;
                break;
        }

        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync();
        return ToDto(payment);
    }

    public async Task<PayPalCreateOrderResponseDto> CreatePayPalOrderAsync(Guid bookingId, Guid requesterUserId)
    {
        EnsurePayPalConfigured();

        var booking = await _uow.Bookings.GetByIdAsync(bookingId)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.TravelerUserId != requesterUserId)
            throw new UnauthorizedAccessException("You are not allowed to pay this booking.");

        if (booking.Status != BookingStatus.PendingPayment)
            throw new InvalidOperationException($"Booking is not ready for payment. Current status: {booking.Status}");

        var payment = await _uow.Payments.GetByBookingIdAsync(bookingId);
        if (payment == null)
        {
            payment = new Payment
            {
                BookingId = bookingId,
                Amount = booking.TotalAmount,
                Method = PaymentMethod.Wallet,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await _uow.Payments.AddAsync(payment);
            await _uow.SaveChangesAsync();
        }

        if (payment.Status is PaymentStatus.Held or PaymentStatus.Released)
            throw new InvalidOperationException("Booking is already paid.");

        var accessToken = await GetPayPalAccessTokenAsync();
        using var client = new HttpClient();
        client.BaseAddress = new Uri(_payPalOptions.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var orderRequest = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = payment.Id.ToString(),
                    custom_id = bookingId.ToString(),
                    amount = new
                    {
                        currency_code = booking.Currency,
                        value = booking.TotalAmount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                    }
                }
            },
            application_context = new
            {
                return_url = _payPalOptions.ReturnUrl,
                cancel_url = _payPalOptions.CancelUrl
            }
        };

        var response = await client.PostAsync(
            "/v2/checkout/orders",
            new StringContent(JsonSerializer.Serialize(orderRequest), Encoding.UTF8, "application/json"));

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal create order failed: {StatusCode} - {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to create PayPal order.");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var orderId = root.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("PayPal response missing order id.");

        string? approveUrl = null;
        if (root.TryGetProperty("links", out var linksEl) && linksEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var link in linksEl.EnumerateArray())
            {
                if (string.Equals(link.GetProperty("rel").GetString(), "approve", StringComparison.OrdinalIgnoreCase))
                {
                    approveUrl = link.GetProperty("href").GetString();
                    break;
                }
            }
        }

        payment.ExternalRef = orderId;
        payment.UpdatedAt = DateTime.UtcNow;
        _uow.Payments.Update(payment);
        await _uow.SaveChangesAsync();

        return new PayPalCreateOrderResponseDto
        {
            PaymentId = payment.Id,
            BookingId = booking.Id,
            OrderId = orderId,
            ApproveUrl = approveUrl,
            Currency = booking.Currency,
            Amount = booking.TotalAmount
        };
    }

    public async Task<PaymentResponseDto> CapturePayPalOrderAsync(Guid paymentId, string orderId, Guid requesterUserId)
    {
        EnsurePayPalConfigured();
        if (string.IsNullOrWhiteSpace(orderId))
            throw new InvalidOperationException("OrderId is required.");

        // FE callback URL may contain token + payer id in one string.
        // Keep only the PayPal order token part before '&'.
        var normalizedOrderId = orderId.Trim();
        var ampIndex = normalizedOrderId.IndexOf('&');
        if (ampIndex > 0)
            normalizedOrderId = normalizedOrderId[..ampIndex];

        var payment = await _uow.Payments.GetByIdAsync(paymentId)
            ?? throw new KeyNotFoundException("Payment not found.");
        var booking = await _uow.Bookings.GetByIdAsync(payment.BookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.TravelerUserId != requesterUserId)
            throw new UnauthorizedAccessException("You are not allowed to capture this payment.");

        if (booking.Status != BookingStatus.PendingPayment && payment.Status == PaymentStatus.Pending)
            throw new InvalidOperationException($"Booking is not in payable state. Current status: {booking.Status}");

        if (payment.Status is PaymentStatus.Held or PaymentStatus.Released)
            return ToDto(payment);

        var accessToken = await GetPayPalAccessTokenAsync();
        using var client = new HttpClient();
        client.BaseAddress = new Uri(_payPalOptions.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.PostAsync($"/v2/checkout/orders/{normalizedOrderId}/capture", null);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal capture failed: {StatusCode} - {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to capture PayPal order.");
        }

        using var doc = JsonDocument.Parse(body);
        var status = doc.RootElement.TryGetProperty("status", out var statusEl)
            ? statusEl.GetString()
            : null;

        if (!string.Equals(status, "COMPLETED", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"PayPal capture not completed. Status: {status ?? "UNKNOWN"}");

        payment.Status = PaymentStatus.Held;
        payment.HeldAt = DateTime.UtcNow;
        payment.ExternalRef = normalizedOrderId;
        payment.Method = PaymentMethod.Wallet;
        payment.UpdatedAt = DateTime.UtcNow;
        _uow.Payments.Update(payment);

        if (booking.Status == BookingStatus.PendingPayment)
        {
            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAt = DateTime.UtcNow;
            _uow.Bookings.Update(booking);
        }

        await _uow.SaveChangesAsync();
        return ToDto(payment);
    }

    public async Task<PayPalWebhookResultDto> HandlePayPalWebhookAsync(string rawBody, IDictionary<string, string> headers)
    {
        EnsurePayPalConfigured();

        if (!await VerifyPayPalWebhookSignatureAsync(rawBody, headers))
            return new PayPalWebhookResultDto { Processed = false, Message = "Invalid PayPal webhook signature." };

        using var doc = JsonDocument.Parse(rawBody);
        var root = doc.RootElement;
        var eventType = root.TryGetProperty("event_type", out var eventTypeEl)
            ? eventTypeEl.GetString()
            : null;

        var resource = root.TryGetProperty("resource", out var resourceEl) ? resourceEl : default;
        var bookingId = TryExtractBookingId(resource);
        var orderId = TryExtractOrderId(resource);

        Payment? payment = null;
        if (bookingId.HasValue)
        {
            payment = await _uow.Payments.GetByBookingIdAsync(bookingId.Value);
        }
        if (payment == null && !string.IsNullOrWhiteSpace(orderId))
        {
            payment = await _uow.Payments
                .GetQueryable()
                .FirstOrDefaultAsync(p => p.ExternalRef == orderId);
        }

        if (payment == null)
            return new PayPalWebhookResultDto { Processed = false, Message = "No matching payment found." };

        var booking = await _uow.Bookings.GetByIdAsync(payment.BookingId);
        if (booking == null)
            return new PayPalWebhookResultDto { Processed = false, Message = "Booking not found for payment." };

        switch (eventType)
        {
            case "CHECKOUT.ORDER.COMPLETED":
            case "PAYMENT.CAPTURE.COMPLETED":
                if (payment.Status == PaymentStatus.Pending)
                {
                    payment.Status = PaymentStatus.Held;
                    payment.HeldAt = DateTime.UtcNow;
                }
                if (!string.IsNullOrWhiteSpace(orderId))
                    payment.ExternalRef = orderId;
                payment.UpdatedAt = DateTime.UtcNow;
                _uow.Payments.Update(payment);

                if (booking.Status == BookingStatus.PendingPayment)
                {
                    booking.Status = BookingStatus.Confirmed;
                    booking.UpdatedAt = DateTime.UtcNow;
                    _uow.Bookings.Update(booking);
                }

                await _uow.SaveChangesAsync();
                return new PayPalWebhookResultDto { Processed = true, Message = "Payment marked as held." };

            case "PAYMENT.CAPTURE.REFUNDED":
            case "PAYMENT.CAPTURE.DENIED":
                if (payment.Status != PaymentStatus.Refunded)
                {
                    payment.Status = PaymentStatus.Refunded;
                    payment.RefundedAt = DateTime.UtcNow;
                    payment.UpdatedAt = DateTime.UtcNow;
                    _uow.Payments.Update(payment);
                    await _uow.SaveChangesAsync();
                }
                return new PayPalWebhookResultDto { Processed = true, Message = "Payment marked as refunded." };

            default:
                return new PayPalWebhookResultDto { Processed = false, Message = $"Event ignored: {eventType}" };
        }
    }

    private void EnsurePayPalConfigured()
    {
        if (string.IsNullOrWhiteSpace(_payPalOptions.BaseUrl) ||
            string.IsNullOrWhiteSpace(_payPalOptions.ClientId) ||
            string.IsNullOrWhiteSpace(_payPalOptions.Secret))
        {
            throw new InvalidOperationException("PayPal is not configured. Missing ClientId/Secret/BaseUrl.");
        }
    }

    private async Task<string> GetPayPalAccessTokenAsync()
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri(_payPalOptions.BaseUrl);
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_payPalOptions.ClientId}:{_payPalOptions.Secret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials"
        });

        var response = await client.PostAsync("/v1/oauth2/token", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal token request failed: {StatusCode} - {Body}", response.StatusCode, body);
            throw new InvalidOperationException("Failed to get PayPal access token.");
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("PayPal token response missing access_token.");
    }

    private async Task<bool> VerifyPayPalWebhookSignatureAsync(string rawBody, IDictionary<string, string> headers)
    {
        if (string.IsNullOrWhiteSpace(_payPalOptions.WebhookId))
        {
            _logger.LogWarning("PAYPAL_WEBHOOK_ID is empty. Skipping webhook signature verification.");
            return false;
        }

        string? transmissionId = GetHeader(headers, "PAYPAL-TRANSMISSION-ID");
        string? transmissionTime = GetHeader(headers, "PAYPAL-TRANSMISSION-TIME");
        string? transmissionSig = GetHeader(headers, "PAYPAL-TRANSMISSION-SIG");
        string? certUrl = GetHeader(headers, "PAYPAL-CERT-URL");
        string? authAlgo = GetHeader(headers, "PAYPAL-AUTH-ALGO");

        if (string.IsNullOrWhiteSpace(transmissionId) ||
            string.IsNullOrWhiteSpace(transmissionTime) ||
            string.IsNullOrWhiteSpace(transmissionSig) ||
            string.IsNullOrWhiteSpace(certUrl) ||
            string.IsNullOrWhiteSpace(authAlgo))
        {
            return false;
        }

        var accessToken = await GetPayPalAccessTokenAsync();
        using var client = new HttpClient();
        client.BaseAddress = new Uri(_payPalOptions.BaseUrl);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var verifyRequest = new
        {
            transmission_id = transmissionId,
            transmission_time = transmissionTime,
            cert_url = certUrl,
            auth_algo = authAlgo,
            transmission_sig = transmissionSig,
            webhook_id = _payPalOptions.WebhookId,
            webhook_event = JsonSerializer.Deserialize<object>(rawBody)
        };

        var response = await client.PostAsync(
            "/v1/notifications/verify-webhook-signature",
            new StringContent(JsonSerializer.Serialize(verifyRequest), Encoding.UTF8, "application/json"));

        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayPal webhook verification failed: {StatusCode} - {Body}", response.StatusCode, body);
            return false;
        }

        using var doc = JsonDocument.Parse(body);
        var verificationStatus = doc.RootElement.TryGetProperty("verification_status", out var statusEl)
            ? statusEl.GetString()
            : null;

        return string.Equals(verificationStatus, "SUCCESS", StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetHeader(IDictionary<string, string> headers, string key)
        => headers.TryGetValue(key, out var value) ? value : null;

    private static Guid? TryExtractBookingId(JsonElement resource)
    {
        if (resource.ValueKind == JsonValueKind.Undefined) return null;

        if (resource.TryGetProperty("purchase_units", out var purchaseUnits) &&
            purchaseUnits.ValueKind == JsonValueKind.Array &&
            purchaseUnits.GetArrayLength() > 0)
        {
            var first = purchaseUnits[0];
            if (first.TryGetProperty("custom_id", out var customIdEl))
            {
                var customId = customIdEl.GetString();
                if (Guid.TryParse(customId, out var id)) return id;
            }
        }

        if (resource.TryGetProperty("custom_id", out var topLevelCustomIdEl))
        {
            var customId = topLevelCustomIdEl.GetString();
            if (Guid.TryParse(customId, out var id)) return id;
        }

        return null;
    }

    private static string? TryExtractOrderId(JsonElement resource)
    {
        if (resource.ValueKind == JsonValueKind.Undefined) return null;

        if (resource.TryGetProperty("id", out var idEl))
        {
            var id = idEl.GetString();
            if (!string.IsNullOrWhiteSpace(id) && id.StartsWith("O-", StringComparison.OrdinalIgnoreCase))
                return id;
        }

        if (resource.TryGetProperty("supplementary_data", out var supplementaryData) &&
            supplementaryData.TryGetProperty("related_ids", out var relatedIds) &&
            relatedIds.TryGetProperty("order_id", out var orderIdEl))
        {
            return orderIdEl.GetString();
        }

        return null;
    }

    private static PaymentResponseDto ToDto(Payment p) => new()
    {
        Id = p.Id,
        BookingId = p.BookingId,
        Amount = p.Amount,
        Method = p.Method,
        Status = p.Status,
        ExternalRef = p.ExternalRef,
        HeldAt = p.HeldAt,
        ReleasedAt = p.ReleasedAt,
        RefundedAt = p.RefundedAt,
        Note = p.Note,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt
    };
}
