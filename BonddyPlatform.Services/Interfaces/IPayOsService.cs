namespace BonddyPlatform.Services.Interfaces;

/// <summary>
/// Low-level service to interact with payOS API.
/// Handles payment link creation, signature verification, and webhook processing.
/// </summary>
public interface IPayOsService
{
    /// <summary>
    /// Tạo link thanh toán PayOS. Amount phải là VND (int).
    /// </summary>
    Task<PayOsCreateLinkResult> CreatePaymentLinkAsync(
        long orderCode, int amountVnd, string description,
        string? buyerName = null, string? buyerEmail = null, string? buyerPhone = null);

    /// <summary>Lấy thông tin link thanh toán từ PayOS.</summary>
    Task<PayOsPaymentInfo?> GetPaymentInfoAsync(long orderCode);

    /// <summary>Hủy link thanh toán.</summary>
    Task<bool> CancelPaymentLinkAsync(long orderCode, string? reason = null);

    /// <summary>Verify webhook signature bằng HMAC_SHA256.</summary>
    bool VerifyWebhookSignature(Dictionary<string, object> data, string signature);
}

/// <summary>Kết quả tạo link thanh toán PayOS.</summary>
public class PayOsCreateLinkResult
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string QrCode { get; set; } = string.Empty;
    public string PaymentLinkId { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public int Amount { get; set; }
}

/// <summary>Thông tin link thanh toán từ PayOS.</summary>
public class PayOsPaymentInfo
{
    public string Id { get; set; } = string.Empty;
    public long OrderCode { get; set; }
    public int Amount { get; set; }
    public int AmountPaid { get; set; }
    public int AmountRemaining { get; set; }
    public string Status { get; set; } = string.Empty; // PENDING, PAID, CANCELLED, EXPIRED
}
