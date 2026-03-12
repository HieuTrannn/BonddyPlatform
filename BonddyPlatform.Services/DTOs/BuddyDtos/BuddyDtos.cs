using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.BuddyDtos;

/// <summary>Used by Admin/CS to create a Buddy profile for an existing User.</summary>
public class BuddyRegisterRequestDto
{
    public List<BuddyActivity> Activities { get; set; } = new();
    public decimal CostPerHour { get; set; }
    public List<string> Languages { get; set; } = new();
    public string Bio { get; set; } = string.Empty;
}

/// <summary>Used by Admin to update only buddy-specific fields.</summary>
public class BuddyUpdateRequestDto
{
    public List<BuddyActivity>? Activities { get; set; }
    public decimal? CostPerHour { get; set; }
    public List<string>? Languages { get; set; }
    public string? Bio { get; set; }
    public bool? IsActive { get; set; }
}

/// <summary>
/// Used by Buddy to update their own profile in a single JSON call.
/// Combines User info fields and Buddy fields together.
/// </summary>
public class BuddySelfUpdateRequestDto
{
    // ── User fields ──────────────────────────────────────────────────────────
    public string? FullName { get; set; }
    public Gender? Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AboutMe { get; set; }

    // ── Buddy fields ─────────────────────────────────────────────────────────
    public List<BuddyActivity>? Activities { get; set; }
    public decimal? CostPerHour { get; set; }
    public List<string>? Languages { get; set; }
    public string? Bio { get; set; }
}

public class BuddyResponseDto
{
    public Guid Id { get; set; }

    // ── User info ─────────────────────────────────────────────────────────────
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AboutMe { get; set; }
    public string? ProfilePicture { get; set; }

    // ── Buddy info ────────────────────────────────────────────────────────────
    public List<BuddyActivity> Activities { get; set; } = new();
    public decimal CostPerHour { get; set; }
    public decimal Rate { get; set; }
    public List<string> Languages { get; set; } = new();
    public string Bio { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
