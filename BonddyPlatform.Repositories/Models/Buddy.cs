using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

/// <summary>
/// A Buddy is created when a User registers to become a Buddy on the platform.
/// It extends the User's basic info with buddy-specific details.
/// </summary>
public class Buddy
{
    public Guid Id { get; set; }

    /// <summary>FK → User (the user who is a buddy)</summary>
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>Activities the buddy specialises in (stored as JSON)</summary>
    public List<BuddyActivity> Activities { get; set; } = new();

    /// <summary>Rate charged per hour (in USD)</summary>
    public decimal CostPerHour { get; set; }

    /// <summary>Average star rating (0.0–5.0), updated from Reviews</summary>
    public decimal Rate { get; set; }

    /// <summary>Languages the buddy can communicate in (stored as JSON)</summary>
    public List<string> Languages { get; set; } = new();

    [MaxLength(300)]
    public string Bio { get; set; } = string.Empty;

    /// <summary>Whether this buddy is visible and accepting bookings</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum BuddyActivity
{
    Shopping = 0,
    NightlifeAndBars = 1,
    ArtAndMuseums = 2,
    ExplorationAndSightseeing = 3,
    FoodAndDining = 4,
    Sports = 5,
    Photography = 6,
    Music = 7,
    LocalExperience = 8,
    NatureAndOutdoors = 9
}
