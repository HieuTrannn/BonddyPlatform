using BonddyPlatform.Services.DTOs.AdminDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IAdminOverviewService
{
    // ── Platform Overview ──────────────────────────────────────────────────────
    Task<AdminOverviewDto> GetOverviewAsync(DateTime? from, DateTime? to);
    Task<KpiCardsDto> GetKpiCardsAsync(DateTime? from, DateTime? to);
    Task<TripDemandTrendDto> GetTripDemandTrendAsync();
    Task<BookingMixDto> GetBookingMixAsync(DateTime? from, DateTime? to);
    Task<BottomStatsDto> GetBottomStatsAsync(DateTime? from, DateTime? to);
    Task<RevenueOverviewDto> GetRevenueOverviewAsync(DateTime? from, DateTime? to);
    Task<RecentActivityDto> GetRecentActivityAsync();
    Task<PlatformHealthDto> GetPlatformHealthAsync(DateTime? from, DateTime? to);

    // ── Buddy Overview ─────────────────────────────────────────────────────────
    Task<BuddyOverviewDto> GetBuddyOverviewAsync(DateTime? from, DateTime? to);
    Task<BuddyKpiDto> GetBuddyKpiAsync(DateTime? from, DateTime? to);
    Task<BuddySubscriptionDistributionDto> GetBuddySubscriptionDistributionAsync();
    Task<BuddyPerformanceDto> GetBuddyPerformanceAsync(DateTime? from, DateTime? to);
    Task<List<BuddyTopEarnerDto>> GetBuddyTopEarnersAsync(int top = 10);
    Task<List<BuddyTopRatedDto>> GetBuddyTopRatedAsync(int top = 10);
    Task<BuddyRecentActivityDto> GetBuddyRecentActivityAsync();
    Task<BuddyGrowthTrendDto> GetBuddyGrowthTrendAsync();
    Task<BuddyRiskAlertsDto> GetBuddyRiskAlertsAsync();
}
