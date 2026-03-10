namespace BonddyPlatform.Repositories.Models;

public class Booking
{
    public Guid Id { get; set; }

    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = null!;

    public Guid GuideId { get; set; }
    public Guide Guide { get; set; } = null!;

    /// <summary>CS/Admin tạo booking</summary>
    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public decimal BaseRatePerHour { get; set; }
    public int DurationMinutes { get; set; }

    public decimal SubtotalAmount { get; set; }
    public decimal SurchargesAmount { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime? ScheduledStartTime { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<BookingSurcharge> Surcharges { get; set; } = new List<BookingSurcharge>();

    public Payment? Payment { get; set; }
}

public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}