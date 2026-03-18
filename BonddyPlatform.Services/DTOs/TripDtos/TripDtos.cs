using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;
using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.TripDtos;

public class TripCreateRequestDto
{
    [Required, MaxLength(100)]
    public string City { get; set; } = string.Empty;

    /// <summary>Date of the trip (yyyy-MM-dd)</summary>
    [Required]
    public DateOnly StartDate { get; set; }

    /// <summary>Preferred start time (HH:mm)</summary>
    [Required]
    public TimeOnly StartTime { get; set; }

    /// <summary>Duration in hours</summary>
    [Required, Range(1, 24)]
    public int DurationHours { get; set; }

    [Required, Range(1, 50)]
    public int Adults { get; set; }

    [Range(0, 50)]
    public int Children { get; set; }

    /// <summary>Activities traveler wants to do</summary>
    public List<string>? Activities { get; set; }

    /// <summary>Languages traveler prefers buddy to speak</summary>
    public List<string>? PreferredLanguages { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class TripUpdateRequestDto
{
    public DateOnly? StartDate { get; set; }
    public TimeOnly? StartTime { get; set; }
    public int? DurationHours { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }
    public List<string>? Activities { get; set; }
    public List<string>? PreferredLanguages { get; set; }
    [MaxLength(1000)]
    public string? Notes { get; set; }
}

public class TripResponseDto
{
    public Guid Id { get; set; }
    public string City { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public TimeOnly StartTime { get; set; }
    public int DurationHours { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public List<string>? Activities { get; set; }
    public List<string>? PreferredLanguages { get; set; }
    public string? Notes { get; set; }
    public TripStatus Status { get; set; }
    public Guid TravelerUserId { get; set; }
    public string TravelerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class TripSearchRequestDto : BaseSearchRequestDto
{
    public string? City { get; set; }
    public TripStatus? Status { get; set; }
    public DateOnly? StartFrom { get; set; }
    public DateOnly? StartTo { get; set; }
}
