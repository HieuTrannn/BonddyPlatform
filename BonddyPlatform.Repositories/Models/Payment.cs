using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class Payment
{
    public Guid Id { get; set; }

    /// <summary>FK → Booking (null nếu payment dành cho subscription)</summary>
    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    /// <summary>FK → BuddySubscription (null nếu payment dành cho booking)</summary>
    public Guid? BuddySubscriptionId { get; set; }
    public BuddySubscription? BuddySubscription { get; set; }

    public decimal Amount { get; set; }

    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    /// <summary>Mã giao dịch từ cổng thanh toán hoặc chuyển khoản</summary>
    [MaxLength(200)]
    public string? ExternalRef { get; set; }

    /// <summary>Mã đơn hàng PayOS (int64) — dùng để map webhook callback.</summary>
    public long? PayOsOrderCode { get; set; }

    /// <summary>Thời điểm tiền được giữ trong escrow</summary>
    public DateTime? HeldAt { get; set; }

    /// <summary>Thời điểm giải ngân cho Guide</summary>
    public DateTime? ReleasedAt { get; set; }

    /// <summary>Thời điểm hoàn tiền cho Traveler</summary>
    public DateTime? RefundedAt { get; set; }

    [MaxLength(500)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    Card = 2,
    Wallet = 3,
    /// <summary>Thanh toán qua PayOS (QR VietQR)</summary>
    PayOS = 4
}

public enum PaymentStatus
{
    /// <summary>Chờ thanh toán</summary>
    Pending = 0,

    /// <summary>Tiền đã được giữ (escrow)</summary>
    Held = 1,

    /// <summary>Đã giải ngân cho Guide</summary>
    Released = 2,

    /// <summary>Đã hoàn tiền cho Traveler</summary>
    Refunded = 3
}
