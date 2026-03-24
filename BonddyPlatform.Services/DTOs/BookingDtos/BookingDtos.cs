using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.PaymentDtos;

namespace BonddyPlatform.Services.DTOs.BookingDtos;

/// <summary>
/// Buddy tạo booking từ một TripRequest đã tồn tại.
/// Buddy được quyền điều chỉnh date/time/duration/adults/children so với Trip gốc.
/// </summary>
public class BookingCreateFromTripRequestDto
{
    // ─── Booked details (buddy có thể chỉnh so với Trip gốc) ──────────────
    public DateOnly BookedDate { get; set; }
    public TimeOnly BookedStartTime { get; set; }
    public int BookedDurationHours { get; set; }
    public int BookedAdults { get; set; }
    public int BookedChildren { get; set; }

    // ─── Giá & nội dung ───────────────────────────────────────────────────
    /// <summary>Tổng giá buddy đề xuất (đã thỏa thuận qua chat).</summary>
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";

    public string? Includes { get; set; }
    public string? Excludes { get; set; }
    public string? NoteForCustomer { get; set; }
}

/// <summary>
/// Buddy creates booking from chat flow.
/// - If TripRequestId is provided: use existing TripRequest flow.
/// - If TripRequestId is null: ChatRoomId is required and backend will create internal Trip/TripRequest.
/// </summary>
public class BookingCreateRequestDto
{
    public Guid? TripRequestId { get; set; }
    public Guid? ChatRoomId { get; set; }

    public DateOnly BookedDate { get; set; }
    public TimeOnly BookedStartTime { get; set; }
    public int BookedDurationHours { get; set; }
    public int BookedAdults { get; set; }
    public int BookedChildren { get; set; }

    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public string? Includes { get; set; }
    public string? Excludes { get; set; }
    public string? NoteForCustomer { get; set; }
}

public class BookingUpdateStatusRequestDto
{
    public BookingStatus Status { get; set; }
}

public class BookingResponseDto
{
    public Guid Id { get; set; }

    // ─── Relationships ────────────────────────────────────────────────────
    public Guid TripId { get; set; }
    public Guid TripRequestId { get; set; }
    public Guid BuddyId { get; set; }
    public string BuddyName { get; set; } = string.Empty;
    public string? BuddyAvatar { get; set; }
    public Guid TravelerUserId { get; set; }
    public string TravelerName { get; set; } = string.Empty;
    public Guid? ChatRoomId { get; set; }

    // ─── Booked details ───────────────────────────────────────────────────
    public DateOnly BookedDate { get; set; }
    public TimeOnly BookedStartTime { get; set; }
    public int BookedDurationHours { get; set; }
    public int BookedAdults { get; set; }
    public int BookedChildren { get; set; }

    // ─── Giá ──────────────────────────────────────────────────────────────
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal PlatformFeeAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Includes { get; set; }
    public string? Excludes { get; set; }
    public string? NoteForCustomer { get; set; }

    // ─── Trạng thái ──────────────────────────────────────────────────────
    public BookingStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime? PaymentDeadline { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ConfirmBookingAndPayResponseDto
{
    public BookingResponseDto Booking { get; set; } = new();
    public PayPalCreateOrderResponseDto PaymentOrder { get; set; } = new();
}
