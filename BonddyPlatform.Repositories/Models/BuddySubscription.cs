namespace BonddyPlatform.Repositories.Models;

/// <summary>
/// Records a Buddy's subscription to a ServicePackage.
/// A buddy can only have one active subscription at a time.
/// </summary>
public class BuddySubscription
{
    public Guid Id { get; set; }

    /// <summary>FK → Buddy</summary>
    public Guid BuddyId { get; set; }
    public Buddy Buddy { get; set; } = null!;

    /// <summary>FK → ServicePackage đang dùng</summary>
    public Guid ServicePackageId { get; set; }
    public ServicePackage ServicePackage { get; set; } = null!;

    /// <summary>When the subscription period starts</summary>
    public DateTime StartDate { get; set; }

    /// <summary>When the subscription period ends</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Current status of this subscription</summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.PendingPayment;

    // ─── Payment Info ────────────────────────────────────────────────────────

    /// <summary>Payment method used for this subscription</summary>
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Wallet;

    /// <summary>Amount paid (snapshot from package price at time of purchase)</summary>
    public decimal AmountPaid { get; set; }

    /// <summary>Currency of the payment</summary>
    public string Currency { get; set; } = "VND";

    /// <summary>External payment reference (e.g. PayPal order ID)</summary>
    public string? ExternalPaymentRef { get; set; }

    /// <summary>Mã đơn hàng PayOS (int64) — dùng để map webhook callback.</summary>
    public long? PayOsOrderCode { get; set; }

    /// <summary>When payment was confirmed</summary>
    public DateTime? PaidAt { get; set; }

    // ─── Downgrade Scheduling ────────────────────────────────────────────────

    /// <summary>
    /// Nếu buddy downgrade: gói hiện tại giữ đến hết EndDate, 
    /// sau đó tự động chuyển sang gói này. Null = không có downgrade scheduled.
    /// </summary>
    public Guid? DowngradeToPackageId { get; set; }
    public ServicePackage? DowngradeToPackage { get; set; }

    /// <summary>
    /// Buddy đã yêu cầu hủy — giữ active đến EndDate, không gia hạn.
    /// </summary>
    public bool IsCancelledAtEndOfCycle { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum SubscriptionStatus
{
    /// <summary>Chờ thanh toán — subscription chưa có hiệu lực</summary>
    PendingPayment = 0,

    /// <summary>Đã thanh toán, subscription đang hoạt động</summary>
    Active = 1,

    /// <summary>Hết hạn</summary>
    Expired = 2,

    /// <summary>Đã bị hủy (cancel/upgrade thay thế)</summary>
    Cancelled = 3
}
