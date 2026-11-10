using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Services.Implements;
using BonddyPlatform.Services.Options;
using BonddyPlatform.Tests.TestUtils;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace BonddyPlatform.Tests;

public class PayOsWebhookSignatureTests
{
    private const string ChecksumKey = "test-checksum-key-not-a-real-secret";

    private const string DataJson =
        """{"orderCode":123456,"amount":2000,"description":"BONDDY Gold","code":"00","desc":"success","counterAccountName":null}""";

    // PayOS spec: keys sorted alphabetically, "key=value" joined by '&', null -> empty string.
    private const string Canonical =
        "amount=2000&code=00&counterAccountName=&desc=success&description=BONDDY Gold&orderCode=123456";

    private static PayOsService CreateService(string checksumKey = ChecksumKey) => new(
        MsOptions.Create(new PayOsOptions { ClientId = "client", ApiKey = "api", ChecksumKey = checksumKey }),
        NullLogger<PayOsService>.Instance,
        new StubHttpClientFactory(new StubHttpMessageHandler()));

    /// <summary>Independent reference implementation: HMAC-SHA256 over the canonical string, lowercase hex.</summary>
    private static string Sign(string canonical, string key = ChecksumKey) =>
        Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(canonical)))
            .ToLowerInvariant();

    /// <summary>Webhook "data" exactly as PaymentService builds it (values are JsonElements).</summary>
    private static Dictionary<string, object> ParseData(string json) =>
        JsonDocument.Parse(json).RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => (object)p.Value.Clone());

    [Fact]
    public void Valid_signature_is_accepted()
    {
        Assert.True(CreateService().VerifyWebhookSignature(ParseData(DataJson), Sign(Canonical)));
    }

    [Fact]
    public void Signature_comparison_is_case_insensitive()
    {
        Assert.True(CreateService().VerifyWebhookSignature(ParseData(DataJson), Sign(Canonical).ToUpperInvariant()));
    }

    [Fact]
    public void Key_order_in_payload_does_not_matter()
    {
        const string reordered =
            """{"desc":"success","code":"00","counterAccountName":null,"amount":2000,"description":"BONDDY Gold","orderCode":123456}""";

        Assert.True(CreateService().VerifyWebhookSignature(ParseData(reordered), Sign(Canonical)));
    }

    [Fact]
    public void Tampered_amount_is_rejected()
    {
        var tampered = DataJson.Replace("\"amount\":2000", "\"amount\":2000000");

        Assert.False(CreateService().VerifyWebhookSignature(ParseData(tampered), Sign(Canonical)));
    }

    [Fact]
    public void Signature_made_with_another_key_is_rejected()
    {
        Assert.False(CreateService().VerifyWebhookSignature(ParseData(DataJson), Sign(Canonical, "attacker-key")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_checksum_key_rejects_a_signature_forged_with_an_empty_key(string configuredKey)
    {
        // Regression: HMAC with an empty key is trivially computable by anyone.
        var forged = Sign(Canonical, key: "");

        Assert.False(CreateService(configuredKey).VerifyWebhookSignature(ParseData(DataJson), forged));
    }

    [Theory]
    [InlineData("")]
    [InlineData("deadbeef")]
    public void Empty_or_truncated_signature_is_rejected(string signature)
    {
        Assert.False(CreateService().VerifyWebhookSignature(ParseData(DataJson), signature));
    }

    [Fact]
    public async Task Webhook_with_invalid_signature_never_touches_the_database()
    {
        var uow = Substitute.For<IUnitOfWork>();
        var paymentService = new PaymentService(
            uow,
            MsOptions.Create(new PayPalOptions()),
            NullLogger<PaymentService>.Instance,
            CreateService(),
            new StubHttpClientFactory(new StubHttpMessageHandler()));

        var body = $$"""
            {"code":"00","desc":"success","success":true,"data":{{DataJson}},"signature":"{{Sign(Canonical, "attacker-key")}}"}
            """;

        var result = await paymentService.HandlePayOsWebhookAsync(body);

        Assert.False(result.Processed);
        Assert.Equal("Invalid PayOS webhook signature.", result.Message);
        Assert.Empty(uow.ReceivedCalls());
    }
}
