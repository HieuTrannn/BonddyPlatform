using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

/// <summary>
/// Defines a service package that Buddies can subscribe to.
/// Each package grants different commission rates, features, and privileges.
/// </summary>
public class ServicePackage
{
    public Guid Id { get; set; }

    /// <summary>Package name, e.g. "Starter", "Pro", "Founder"</summary>
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Short description of the package</summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Monthly price in the specified currency</summary>
    public decimal PricePerMonth { get; set; }

    /// <summary>Currency code, e.g. "VND"</summary>
    [MaxLength(10)]
    public string Currency { get; set; } = "VND";

    /// <summary>Commission rate taken by the platform (0.20 = 20%, 0.10 = 10%, 0.00 = 0%)</summary>
    public decimal CommissionRate { get; set; }

    /// <summary>Whether the buddy can chat with customers via the app</summary>
    public bool HasChatAccess { get; set; }

    /// <summary>Whether the buddy gets priority in search results</summary>
    public bool HasSearchPriority { get; set; }

    /// <summary>Whether the buddy gets priority support from team Bonddy</summary>
    public bool HasPrioritySupport { get; set; }

    /// <summary>Whether the buddy can give product direction feedback</summary>
    public bool HasProductFeedback { get; set; }

    /// <summary>Maximum number of active subscriptions for this package. null = unlimited.</summary>
    public int? MaxSlots { get; set; }

    /// <summary>Display order (lower = first)</summary>
    public int SortOrder { get; set; }

    /// <summary>Whether this package is available for purchase</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>List of feature descriptions (stored as JSON)</summary>
    public List<string> Features { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
