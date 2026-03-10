using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.DTOs.TripDtos;

public class TripCreateRequestDto
{
    public string City { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public int GroupSize { get; set; }
    public string? PreferredLanguage { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? MeetingPoint { get; set; }
    public string? Notes { get; set; }
}

public class TripResponseDto
{
    public Guid Id { get; set; }
    public string City { get; set; } = string.Empty;
    public DateTime? StartTime { get; set; }
    public int DurationMinutes { get; set; }
    public int GroupSize { get; set; }
    public string? PreferredLanguage { get; set; }
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }
    public string? MeetingPoint { get; set; }
    public string? Notes { get; set; }
    public TripStatus Status { get; set; }
    public Guid TravelerUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TripSearchRequestDto : BaseSearchRequestDto
{
    public string? City { get; set; }
    public TripStatus? Status { get; set; }
    public DateTime? StartFrom { get; set; }
    public DateTime? StartTo { get; set; }
}

