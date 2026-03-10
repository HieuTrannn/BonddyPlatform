using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class Trip
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    public DateTime? StartTime { get; set; }

    public int DurationMinutes { get; set; }

    public int GroupSize { get; set; }

    [MaxLength(10)]
    public string? PreferredLanguage { get; set; }

    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }

    [MaxLength(200)]
    public string? MeetingPoint { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public TripStatus Status { get; set; } = TripStatus.Submitted;

    public Guid TravelerUserId { get; set; }
    public User TravelerUser { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum TripStatus
{
    Draft = 0,
    Submitted = 1,
    InProgress = 2,
    Completed = 3,
    Closed = 4,
    Cancelled = 5
}

