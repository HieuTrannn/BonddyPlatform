using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.AdminDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

/// <summary>
/// Admin Overview Dashboard — Platform + Buddy analytics APIs.
/// Route prefix: /api/admin/overview
/// </summary>
[Authorize(Roles = "Admin")]
[Route("api/admin/overview")]
[ApiController]
public class AdminOverviewController : ApiControllerBase
{
    private readonly IAdminOverviewService _overviewService;

    public AdminOverviewController(IAdminOverviewService overviewService)
    {
        _overviewService = overviewService;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PLATFORM OVERVIEW
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [TỔNG HỢP] Trả về toàn bộ dữ liệu Platform Overview trong 1 lần gọi.
    /// Bao gồm: KPI Cards, Trip Demand Trend, Booking Mix, Bottom Stats,
    /// Revenue Overview, Recent Activity, Platform Health.
    /// </summary>
    [HttpGet("summary")]
    [SwaggerOperation(
        Summary = "Admin Overview: tổng hợp toàn bộ dữ liệu platform",
        Description = "Trả về tất cả section của Platform Overview trong 1 call. " +
                      "Dùng cho lần load đầu của trang dashboard. " +
                      "Query params `from` / `to` (ISO 8601 UTC) để lọc theo time range. " +
                      "Mặc định = tháng hiện tại nếu không truyền.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<AdminOverviewDto>))]
    public async Task<IActionResult> GetOverviewSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetOverviewAsync(from, to));

    // ─── Granular endpoints ──────────────────────────────────────────────────

    /// <summary>4 KPI thẻ: Total Users, Buddy Supply, Trip Demand, Incident Queue.</summary>
    [HttpGet("kpi-cards")]
    [SwaggerOperation(Summary = "Admin Overview: 4 KPI cards — Users / Buddies / Trips / Incidents")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<KpiCardsDto>))]
    public async Task<IActionResult> GetKpiCards(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetKpiCardsAsync(from, to));

