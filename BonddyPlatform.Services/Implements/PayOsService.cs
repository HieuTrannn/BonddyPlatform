using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BonddyPlatform.Services.Implements;

public class PayOsService : IPayOsService
{
    private const string PayOsApiBaseUrl = "https://api-merchant.payos.vn";

    private readonly PayOsOptions _options;
    private readonly ILogger<PayOsService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public PayOsService(
        IOptions<PayOsOptions> options,
        ILogger<PayOsService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _options = options.Value;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Create Payment Link
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<PayOsCreateLinkResult> CreatePaymentLinkAsync(
        long orderCode, int amountVnd, string description,
        string? buyerName = null, string? buyerEmail = null, string? buyerPhone = null)
    {
        EnsureConfigured();

        // PayOS giới hạn description 9 ký tự cho tài khoản không liên kết
        // Tuy nhiên, nếu tài khoản đã liên kết qua PayOS thì không giới hạn
        var safeDescription = description.Length > 25
            ? description[..25]
            : description;

        // Build signature: sorted alphabetically by key
        // amount=$amount&cancelUrl=$cancelUrl&description=$description&orderCode=$orderCode&returnUrl=$returnUrl
        var signatureData = $"amount={amountVnd}&cancelUrl={_options.CancelUrl}&description={safeDescription}&orderCode={orderCode}&returnUrl={_options.ReturnUrl}";
        var signature = ComputeHmacSha256(signatureData, _options.ChecksumKey);

        var requestBody = new Dictionary<string, object>
        {
            ["orderCode"] = orderCode,
            ["amount"] = amountVnd,
            ["description"] = safeDescription,
            ["cancelUrl"] = _options.CancelUrl,
            ["returnUrl"] = _options.ReturnUrl,
            ["signature"] = signature
        };

        if (!string.IsNullOrWhiteSpace(buyerName))
            requestBody["buyerName"] = buyerName;
        if (!string.IsNullOrWhiteSpace(buyerEmail))
            requestBody["buyerEmail"] = buyerEmail;
        if (!string.IsNullOrWhiteSpace(buyerPhone))
            requestBody["buyerPhone"] = buyerPhone;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
        client.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync($"{PayOsApiBaseUrl}/v2/payment-requests", jsonContent);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("PayOS create payment link failed: {StatusCode} - {Body}",
                response.StatusCode, body);
            throw new InvalidOperationException($"Failed to create PayOS payment link. Status: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        var code = root.GetProperty("code").GetString();
        if (code != "00")
        {
            var desc = root.TryGetProperty("desc", out var descEl) ? descEl.GetString() : "Unknown error";
            _logger.LogError("PayOS create payment link error: code={Code}, desc={Desc}", code, desc);
            throw new InvalidOperationException($"PayOS error: {desc}");
        }

        var data = root.GetProperty("data");

        return new PayOsCreateLinkResult
        {
            OrderCode = orderCode,
            Amount = amountVnd,
            CheckoutUrl = data.GetProperty("checkoutUrl").GetString() ?? string.Empty,
            QrCode = data.GetProperty("qrCode").GetString() ?? string.Empty,
            PaymentLinkId = data.GetProperty("paymentLinkId").GetString() ?? string.Empty
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Get Payment Info
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<PayOsPaymentInfo?> GetPaymentInfoAsync(long orderCode)
    {
        EnsureConfigured();

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
        client.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);

        var response = await client.GetAsync($"{PayOsApiBaseUrl}/v2/payment-requests/{orderCode}");
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayOS get payment info failed: {StatusCode} - {Body}",
                response.StatusCode, body);
            return null;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var code = root.GetProperty("code").GetString();
        if (code != "00") return null;

        var data = root.GetProperty("data");
        return new PayOsPaymentInfo
        {
            Id = data.GetProperty("id").GetString() ?? string.Empty,
            OrderCode = data.GetProperty("orderCode").GetInt64(),
            Amount = data.GetProperty("amount").GetInt32(),
            AmountPaid = data.GetProperty("amountPaid").GetInt32(),
            AmountRemaining = data.GetProperty("amountRemaining").GetInt32(),
            Status = data.GetProperty("status").GetString() ?? "UNKNOWN"
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Cancel Payment Link
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<bool> CancelPaymentLinkAsync(long orderCode, string? reason = null)
    {
        EnsureConfigured();

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-client-id", _options.ClientId);
        client.DefaultRequestHeaders.Add("x-api-key", _options.ApiKey);

        var requestBody = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(reason))
            requestBody["cancellationReason"] = reason;

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync(
            $"{PayOsApiBaseUrl}/v2/payment-requests/{orderCode}/cancel", jsonContent);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("PayOS cancel payment link failed: {StatusCode} - {Body}",
                response.StatusCode, body);
            return false;
        }

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("code").GetString() == "00";
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Webhook Signature Verification
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verify webhook data using HMAC_SHA256.
    /// Data format: keys sorted alphabetically → key1=value1&key2=value2...
    /// </summary>
    public bool VerifyWebhookSignature(Dictionary<string, object> data, string signature)
    {
        // Sort keys alphabetically
        var sortedKeys = data.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

        var parts = new List<string>();
        foreach (var key in sortedKeys)
        {
            var value = data[key];
            string valueStr;

            if (value is null || value.ToString() == "null" || value.ToString() == "undefined")
            {
                valueStr = "";
            }
            else if (value is JsonElement jsonElement)
            {
                valueStr = jsonElement.ValueKind switch
                {
                    JsonValueKind.String => jsonElement.GetString() ?? "",
                    JsonValueKind.Number => jsonElement.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "",
                    _ => jsonElement.GetRawText()
                };
            }
            else
            {
                valueStr = value.ToString() ?? "";
            }

            parts.Add($"{key}={valueStr}");
        }

        var dataString = string.Join("&", parts);
        var computedSignature = ComputeHmacSha256(dataString, _options.ChecksumKey);

        var isValid = string.Equals(computedSignature, signature, StringComparison.OrdinalIgnoreCase);
        if (!isValid)
        {
            _logger.LogWarning(
                "PayOS webhook signature mismatch. Data: {DataString}, Expected: {Expected}, Computed: {Computed}",
                dataString, signature, computedSignature);
        }

        return isValid;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ═══════════════════════════════════════════════════════════════════════════

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.ClientId) ||
            string.IsNullOrWhiteSpace(_options.ApiKey) ||
            string.IsNullOrWhiteSpace(_options.ChecksumKey))
        {
            throw new InvalidOperationException("PayOS is not configured. Missing ClientId/ApiKey/ChecksumKey.");
        }
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);

        using var hmac = new HMACSHA256(keyBytes);
        var hash = hmac.ComputeHash(dataBytes);

        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    /// <summary>
    /// Generate a unique order code for PayOS (must be positive int64, unique).
    /// Uses timestamp + random suffix to avoid collisions.
    /// </summary>
    public static long GenerateOrderCode()
    {
        // Use Unix timestamp in seconds (fits in int32 until 2038)
        // Plus random 4-digit suffix for uniqueness
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var random = Random.Shared.Next(1000, 9999);
        // Result: e.g., 17120123451234 (14 digits, well within int64 range)
        return timestamp * 10000 + random;
    }
}

