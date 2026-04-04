using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.DTOs.BuddyDtos;

/// <summary>Used by Admin/CS to create a Buddy profile for an existing User.</summary>
public class BuddyRegisterRequestDto
{
    public List<string> Activities { get; set; } = new();
    public decimal CostPerHour { get; set; }
    public List<string> Languages { get; set; } = new();
    public string Bio { get; set; } = string.Empty;
}

/// <summary>Search request DTO for paginated buddy listing.</summary>
public class BuddySearchRequestDto : BaseSearchRequestDto
{
    /// <summary>Filter by active status</summary>
    public bool? IsActive { get; set; }
}

/// <summary>Used by Admin to update only buddy-specific fields.</summary>
public class BuddyUpdateRequestDto
{
    public List<string>? Activities { get; set; }
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
    public List<string>? Activities { get; set; }
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
    public List<string> Activities { get; set; } = new();
    public decimal CostPerHour { get; set; }
    public decimal Rate { get; set; }
    public List<string> Languages { get; set; } = new();
    public string Bio { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // ── Subscription info (null nếu chưa mua gói) ────────────────────────────
    public BuddySubscriptionSummaryDto? Subscription { get; set; }
}

/// <summary>Tóm tắt thông tin gói dịch vụ buddy đang dùng.</summary>
public class BuddySubscriptionSummaryDto
{
    public Guid SubscriptionId { get; set; }
    public Guid ServicePackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
}