    /// <summary>
    /// Biểu đồ cột Trip Demand — fixed 4 tuần gần nhất.
    /// Mỗi tuần trả về tổng trips được tạo vs số open trips.
    /// </summary>
    [HttpGet("trip-demand-trend")]
    [SwaggerOperation(Summary = "Admin Overview: trip demand trend — 4 tuần gần nhất")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<TripDemandTrendDto>))]
    public async Task<IActionResult> GetTripDemandTrend()
        => await ExecuteAsync(() => _overviewService.GetTripDemandTrendAsync());

    /// <summary>Donut chart phân bổ Booking theo status trong time range.</summary>
    [HttpGet("booking-mix")]
    [SwaggerOperation(Summary = "Admin Overview: booking mix — phân bổ theo status")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BookingMixDto>))]
    public async Task<IActionResult> GetBookingMix(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetBookingMixAsync(from, to));

    /// <summary>
    /// Bottom stats: Selected Trip Revenue, Booking Focus (direct bookings),
    /// Resolution Rate của incidents.
    /// </summary>
    [HttpGet("bottom-stats")]
    [SwaggerOperation(Summary = "Admin Overview: bottom stats — revenue / booking focus / resolution rate")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BottomStatsDto>))]
    public async Task<IActionResult> GetBottomStats(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetBottomStatsAsync(from, to));

    /// <summary>
    /// Revenue overview: booking revenue, platform fees, subscription revenue,
    /// tổng kết hợp và so sánh MoM (month-over-month).
    /// </summary>
    [HttpGet("revenue")]
    [SwaggerOperation(Summary = "Admin Overview: revenue — booking fees + subscription breakdown + MoM growth")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<RevenueOverviewDto>))]
    public async Task<IActionResult> GetRevenue(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetRevenueOverviewAsync(from, to));

    /// <summary>5 bookings, 5 incidents và 5 users mới nhất (không phụ thuộc time range).</summary>
    [HttpGet("recent-activity")]
    [SwaggerOperation(Summary = "Admin Overview: recent activity — 5 bookings / incidents / users mới nhất")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<RecentActivityDto>))]
    public async Task<IActionResult> GetRecentActivity()
        => await ExecuteAsync(() => _overviewService.GetRecentActivityAsync());

    /// <summary>
    /// Platform health: verification rate, buddy rating, booking completion rate,
    /// subscription distribution, review metrics.
    /// </summary>
    [HttpGet("platform-health")]
    [SwaggerOperation(Summary = "Admin Overview: platform health — rates / averages / distributions")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PlatformHealthDto>))]
    public async Task<IActionResult> GetPlatformHealth(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetPlatformHealthAsync(from, to));

    // ═══════════════════════════════════════════════════════════════════════════
    //  BUDDY OVERVIEW
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// [TỔNG HỢP] Trả về toàn bộ dữ liệu Buddy Overview trong 1 lần gọi.
    /// Bao gồm: KPI, Subscription Distribution, Performance, Top Earners/Rated,
    /// Recent Activity, Growth Trend, Risk Alerts.
    /// </summary>
    [HttpGet("buddy-summary")]
    [SwaggerOperation(
        Summary = "Admin Overview: tổng hợp toàn bộ dữ liệu Buddy",
        Description = "Trả về tất cả section của Buddy Overview trong 1 call. " +
                      "Query params `from` / `to` áp dụng cho các metric có time window.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyOverviewDto>))]
    public async Task<IActionResult> GetBuddyOverviewSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetBuddyOverviewAsync(from, to));

    // ─── Granular Buddy endpoints ─────────────────────────────────────────────

    /// <summary>
    /// Buddy KPI cards: tổng/active/inactive buddies, subscription count,
    /// average rating, activity metrics.
    /// </summary>
    [HttpGet("buddy-kpi")]
    [SwaggerOperation(Summary = "Admin Overview: buddy KPI cards")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyKpiDto>))]
    public async Task<IActionResult> GetBuddyKpi(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetBuddyKpiAsync(from, to));

    /// <summary>Donut chart phân bổ buddy theo gói subscription (Free / Starter / Pro / Founder).</summary>
    [HttpGet("buddy-subscription-distribution")]
    [SwaggerOperation(Summary = "Admin Overview: buddy subscription distribution — phân bổ theo gói")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddySubscriptionDistributionDto>))]
    public async Task<IActionResult> GetBuddySubscriptionDistribution()
        => await ExecuteAsync(() => _overviewService.GetBuddySubscriptionDistributionAsync());

    /// <summary>
    /// Performance tổng thể của Buddy ecosystem: trip requests, booking rates,
    /// earnings, reviews, incidents.
    /// </summary>
    [HttpGet("buddy-performance")]
    [SwaggerOperation(Summary = "Admin Overview: buddy performance — bookings / earnings / reviews / incidents")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyPerformanceDto>))]
    public async Task<IActionResult> GetBuddyPerformance(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => await ExecuteAsync(() => _overviewService.GetBuddyPerformanceAsync(from, to));

    /// <summary>Top 10 buddy có tổng thu nhập cao nhất (dựa trên completed bookings).</summary>
    [HttpGet("buddy-top-earners")]
    [SwaggerOperation(Summary = "Admin Overview: top 10 buddy thu nhập cao nhất")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<List<BuddyTopEarnerDto>>))]
    public async Task<IActionResult> GetBuddyTopEarners([FromQuery] int top = 10)
        => await ExecuteAsync(() => _overviewService.GetBuddyTopEarnersAsync(top));

    /// <summary>Top 10 buddy có rating cao nhất (chỉ tính buddy có ít nhất 1 review).</summary>
    [HttpGet("buddy-top-rated")]
    [SwaggerOperation(Summary = "Admin Overview: top 10 buddy rating cao nhất")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<List<BuddyTopRatedDto>>))]
    public async Task<IActionResult> GetBuddyTopRated([FromQuery] int top = 10)
        => await ExecuteAsync(() => _overviewService.GetBuddyTopRatedAsync(top));

    /// <summary>
    /// Recent activity của buddies: 5 buddy mới đăng ký, 5 booking mới nhất của buddy,
    /// 5 subscription được kích hoạt gần đây nhất.
    /// </summary>
    [HttpGet("buddy-recent-activity")]
    [SwaggerOperation(Summary = "Admin Overview: buddy recent activity — registrations / bookings / subscriptions")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyRecentActivityDto>))]
    public async Task<IActionResult> GetBuddyRecentActivity()
        => await ExecuteAsync(() => _overviewService.GetBuddyRecentActivityAsync());

    /// <summary>
    /// Biểu đồ tăng trưởng Buddy — fixed 4 tuần gần nhất.
    /// Mỗi tuần: buddy mới, subscription mới, completed bookings.
    /// </summary>
    [HttpGet("buddy-growth-trend")]
    [SwaggerOperation(Summary = "Admin Overview: buddy growth trend — 4 tuần gần nhất")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyGrowthTrendDto>))]
    public async Task<IActionResult> GetBuddyGrowthTrend()
        => await ExecuteAsync(() => _overviewService.GetBuddyGrowthTrendAsync());

    /// <summary>
    /// Risk alerts: idle buddies (không booking), low-rated buddies (&lt;3.0),
    /// buddies có open incidents, subscriptions sắp hết hạn trong 7 ngày.
    /// </summary>
    [HttpGet("buddy-risk-alerts")]
    [SwaggerOperation(Summary = "Admin Overview: buddy risk alerts — idle / low-rated / incidents / expiring subs")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyRiskAlertsDto>))]
    public async Task<IActionResult> GetBuddyRiskAlerts()
        => await ExecuteAsync(() => _overviewService.GetBuddyRiskAlertsAsync());
}
