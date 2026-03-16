using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class Booking
{
    public Guid Id { get; set; }

    // Core relationships
    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public Guid TripRequestId { get; set; }
    public TripRequest TripRequest { get; set; } = null!;

    public Guid BuddyId { get; set; }
    public Buddy Buddy { get; set; } = null!;

    public Guid TravelerUserId { get; set; }
    public User TravelerUser { get; set; } = null!;

    public Guid? ChatRoomId { get; set; }

    // ─── Booked fields — những gì buddy và traveler thực sự chốt ─────────────
    public DateOnly BookedDate { get; set; }
    public TimeOnly BookedStartTime { get; set; }
    public int BookedDurationHours { get; set; }
    public int BookedAdults { get; set; }
    public int BookedChildren { get; set; }

    // ─── Giá & nội dung ──────────────────────────────────────────────────────
    public decimal Price { get; set; }          // Tổng tiền buddy offer
    [MaxLength(10)]
    public string Currency { get; set; } = "USD"; // Default currency is USD
    public decimal PlatformFeeAmount { get; set; } // 10% of Price
    public decimal TotalAmount { get; set; }    // Price + PlatformFee

    [MaxLength(2000)]
    public string? Includes { get; set; }       // Dịch vụ bao gồm
    [MaxLength(2000)]
    public string? Excludes { get; set; }       // Dịch vụ không bao gồm
    [MaxLength(500)]
    public string? NoteForCustomer { get; set; }

    // ─── Trạng thái ──────────────────────────────────────────────────────────
    public BookingStatus Status { get; set; } = BookingStatus.PendingCustomerConfirm;

    /// <summary>Hạn để khách thanh toán khi chuyển sang PendingPayment.</summary>
    public DateTime? PaymentDeadline { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Payment? Payment { get; set; }
}

public enum BookingStatus
{
    /// <summary>Buddy đã tạo booking, chờ khách bấm Confirm.</summary>
    PendingCustomerConfirm = 0,
    /// <summary>Khách đã Confirm, đang chờ thanh toán.</summary>
    PendingPayment = 1,
    /// <summary>Thanh toán xong, booking được giữ chỗ chính thức.</summary>
    Confirmed = 2,
    /// <summary>Tour đang diễn ra.</summary>
    InProgress = 3,
    /// <summary>Tour hoàn thành.</summary>
    Completed = 4,
    /// <summary>Bị hủy bởi bất kỳ bên nào.</summary>
    Cancelled = 5,
    /// <summary>Hết hạn thanh toán mà chưa trả tiền; hủy tự động.</summary>
    CancelledByTimeout = 6
}
