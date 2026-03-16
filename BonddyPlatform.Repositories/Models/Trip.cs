using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class Trip
{
    public Guid Id { get; set; }

    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationHours { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public List<string>? PreferredLanguages { get; set; }
    [MaxLength(1000)]
    public string? Notes { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Active;
    public Guid TravelerUserId { get; set; }
    public User TravelerUser { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum TripStatus
{
    Draft = 0,
    Active = 1,
    Deactivate = 2,
    Expired = 3,
    /// <summary>Traveler đã xóa trip — soft delete, dữ liệu vẫn còn trong DB.</summary>
    Deleted = 4,
}

