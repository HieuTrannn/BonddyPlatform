using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public class Review
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;

    /// <summary>Traveler viết review</summary>
    public Guid ReviewerUserId { get; set; }
    public User ReviewerUser { get; set; } = null!;

    /// <summary>1–5</summary>
    public int Rating { get; set; }

    [MaxLength(1000)]
    public string? Comment { get; set; }

    public bool IsPublic { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
