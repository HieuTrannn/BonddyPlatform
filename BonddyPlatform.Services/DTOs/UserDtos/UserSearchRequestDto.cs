using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.UserDtos;

/// <summary>
/// Search request DTO for Users with filtering capabilities
/// </summary>
public class UserSearchRequestDto : BaseSearchRequestDto
{
    /// <summary>
    /// Filter by gender
    /// </summary>
    public Gender? Gender { get; set; }

    /// <summary>
    /// Filter by email verified status
    /// </summary>
    public bool? IsEmailVerified { get; set; }

    /// <summary>
    /// Filter by date range - from date
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by date range - to date
    /// </summary>
    public DateTime? CreatedTo { get; set; }
}
