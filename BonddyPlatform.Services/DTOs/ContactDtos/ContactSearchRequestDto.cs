using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.DTOs.ContactDtos;

/// <summary>
/// Search request DTO for Contacts
/// </summary>
public class ContactSearchRequestDto : BaseSearchRequestDto
{
    /// <summary>
    /// Filter by date range - from date
    /// </summary>
    public DateTime? CreatedFrom { get; set; }

    /// <summary>
    /// Filter by date range - to date
    /// </summary>
    public DateTime? CreatedTo { get; set; }
}
