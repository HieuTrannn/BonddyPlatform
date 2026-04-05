namespace BonddyPlatform.Services.DTOs.AdminDtos;

// ═══════════════════════════════════════════════════════════════════════════════
//  BUDDY OVERVIEW — Consolidated DTO
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Consolidated response for GET /api/admin/overview/buddy-summary</summary>
public class BuddyOverviewDto
{
    public BuddyKpiDto BuddyKpi { get; set; } = new();
    public BuddySubscriptionDistributionDto SubscriptionDistribution { get; set; } = new();
    public BuddyPerformanceDto Performance { get; set; } = new();
    public List<BuddyTopEarnerDto> TopEarners { get; set; } = new();
    public List<BuddyTopRatedDto> TopRated { get; set; } = new();
    public BuddyRecentActivityDto RecentActivity { get; set; } = new();
    public BuddyGrowthTrendDto GrowthTrend { get; set; } = new();
    public BuddyRiskAlertsDto RiskAlerts { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Buddy KPI Cards
// ═══════════════════════════════════════════════════════════════════════════════

public class BuddyKpiDto
{
    public int TotalBuddies { get; set; }
    public int ActiveBuddies { get; set; }
    public int InactiveBuddies { get; set; }
    public int NewBuddiesThisMonth { get; set; }

    // Subscription
    public int BuddiesWithSubscription { get; set; }
    public int BuddiesWithoutSubscription { get; set; }

    // Rating
    public decimal AverageRating { get; set; }
    public int BuddiesWithPerfectRating { get; set; }

    // Activity
    public int BuddiesWithBookings { get; set; }
    public int BuddiesWithoutBookings { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Buddy Subscription Distribution (donut chart)
// ═══════════════════════════════════════════════════════════════════════════════

public class BuddySubscriptionDistributionDto
{
    public int TotalSubscribed { get; set; }
    public int TotalFree { get; set; }
    public List<PackageDistributionDto> Packages { get; set; } = new();
}

public class PackageDistributionDto
{
    public Guid PackageId { get; set; }
    public string PackageName { get; set; } = string.Empty;
    public decimal PricePerMonth { get; set; }
    public string Currency { get; set; } = "VND";
    public decimal CommissionRate { get; set; }
    public int ActiveCount { get; set; }
    public decimal Percentage { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Buddy Performance
// ═══════════════════════════════════════════════════════════════════════════════

public class BuddyPerformanceDto
{
    // Trip Requests
    public int TotalTripRequestsSent { get; set; }
    public int TripRequestsThisMonth { get; set; }

    // Bookings
    public int TotalBookings { get; set; }
    public int CompletedBookings { get; set; }
    public int CancelledBookings { get; set; }
    public int ActiveBookings { get; set; }
    public decimal BookingCompletionRate { get; set; }

    // Earnings
    public decimal TotalEarnings { get; set; }
    public decimal TotalPlatformFees { get; set; }
    public decimal AverageBookingValue { get; set; }

    // Reviews
    public int TotalReviews { get; set; }
    public decimal AverageReviewRating { get; set; }

    // Incidents
    public int TotalIncidents { get; set; }
    public int OpenIncidents { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Buddy Top Earners & Top Rated
// ═══════════════════════════════════════════════════════════════════════════════

public class BuddyTopEarnerDto
{
    public Guid BuddyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public decimal TotalEarnings { get; set; }
    public int CompletedBookings { get; set; }
    public decimal Rating { get; set; }
    public string? CurrentPackage { get; set; }
}

public class BuddyTopRatedDto
{
    public Guid BuddyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public decimal Rating { get; set; }
    public int TotalReviews { get; set; }
    public int CompletedBookings { get; set; }
    public string? CurrentPackage { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Buddy Recent Activity
// ═══════════════════════════════════════════════════════════════════════════════

public class BuddyRecentActivityDto
{
    public List<RecentBuddyRegistrationDto> RecentRegistrations { get; set; } = new();
    public List<RecentBuddyBookingDto> RecentBookings { get; set; } = new();
    public List<RecentBuddySubscriptionDto> RecentSubscriptions { get; set; } = new();
}

public class RecentBuddyRegistrationDto
{
    public Guid BuddyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class RecentBuddyBookingDto
{
    public Guid BookingId { get; set; }
    public string BuddyName { get; set; } = string.Empty;
    public string TravelerName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecentBuddySubscriptionDto
{
    public Guid BuddyId { get; set; }
    public string BuddyName { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public string Currency { get; set; } = "VND";
    public DateTime? PaidAt { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Buddy Growth Trend (4 weeks)
// ═══════════════════════════════════════════════════════════════════════════════

public class BuddyGrowthTrendDto
{
    public List<WeeklyBuddyGrowthDto> Weeks { get; set; } = new();
}

public class WeeklyBuddyGrowthDto
{
    public string WeekLabel { get; set; } = string.Empty;
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int NewBuddies { get; set; }
    public int NewSubscriptions { get; set; }
    public int CompletedBookings { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Buddy Risk Alerts
// ═══════════════════════════════════════════════════════════════════════════════

public class BuddyRiskAlertsDto
{
    /// <summary>Buddies active nhưng chưa nhận booking nào (cần hỗ trợ)</summary>
    public List<BuddyAlertDto> IdleBuddies { get; set; } = new();

    /// <summary>Buddies có rating dưới 3.0</summary>
    public List<BuddyAlertDto> LowRatedBuddies { get; set; } = new();

    /// <summary>Buddies có incident chưa resolved</summary>
    public List<BuddyAlertDto> BuddiesWithOpenIncidents { get; set; } = new();

    /// <summary>Subscriptions sắp hết hạn trong 7 ngày tới</summary>
    public List<BuddyAlertDto> ExpiringSubscriptions { get; set; } = new();
}

public class BuddyAlertDto
{
    public Guid BuddyId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? ProfilePicture { get; set; }
    public string AlertReason { get; set; } = string.Empty;
    public decimal? Rating { get; set; }
    public DateTime? RelevantDate { get; set; }
}
