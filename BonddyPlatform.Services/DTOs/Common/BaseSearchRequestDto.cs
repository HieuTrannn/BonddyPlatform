using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.Common;

/// <summary>
/// Base class for search request DTOs with paging, sorting, and search capabilities
/// </summary>
public class BaseSearchRequestDto
{
    /// <summary>
    /// Search keyword to search across multiple fields
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page must be greater than 0")]
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page (max 100)
    /// </summary>
    [Range(1, 100, ErrorMessage = "PageSize must be between 1 and 100")]
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Field name to sort by
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Sort order: "asc" or "desc" (default: "desc")
    /// </summary>
    public string? SortOrder { get; set; } = "desc";
}
