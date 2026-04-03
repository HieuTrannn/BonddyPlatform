namespace BonddyPlatform.Services.Options;

public class PayOsOptions
{
    public const string SectionName = "PayOS";

    public string ClientId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ChecksumKey { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = "https://bonddy.vn/payment/payos/success";
    public string CancelUrl { get; set; } = "https://bonddy.vn/payment/payos/cancel";
}
