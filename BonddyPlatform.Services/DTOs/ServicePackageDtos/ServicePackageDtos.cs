using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.ServicePackageDtos;

// ─── Service Package DTOs ──────────────────────────────────────────────────

/// <summary>Response DTO for a service package.</summary>
public class ServicePackageResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal CommissionRate { get; set; }
    public bool HasChatAccess { get; set; }
    public bool HasSearchPriority { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasProductFeedback { get; set; }
    public int? MaxSlots { get; set; }
    /// <summary>Current number of active subscriptions for this package.</summary>
    public int CurrentSlots { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public List<string> Features { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Admin creates a new service package.</summary>
public class ServicePackageCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal CommissionRate { get; set; }
    public bool HasChatAccess { get; set; }
    public bool HasSearchPriority { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool HasProductFeedback { get; set; }
    public int? MaxSlots { get; set; }
    public int SortOrder { get; set; }
    public List<string> Features { get; set; } = new();
}

/// <summary>Admin updates an existing service package.</summary>
public class ServicePackageUpdateDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? PricePerMonth { get; set; }
    public string? Currency { get; set; }
    public decimal? CommissionRate { get; set; }
    public bool? HasChatAccess { get; set; }
    public bool? HasSearchPriority { get; set; }
    public bool? HasPrioritySupport { get; set; }
    public bool? HasProductFeedback { get; set; }
    public int? MaxSlots { get; set; }
    public int? SortOrder { get; set; }
    public bool? IsActive { get; set; }
    public List<string>? Features { get; set; }
}

// ─── Subscription DTOs ─────────────────────────────────────────────────────

/// <summary>Buddy subscribes to a service package — initiates payment flow.</summary>
public class SubscribeRequestDto
{
    public Guid ServicePackageId { get; set; }
}

/// <summary>Response DTO for a buddy's subscription (includes payment info).</summary>
public class BuddySubscriptionResponseDto
{
    public Guid Id { get; set; }
    public Guid BuddyId { get; set; }
    public Guid ServicePackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal CommissionRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;

    // Payment info
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "VND";
    public string? ExternalPaymentRef { get; set; }
    public DateTime? PaidAt { get; set; }

    // Downgrade / Cancel scheduling
    public Guid? DowngradeToPackageId { get; set; }
    public string? DowngradeToPackageName { get; set; }
    public bool IsCancelledAtEndOfCycle { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Response when PayPal order is created for subscription payment.</summary>
public class SubscriptionPayPalOrderResponseDto
{
    public Guid SubscriptionId { get; set; }
    public Guid ServicePackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string? ApproveUrl { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal Amount { get; set; }

    /// <summary>Nếu là upgrade — thông tin proration.</summary>
    public bool IsUpgrade { get; set; }
    public decimal CreditFromOldPlan { get; set; }
    public decimal ProratedCharge { get; set; }
}

/// <summary>Response khi buddy thực hiện downgrade — gói mới sẽ áp dụng khi hết kỳ.</summary>
public class DowngradeResponseDto
{
    public Guid SubscriptionId { get; set; }
    public string CurrentPackageName { get; set; } = string.Empty;
    public string DowngradeToPackageName { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Request to capture a PayPal order for subscription payment.</summary>
public class SubscriptionCaptureRequestDto
{
    public string OrderId { get; set; } = string.Empty;
}

