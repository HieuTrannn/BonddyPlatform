namespace BonddyPlatform.Services.DTOs.AdminDtos;

// ═══════════════════════════════════════════════════════════════════════════════
//  PLATFORM OVERVIEW — Consolidated DTO
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Consolidated response for GET /api/admin/overview/summary</summary>
public class AdminOverviewDto
{
    public KpiCardsDto KpiCards { get; set; } = new();
    public TripDemandTrendDto TripDemandTrend { get; set; } = new();
    public BookingMixDto BookingMix { get; set; } = new();
    public BottomStatsDto BottomStats { get; set; } = new();
    public RevenueOverviewDto RevenueOverview { get; set; } = new();
    public RecentActivityDto RecentActivity { get; set; } = new();
    public PlatformHealthDto PlatformHealth { get; set; } = new();
}

// ═══════════════════════════════════════════════════════════════════════════════
//  KPI Cards
// ═══════════════════════════════════════════════════════════════════════════════

public class KpiCardsDto
{
    // Total Users
    public int TotalUsers { get; set; }
    public int VerifiedUsers { get; set; }
    public int NewUsersThisWindow { get; set; }

    // Buddy Supply
    public int TotalBuddies { get; set; }
    public int ActiveBuddies { get; set; }
    public int BuddiesWithSubscription { get; set; }

    // Trip Demand
    public int TotalTrips { get; set; }
    public int OpenTrips { get; set; }

    // Incident Queue
    public int TotalIncidents { get; set; }
    public int UnresolvedIncidents { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Trip Demand Trend (fixed 4 weeks)
// ═══════════════════════════════════════════════════════════════════════════════

public class TripDemandTrendDto
{
    public List<WeeklyTripDataDto> Weeks { get; set; } = new();
}

public class WeeklyTripDataDto
{
    public string WeekLabel { get; set; } = string.Empty;
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public int TotalTrips { get; set; }
    public int OpenTrips { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Booking Mix (donut chart)
// ═══════════════════════════════════════════════════════════════════════════════

public class BookingMixDto
{
    public int TotalBookings { get; set; }
    public List<BookingStatusBreakdownDto> StatusBreakdown { get; set; } = new();
}

public class BookingStatusBreakdownDto
{
    public string StatusName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Bottom Stats
// ═══════════════════════════════════════════════════════════════════════════════

public class BottomStatsDto
{
    // Revenue
    public decimal TotalRevenue { get; set; }
    public decimal PlatformFeeRevenue { get; set; }
    public string Currency { get; set; } = "USD";

    // Booking Focus
    public int TotalActiveBookings { get; set; }
    public int DirectBookings { get; set; }

    // Resolution Rate
    public decimal ResolutionRate { get; set; }
    public int ResolvedIncidents { get; set; }
    public int ClosedIncidents { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Revenue Overview
// ═══════════════════════════════════════════════════════════════════════════════

public class RevenueOverviewDto
{
    public decimal TotalBookingRevenue { get; set; }
    public decimal TotalPlatformFees { get; set; }
    public decimal TotalSubscriptionRevenue { get; set; }
    public decimal TotalCombinedRevenue { get; set; }

    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public decimal GrowthPercentage { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Recent Activity
// ═══════════════════════════════════════════════════════════════════════════════

public class RecentActivityDto
{
    public List<RecentBookingDto> RecentBookings { get; set; } = new();
    public List<RecentIncidentDto> RecentIncidents { get; set; } = new();
    public List<RecentUserDto> RecentUsers { get; set; } = new();
}

public class RecentBookingDto
{
    public Guid Id { get; set; }
    public string TravelerName { get; set; } = string.Empty;
    public string BuddyName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecentIncidentDto
{
    public Guid Id { get; set; }
    public string TypeName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string ReporterName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class RecentUserDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ═══════════════════════════════════════════════════════════════════════════════
//  Platform Health
// ═══════════════════════════════════════════════════════════════════════════════

public class PlatformHealthDto
{
    // User Metrics
    public decimal UserVerificationRate { get; set; }
    public int InactiveUsers { get; set; }

    // Buddy Metrics
    public decimal AverageBuddyRating { get; set; }
    public int BuddiesWithoutBookings { get; set; }

    // Booking Metrics
    public decimal BookingCompletionRate { get; set; }
    public decimal AverageBookingValue { get; set; }
    public int CancelledBookingsThisMonth { get; set; }

    // Subscription Metrics
    public int ActiveSubscriptions { get; set; }
    public int PendingPaymentSubscriptions { get; set; }
    public List<SubscriptionByPackageDto> SubscriptionByPackage { get; set; } = new();

    // Review Metrics
    public decimal AverageReviewRating { get; set; }
    public int TotalReviews { get; set; }
}

public class SubscriptionByPackageDto
{
    public string PackageName { get; set; } = string.Empty;
    public int Count { get; set; }
}
