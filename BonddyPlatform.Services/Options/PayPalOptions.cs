namespace BonddyPlatform.Services.Options;

public class PayPalOptions
{
    public const string SectionName = "PayPal";

    public string BaseUrl { get; set; } = "https://api-m.paypal.com";
    public string ClientId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
    public string WebhookId { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;
}
