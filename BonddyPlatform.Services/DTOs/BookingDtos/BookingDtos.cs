using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.BookingDtos;

public class BookingSurchargeRequestDto
{
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class BookingCreateRequestDto
{
    public Guid TripId { get; set; }
    public Guid BuddyId { get; set; }
    public decimal BaseRatePerHour { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal TipAmount { get; set; }
    public DateTime? ScheduledStartTime { get; set; }
    public List<BookingSurchargeRequestDto> Surcharges { get; set; } = new();
}

public class BookingUpdateStatusRequestDto
{
    public BookingStatus Status { get; set; }
}

public class BookingSurchargeResponseDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class BookingResponseDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string TripCity { get; set; } = string.Empty;
    public Guid BuddyId { get; set; }
    public string BuddyName { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public string CreatedByUserName { get; set; } = string.Empty;

    public decimal BaseRatePerHour { get; set; }
    public int DurationMinutes { get; set; }
    public decimal SubtotalAmount { get; set; }
    public decimal SurchargesAmount { get; set; }
    public decimal PlatformFeeAmount { get; set; }
    public decimal TipAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime? ScheduledStartTime { get; set; }
    public BookingStatus Status { get; set; }

    public List<BookingSurchargeResponseDto> Surcharges { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
