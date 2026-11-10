using System.Net;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.Implements;
using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using BonddyPlatform.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace BonddyPlatform.Tests;

/// <summary>
/// PayPal webhooks are verified by calling PayPal's verify-webhook-signature API.
/// These tests fake that API and check that nothing changes unless PayPal says SUCCESS.
/// </summary>
public class PayPalWebhookTests
{
    private const string VerifyPath = "/v1/notifications/verify-webhook-signature";
    private const string TokenPath = "/v1/oauth2/token";

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IPaymentRepository _payments = Substitute.For<IPaymentRepository>();
    private readonly IBookingRepository _bookings = Substitute.For<IBookingRepository>();
    private readonly StubHttpMessageHandler _paypal = new();

    private readonly Booking _booking = new() { Id = Guid.NewGuid(), Status = BookingStatus.PendingPayment };
    private readonly Payment _payment;

    public PayPalWebhookTests()
    {
        _payment = new Payment { Id = Guid.NewGuid(), BookingId = _booking.Id, Status = PaymentStatus.Pending };
        _uow.Payments.Returns(_payments);
        _uow.Bookings.Returns(_bookings);
        _payments.GetByBookingIdAsync(_booking.Id).Returns(_payment);
        _bookings.GetByIdAsync(_booking.Id).Returns(_booking);
        _paypal.On(HttpMethod.Post, TokenPath, """{"access_token":"fake-token"}""");
    }

    private PaymentService CreateService(string webhookId = "WH-TEST") => new(
        _uow,
        MsOptions.Create(new PayPalOptions
        {
            BaseUrl = "https://paypal.test",
            ClientId = "client",
            Secret = "secret",
            WebhookId = webhookId
        }),
        NullLogger<PaymentService>.Instance,
        Substitute.For<IPayOsService>(),
        new StubHttpClientFactory(_paypal));

    private static Dictionary<string, string> SignedHeaders() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["PAYPAL-TRANSMISSION-ID"] = "tx-1",
        ["PAYPAL-TRANSMISSION-TIME"] = "2026-01-01T00:00:00Z",
        ["PAYPAL-TRANSMISSION-SIG"] = "sig",
        ["PAYPAL-CERT-URL"] = "https://api.paypal.com/cert.pem",
        ["PAYPAL-AUTH-ALGO"] = "SHA256withRSA"
    };

    private string Event(string type = "CHECKOUT.ORDER.COMPLETED") =>
        $$$"""{"event_type":"{{{type}}}","resource":{"id":"O-123","purchase_units":[{"custom_id":"{{{_booking.Id}}}"}]}}""";

    [Fact]
    public async Task Verified_completed_event_holds_payment_and_confirms_booking()
    {
        _paypal.On(HttpMethod.Post, VerifyPath, """{"verification_status":"SUCCESS"}""");

        var result = await CreateService().HandlePayPalWebhookAsync(Event(), SignedHeaders());

        Assert.True(result.Processed);
        Assert.Equal(PaymentStatus.Held, _payment.Status);
        Assert.NotNull(_payment.HeldAt);
        Assert.Equal("O-123", _payment.ExternalRef);
        Assert.Equal(BookingStatus.Confirmed, _booking.Status);
        await _uow.Received(1).SaveChangesAsync();

        // Our webhook id must be sent to PayPal so it verifies against the right subscription.
        var verifyCall = Assert.Single(_paypal.Requests, r => r.Request.RequestUri!.AbsolutePath == VerifyPath);
        Assert.Contains("\"webhook_id\":\"WH-TEST\"", verifyCall.Body);
        Assert.Equal("Bearer", verifyCall.Request.Headers.Authorization?.Scheme);
    }

    [Fact]
    public async Task Failed_verification_changes_nothing()
    {
        _paypal.On(HttpMethod.Post, VerifyPath, """{"verification_status":"FAILURE"}""");

        var result = await CreateService().HandlePayPalWebhookAsync(Event(), SignedHeaders());

        Assert.False(result.Processed);
        Assert.Equal("Invalid PayPal webhook signature.", result.Message);
        await AssertUntouchedAsync();
    }

    [Fact]
    public async Task Verification_endpoint_error_is_treated_as_invalid()
    {
        _paypal.On(HttpMethod.Post, VerifyPath, """{"name":"INTERNAL_SERVICE_ERROR"}""", HttpStatusCode.InternalServerError);

        var result = await CreateService().HandlePayPalWebhookAsync(Event(), SignedHeaders());

        Assert.False(result.Processed);
        await AssertUntouchedAsync();
    }

    [Theory]
    [InlineData("PAYPAL-TRANSMISSION-ID")]
    [InlineData("PAYPAL-TRANSMISSION-TIME")]
    [InlineData("PAYPAL-TRANSMISSION-SIG")]
    [InlineData("PAYPAL-CERT-URL")]
    [InlineData("PAYPAL-AUTH-ALGO")]
    public async Task Missing_signature_header_is_rejected_without_calling_PayPal(string header)
    {
        var headers = SignedHeaders();
        headers.Remove(header);

        var result = await CreateService().HandlePayPalWebhookAsync(Event(), headers);

        Assert.False(result.Processed);
        Assert.Empty(_paypal.Requests);
        await AssertUntouchedAsync();
    }

    [Fact]
    public async Task Missing_webhook_id_configuration_rejects_every_event()
    {
        _paypal.On(HttpMethod.Post, VerifyPath, """{"verification_status":"SUCCESS"}""");

        var result = await CreateService(webhookId: "").HandlePayPalWebhookAsync(Event(), SignedHeaders());

        Assert.False(result.Processed);
        Assert.Empty(_paypal.Requests);
        await AssertUntouchedAsync();
    }

    [Fact]
    public async Task Verified_refund_event_marks_payment_refunded_and_leaves_booking_alone()
    {
        _paypal.On(HttpMethod.Post, VerifyPath, """{"verification_status":"SUCCESS"}""");
        _payment.Status = PaymentStatus.Held;

        var result = await CreateService().HandlePayPalWebhookAsync(Event("PAYMENT.CAPTURE.REFUNDED"), SignedHeaders());

        Assert.True(result.Processed);
        Assert.Equal(PaymentStatus.Refunded, _payment.Status);
        Assert.NotNull(_payment.RefundedAt);
        Assert.Equal(BookingStatus.PendingPayment, _booking.Status);
    }

    private async Task AssertUntouchedAsync()
    {
        Assert.Equal(PaymentStatus.Pending, _payment.Status);
        Assert.Equal(BookingStatus.PendingPayment, _booking.Status);
        await _uow.DidNotReceive().SaveChangesAsync();
    }
}
