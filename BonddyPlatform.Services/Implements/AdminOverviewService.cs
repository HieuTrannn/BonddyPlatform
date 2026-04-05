using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.AdminDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class AdminOverviewService : IAdminOverviewService
{
    private readonly IUnitOfWork _uow;

    public AdminOverviewService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  Helper: default time window = current month
    // ═══════════════════════════════════════════════════════════════════════════

    private static (DateTime from, DateTime to) ResolveTimeWindow(DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow;
        var resolvedFrom = from ?? new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var resolvedTo = to ?? now;
        return (resolvedFrom, resolvedTo);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  PLATFORM OVERVIEW — Consolidated
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<AdminOverviewDto> GetOverviewAsync(DateTime? from, DateTime? to)
    {
        return new AdminOverviewDto
        {
            KpiCards = await GetKpiCardsAsync(from, to),
            TripDemandTrend = await GetTripDemandTrendAsync(),
            BookingMix = await GetBookingMixAsync(from, to),
            BottomStats = await GetBottomStatsAsync(from, to),
            RevenueOverview = await GetRevenueOverviewAsync(from, to),
            RecentActivity = await GetRecentActivityAsync(),
            PlatformHealth = await GetPlatformHealthAsync(from, to)
        };
    }

    // ─── KPI Cards ──────────────────────────────────────────────────────────

    public async Task<KpiCardsDto> GetKpiCardsAsync(DateTime? from, DateTime? to)
    {
        var (windowFrom, windowTo) = ResolveTimeWindow(from, to);

        var usersQuery = _uow.Users.GetQueryable();
        var buddiesQuery = _uow.Buddies.GetQueryable();
        var tripsQuery = _uow.Trips.GetQueryable();
        var incidentsQuery = _uow.Incidents.GetQueryable();
        var subsQuery = _uow.BuddySubscriptions.GetQueryable();

        return new KpiCardsDto
        {
            TotalUsers = await usersQuery.CountAsync(),
            VerifiedUsers = await usersQuery.CountAsync(u => u.IsEmailVerified),
            NewUsersThisWindow = await usersQuery.CountAsync(u => u.CreatedAt >= windowFrom && u.CreatedAt <= windowTo),

            TotalBuddies = await buddiesQuery.CountAsync(),
            ActiveBuddies = await buddiesQuery.CountAsync(b => b.IsActive),
            BuddiesWithSubscription = await subsQuery.CountAsync(s => s.Status == SubscriptionStatus.Active),

            TotalTrips = await tripsQuery.CountAsync(t => t.CreatedAt >= windowFrom && t.CreatedAt <= windowTo),
            OpenTrips = await tripsQuery.CountAsync(t => t.Status == TripStatus.Open),

            TotalIncidents = await incidentsQuery.CountAsync(),
            UnresolvedIncidents = await incidentsQuery.CountAsync(i =>
                i.Status == IncidentStatus.Open || i.Status == IncidentStatus.InReview)
        };
    }

    // ─── Trip Demand Trend (fixed 4 weeks) ──────────────────────────────────

    public async Task<TripDemandTrendDto> GetTripDemandTrendAsync()
    {
        var now = DateTime.UtcNow;
        // Start of current week (Monday)
        var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        var currentWeekStart = now.Date.AddDays(-daysSinceMonday);
        var fourWeeksAgo = currentWeekStart.AddDays(-21); // 3 weeks before current week start

        var trips = await _uow.Trips.GetQueryable()
            .Where(t => t.CreatedAt >= fourWeeksAgo && t.Status != TripStatus.Deleted)
            .Select(t => new { t.CreatedAt, t.Status })
            .ToListAsync();

        var weeks = new List<WeeklyTripDataDto>();
        for (int i = 0; i < 4; i++)
        {
            var weekStart = fourWeeksAgo.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(7);
            var weekTrips = trips.Where(t => t.CreatedAt >= weekStart && t.CreatedAt < weekEnd).ToList();

            weeks.Add(new WeeklyTripDataDto
            {
                WeekLabel = $"Week {i + 1}",
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                TotalTrips = weekTrips.Count,
                OpenTrips = weekTrips.Count(t => t.Status == TripStatus.Open)
            });
        }

        return new TripDemandTrendDto { Weeks = weeks };
    }

    // ─── Booking Mix ────────────────────────────────────────────────────────

    public async Task<BookingMixDto> GetBookingMixAsync(DateTime? from, DateTime? to)
    {
        var (windowFrom, windowTo) = ResolveTimeWindow(from, to);

        var bookings = await _uow.Bookings.GetQueryable()
            .Where(b => b.CreatedAt >= windowFrom && b.CreatedAt <= windowTo)
            .GroupBy(b => b.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var total = bookings.Sum(b => b.Count);

        return new BookingMixDto
        {
            TotalBookings = total,
            StatusBreakdown = bookings.Select(b => new BookingStatusBreakdownDto
            {
                StatusName = b.Status.ToString(),
                Count = b.Count,
                Percentage = total > 0 ? Math.Round((decimal)b.Count / total * 100, 1) : 0
            }).ToList()
        };
    }

    // ─── Bottom Stats ───────────────────────────────────────────────────────

    public async Task<BottomStatsDto> GetBottomStatsAsync(DateTime? from, DateTime? to)
    {
        var (windowFrom, windowTo) = ResolveTimeWindow(from, to);

        var bookingsQuery = _uow.Bookings.GetQueryable()
            .Where(b => b.CreatedAt >= windowFrom && b.CreatedAt <= windowTo);

        var paidStatuses = new[]
        {
            BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Completed
        };

        var paidBookings = bookingsQuery.Where(b => paidStatuses.Contains(b.Status));

        var totalRevenue = await paidBookings.SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
        var platformFees = await paidBookings.SumAsync(b => (decimal?)b.PlatformFeeAmount) ?? 0;

        var activeBookings = await bookingsQuery.CountAsync(b =>
            b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress);

        // Direct bookings: Trip.City == "Direct booking"
        var directBookings = await bookingsQuery
            .Include(b => b.Trip)
            .CountAsync(b => b.Trip.City == "Direct booking");

        // Incident resolution
        var incidentsQuery = _uow.Incidents.GetQueryable();
        var totalIncidents = await incidentsQuery.CountAsync();
        var resolved = await incidentsQuery.CountAsync(i => i.Status == IncidentStatus.Resolved);
        var closed = await incidentsQuery.CountAsync(i => i.Status == IncidentStatus.Closed);
        var resolutionRate = totalIncidents > 0
            ? Math.Round((decimal)(resolved + closed) / totalIncidents * 100, 1)
            : 0;

        return new BottomStatsDto
        {
            TotalRevenue = totalRevenue,
            PlatformFeeRevenue = platformFees,
            TotalActiveBookings = activeBookings,
            DirectBookings = directBookings,
            ResolutionRate = resolutionRate,
            ResolvedIncidents = resolved,
            ClosedIncidents = closed
        };
    }

    // ─── Revenue Overview ───────────────────────────────────────────────────

    public async Task<RevenueOverviewDto> GetRevenueOverviewAsync(DateTime? from, DateTime? to)
    {
        var paidStatuses = new[]
        {
            BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Completed
        };

        var allPaidBookings = _uow.Bookings.GetQueryable()
            .Where(b => paidStatuses.Contains(b.Status));

        var totalBookingRevenue = await allPaidBookings.SumAsync(b => (decimal?)b.TotalAmount) ?? 0;
        var totalPlatformFees = await allPaidBookings.SumAsync(b => (decimal?)b.PlatformFeeAmount) ?? 0;

        var totalSubRevenue = await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active)
            .SumAsync(s => (decimal?)s.AmountPaid) ?? 0;

        // Month-over-month
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = thisMonthStart.AddMonths(-1);

        var revenueThisMonth = await allPaidBookings
            .Where(b => b.CreatedAt >= thisMonthStart)
            .SumAsync(b => (decimal?)b.PlatformFeeAmount) ?? 0;

        // Add subscription revenue this month
        revenueThisMonth += await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active && s.PaidAt >= thisMonthStart)
            .SumAsync(s => (decimal?)s.AmountPaid) ?? 0;

        var revenueLastMonth = await allPaidBookings
            .Where(b => b.CreatedAt >= lastMonthStart && b.CreatedAt < thisMonthStart)
            .SumAsync(b => (decimal?)b.PlatformFeeAmount) ?? 0;

        revenueLastMonth += await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.PaidAt >= lastMonthStart && s.PaidAt < thisMonthStart)
            .SumAsync(s => (decimal?)s.AmountPaid) ?? 0;

        var growth = revenueLastMonth > 0
            ? Math.Round((revenueThisMonth - revenueLastMonth) / revenueLastMonth * 100, 1)
            : (revenueThisMonth > 0 ? 100m : 0m);

        return new RevenueOverviewDto
        {
            TotalBookingRevenue = totalBookingRevenue,
            TotalPlatformFees = totalPlatformFees,
            TotalSubscriptionRevenue = totalSubRevenue,
            TotalCombinedRevenue = totalPlatformFees + totalSubRevenue,
            RevenueThisMonth = revenueThisMonth,
            RevenueLastMonth = revenueLastMonth,
            GrowthPercentage = growth
        };
    }

    // ─── Recent Activity ────────────────────────────────────────────────────

    public async Task<RecentActivityDto> GetRecentActivityAsync()
    {
        var recentBookings = await _uow.Bookings.GetQueryable()
            .Include(b => b.TravelerUser)
            .Include(b => b.Buddy).ThenInclude(buddy => buddy!.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentIncidents = await _uow.Incidents.GetQueryable()
            .Include(i => i.ReportedByUser)
            .OrderByDescending(i => i.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentUsers = await _uow.Users.GetQueryable()
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();

        return new RecentActivityDto
        {
            RecentBookings = recentBookings.Select(b => new RecentBookingDto
            {
                Id = b.Id,
                TravelerName = b.TravelerUser?.FullName ?? string.Empty,
                BuddyName = b.Buddy?.User?.FullName ?? string.Empty,
                StatusName = b.Status.ToString(),
                TotalAmount = b.TotalAmount,
                CreatedAt = b.CreatedAt
            }).ToList(),

            RecentIncidents = recentIncidents.Select(i => new RecentIncidentDto
            {
                Id = i.Id,
                TypeName = i.Type.ToString(),
                StatusName = i.Status.ToString(),
                ReporterName = i.ReportedByUser?.FullName ?? string.Empty,
                CreatedAt = i.CreatedAt
            }).ToList(),

            RecentUsers = recentUsers.Select(u => new RecentUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                IsEmailVerified = u.IsEmailVerified,
                CreatedAt = u.CreatedAt
            }).ToList()
        };
    }

    // ─── Platform Health ────────────────────────────────────────────────────

    public async Task<PlatformHealthDto> GetPlatformHealthAsync(DateTime? from, DateTime? to)
    {
        var now = DateTime.UtcNow;
        var thisMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var usersQuery = _uow.Users.GetQueryable();
        var buddiesQuery = _uow.Buddies.GetQueryable();
        var bookingsQuery = _uow.Bookings.GetQueryable();
        var reviewsQuery = _uow.Reviews.GetQueryable();
        var subsQuery = _uow.BuddySubscriptions.GetQueryable();

        var totalUsers = await usersQuery.CountAsync();
        var verifiedUsers = await usersQuery.CountAsync(u => u.IsEmailVerified);

        // Buddy IDs that have at least 1 booking
        var buddyIdsWithBookings = await bookingsQuery
            .Select(b => b.BuddyId)
            .Distinct()
            .ToListAsync();

        var activeBuddies = await buddiesQuery.Where(b => b.IsActive).ToListAsync();
        var avgBuddyRating = activeBuddies.Any()
            ? Math.Round(activeBuddies.Average(b => b.Rate), 2)
            : 0m;

        // Booking completion
        var relevantBookings = await bookingsQuery.CountAsync(b =>
            b.Status != BookingStatus.Cancelled &&
            b.Status != BookingStatus.CancelledByTimeout &&
            b.Status != BookingStatus.Expired);
        var completedBookings = await bookingsQuery.CountAsync(b => b.Status == BookingStatus.Completed);
        var completionRate = relevantBookings > 0
            ? Math.Round((decimal)completedBookings / relevantBookings * 100, 1)
            : 0;

        var paidStatuses = new[]
        {
            BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Completed
        };
        var avgBookingValue = await bookingsQuery
            .Where(b => paidStatuses.Contains(b.Status))
            .AverageAsync(b => (decimal?)b.TotalAmount) ?? 0;

        var cancelledThisMonth = await bookingsQuery.CountAsync(b =>
            (b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.CancelledByTimeout) &&
            b.UpdatedAt >= thisMonthStart);

        // Subscriptions
        var activeSubs = await subsQuery.CountAsync(s => s.Status == SubscriptionStatus.Active);
        var pendingSubs = await subsQuery.CountAsync(s => s.Status == SubscriptionStatus.PendingPayment);

        var subsByPackage = await subsQuery
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Include(s => s.ServicePackage)
            .GroupBy(s => s.ServicePackage!.Name)
            .Select(g => new SubscriptionByPackageDto
            {
                PackageName = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        // Reviews
        var totalReviews = await reviewsQuery.CountAsync();
        var avgReviewRating = totalReviews > 0
            ? Math.Round(await reviewsQuery.AverageAsync(r => (double)r.Rating), 2)
            : 0;

        return new PlatformHealthDto
        {
            UserVerificationRate = totalUsers > 0 ? Math.Round((decimal)verifiedUsers / totalUsers * 100, 1) : 0,
            InactiveUsers = await usersQuery.CountAsync(u => !u.IsActive),
            AverageBuddyRating = avgBuddyRating,
            BuddiesWithoutBookings = activeBuddies.Count(b => !buddyIdsWithBookings.Contains(b.Id)),
            BookingCompletionRate = completionRate,
            AverageBookingValue = Math.Round(avgBookingValue, 2),
            CancelledBookingsThisMonth = cancelledThisMonth,
            ActiveSubscriptions = activeSubs,
            PendingPaymentSubscriptions = pendingSubs,
            SubscriptionByPackage = subsByPackage,
            AverageReviewRating = (decimal)avgReviewRating,
            TotalReviews = totalReviews
        };
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  BUDDY OVERVIEW — Consolidated
    // ═══════════════════════════════════════════════════════════════════════════

    public async Task<BuddyOverviewDto> GetBuddyOverviewAsync(DateTime? from, DateTime? to)
    {
        return new BuddyOverviewDto
        {
            BuddyKpi = await GetBuddyKpiAsync(from, to),
            SubscriptionDistribution = await GetBuddySubscriptionDistributionAsync(),
            Performance = await GetBuddyPerformanceAsync(from, to),
            TopEarners = await GetBuddyTopEarnersAsync(),
            TopRated = await GetBuddyTopRatedAsync(),
            RecentActivity = await GetBuddyRecentActivityAsync(),
            GrowthTrend = await GetBuddyGrowthTrendAsync(),
            RiskAlerts = await GetBuddyRiskAlertsAsync()
        };
    }

    // ─── Buddy KPI ──────────────────────────────────────────────────────────

    public async Task<BuddyKpiDto> GetBuddyKpiAsync(DateTime? from, DateTime? to)
    {
        var (windowFrom, windowTo) = ResolveTimeWindow(from, to);

        var buddiesQuery = _uow.Buddies.GetQueryable();
        var subsQuery = _uow.BuddySubscriptions.GetQueryable();
        var bookingsQuery = _uow.Bookings.GetQueryable();

        var totalBuddies = await buddiesQuery.CountAsync();
        var activeBuddies = await buddiesQuery.CountAsync(b => b.IsActive);
        var buddiesWithSub = await subsQuery
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Select(s => s.BuddyId)
            .Distinct()
            .CountAsync();

        var activeBuddyList = await buddiesQuery.Where(b => b.IsActive && b.Rate > 0).ToListAsync();
        var avgRating = activeBuddyList.Any() ? Math.Round(activeBuddyList.Average(b => b.Rate), 2) : 0m;

        var buddyIdsWithBookings = await bookingsQuery
            .Select(b => b.BuddyId)
            .Distinct()
            .CountAsync();

        return new BuddyKpiDto
        {
            TotalBuddies = totalBuddies,
            ActiveBuddies = activeBuddies,
            InactiveBuddies = totalBuddies - activeBuddies,
            NewBuddiesThisMonth = await buddiesQuery.CountAsync(b =>
                b.CreatedAt >= windowFrom && b.CreatedAt <= windowTo),
            BuddiesWithSubscription = buddiesWithSub,
            BuddiesWithoutSubscription = totalBuddies - buddiesWithSub,
            AverageRating = avgRating,
            BuddiesWithPerfectRating = await buddiesQuery.CountAsync(b => b.Rate == 5.0m),
            BuddiesWithBookings = buddyIdsWithBookings,
            BuddiesWithoutBookings = totalBuddies - buddyIdsWithBookings
        };
    }

    // ─── Buddy Subscription Distribution ────────────────────────────────────

    public async Task<BuddySubscriptionDistributionDto> GetBuddySubscriptionDistributionAsync()
    {
        var totalBuddies = await _uow.Buddies.GetQueryable().CountAsync();

        var activeSubs = await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Include(s => s.ServicePackage)
            .ToListAsync();

        var totalSubscribed = activeSubs.Select(s => s.BuddyId).Distinct().Count();

        var packages = activeSubs
            .GroupBy(s => new
            {
                s.ServicePackageId,
                s.ServicePackage!.Name,
                s.ServicePackage.PricePerMonth,
                s.ServicePackage.Currency,
                s.ServicePackage.CommissionRate
            })
            .Select(g => new PackageDistributionDto
            {
                PackageId = g.Key.ServicePackageId,
                PackageName = g.Key.Name,
                PricePerMonth = g.Key.PricePerMonth,
                Currency = g.Key.Currency,
                CommissionRate = g.Key.CommissionRate,
                ActiveCount = g.Count(),
                Percentage = totalSubscribed > 0
                    ? Math.Round((decimal)g.Count() / totalSubscribed * 100, 1)
                    : 0
            })
            .ToList();

        return new BuddySubscriptionDistributionDto
        {
            TotalSubscribed = totalSubscribed,
            TotalFree = totalBuddies - totalSubscribed,
            Packages = packages
        };
    }

    // ─── Buddy Performance ──────────────────────────────────────────────────

    public async Task<BuddyPerformanceDto> GetBuddyPerformanceAsync(DateTime? from, DateTime? to)
    {
        var (windowFrom, windowTo) = ResolveTimeWindow(from, to);

        var tripRequestsQuery = _uow.TripRequests.GetQueryable();
        var bookingsQuery = _uow.Bookings.GetQueryable();
        var reviewsQuery = _uow.Reviews.GetQueryable();
        var incidentsQuery = _uow.Incidents.GetQueryable();

        var totalTripRequests = await tripRequestsQuery.CountAsync();
        var tripRequestsThisMonth = await tripRequestsQuery
            .CountAsync(r => r.CreatedAt >= windowFrom && r.CreatedAt <= windowTo);

        var totalBookings = await bookingsQuery.CountAsync();
        var completedBookings = await bookingsQuery.CountAsync(b => b.Status == BookingStatus.Completed);
        var cancelledBookings = await bookingsQuery.CountAsync(b =>
            b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.CancelledByTimeout);
        var activeBookings = await bookingsQuery.CountAsync(b =>
            b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.InProgress);

        var relevantBookings = totalBookings - cancelledBookings;
        var completionRate = relevantBookings > 0
            ? Math.Round((decimal)completedBookings / relevantBookings * 100, 1)
            : 0;

        // Earnings: sum of Price for completed bookings (what buddies earn)
        var totalEarnings = await bookingsQuery
            .Where(b => b.Status == BookingStatus.Completed)
            .SumAsync(b => (decimal?)b.Price) ?? 0;

        var totalPlatformFees = await bookingsQuery
            .Where(b => b.Status == BookingStatus.Completed)
            .SumAsync(b => (decimal?)b.PlatformFeeAmount) ?? 0;

        var paidStatuses = new[] { BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Completed };
        var avgValue = await bookingsQuery
            .Where(b => paidStatuses.Contains(b.Status))
            .AverageAsync(b => (decimal?)b.TotalAmount) ?? 0;

        var totalReviews = await reviewsQuery.CountAsync();
        var avgRating = totalReviews > 0
            ? Math.Round(await reviewsQuery.AverageAsync(r => (double)r.Rating), 2)
            : 0;

        var totalIncidents = await incidentsQuery.CountAsync();
        var openIncidents = await incidentsQuery.CountAsync(i =>
            i.Status == IncidentStatus.Open || i.Status == IncidentStatus.InReview);

        return new BuddyPerformanceDto
        {
            TotalTripRequestsSent = totalTripRequests,
            TripRequestsThisMonth = tripRequestsThisMonth,
            TotalBookings = totalBookings,
            CompletedBookings = completedBookings,
            CancelledBookings = cancelledBookings,
            ActiveBookings = activeBookings,
            BookingCompletionRate = completionRate,
            TotalEarnings = totalEarnings,
            TotalPlatformFees = totalPlatformFees,
            AverageBookingValue = Math.Round(avgValue, 2),
            TotalReviews = totalReviews,
            AverageReviewRating = (decimal)avgRating,
            TotalIncidents = totalIncidents,
            OpenIncidents = openIncidents
        };
    }

    // ─── Buddy Top Earners ──────────────────────────────────────────────────

    public async Task<List<BuddyTopEarnerDto>> GetBuddyTopEarnersAsync(int top = 10)
    {
        var topEarners = await _uow.Bookings.GetQueryable()
            .Where(b => b.Status == BookingStatus.Completed)
            .GroupBy(b => b.BuddyId)
            .Select(g => new
            {
                BuddyId = g.Key,
                TotalEarnings = g.Sum(b => b.Price),
                CompletedBookings = g.Count()
            })
            .OrderByDescending(x => x.TotalEarnings)
            .Take(top)
            .ToListAsync();

        var buddyIds = topEarners.Select(e => e.BuddyId).ToList();
        var buddies = await _uow.Buddies.GetQueryable()
            .Include(b => b.User)
            .Where(b => buddyIds.Contains(b.Id))
            .ToListAsync();

        var activeSubs = await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active && buddyIds.Contains(s.BuddyId))
            .Include(s => s.ServicePackage)
            .ToListAsync();

        return topEarners.Select(e =>
        {
            var buddy = buddies.FirstOrDefault(b => b.Id == e.BuddyId);
            var sub = activeSubs.FirstOrDefault(s => s.BuddyId == e.BuddyId);
            return new BuddyTopEarnerDto
            {
                BuddyId = e.BuddyId,
                FullName = buddy?.User?.FullName ?? string.Empty,
                ProfilePicture = buddy?.User?.ProfilePicture,
                TotalEarnings = e.TotalEarnings,
                CompletedBookings = e.CompletedBookings,
                Rating = buddy?.Rate ?? 0,
                CurrentPackage = sub?.ServicePackage?.Name
            };
        }).ToList();
    }

    // ─── Buddy Top Rated ────────────────────────────────────────────────────

    public async Task<List<BuddyTopRatedDto>> GetBuddyTopRatedAsync(int top = 10)
    {
        var topRated = await _uow.Buddies.GetQueryable()
            .Include(b => b.User)
            .Where(b => b.IsActive && b.Rate > 0)
            .OrderByDescending(b => b.Rate)
            .Take(top)
            .ToListAsync();

        var buddyIds = topRated.Select(b => b.Id).ToList();

        // Count reviews per buddy
        var reviewCounts = await _uow.Bookings.GetQueryable()
            .Where(b => buddyIds.Contains(b.BuddyId))
            .Join(_uow.Reviews.GetQueryable(),
                b => b.Id, r => r.BookingId,
                (b, r) => new { b.BuddyId })
            .GroupBy(x => x.BuddyId)
            .Select(g => new { BuddyId = g.Key, Count = g.Count() })
            .ToListAsync();

        // Count completed bookings per buddy
        var completedCounts = await _uow.Bookings.GetQueryable()
            .Where(b => buddyIds.Contains(b.BuddyId) && b.Status == BookingStatus.Completed)
            .GroupBy(b => b.BuddyId)
            .Select(g => new { BuddyId = g.Key, Count = g.Count() })
            .ToListAsync();

        var activeSubs = await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active && buddyIds.Contains(s.BuddyId))
            .Include(s => s.ServicePackage)
            .ToListAsync();

        return topRated.Select(b =>
        {
            var sub = activeSubs.FirstOrDefault(s => s.BuddyId == b.Id);
            return new BuddyTopRatedDto
            {
                BuddyId = b.Id,
                FullName = b.User?.FullName ?? string.Empty,
                ProfilePicture = b.User?.ProfilePicture,
                Rating = b.Rate,
                TotalReviews = reviewCounts.FirstOrDefault(r => r.BuddyId == b.Id)?.Count ?? 0,
                CompletedBookings = completedCounts.FirstOrDefault(c => c.BuddyId == b.Id)?.Count ?? 0,
                CurrentPackage = sub?.ServicePackage?.Name
            };
        }).ToList();
    }

    // ─── Buddy Recent Activity ──────────────────────────────────────────────

    public async Task<BuddyRecentActivityDto> GetBuddyRecentActivityAsync()
    {
        var recentRegistrations = await _uow.Buddies.GetQueryable()
            .Include(b => b.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentBookings = await _uow.Bookings.GetQueryable()
            .Include(b => b.Buddy).ThenInclude(buddy => buddy!.User)
            .Include(b => b.TravelerUser)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentSubs = await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active && s.PaidAt != null)
            .Include(s => s.Buddy).ThenInclude(b => b.User)
            .Include(s => s.ServicePackage)
            .OrderByDescending(s => s.PaidAt)
            .Take(5)
            .ToListAsync();

        return new BuddyRecentActivityDto
        {
            RecentRegistrations = recentRegistrations.Select(b => new RecentBuddyRegistrationDto
            {
                BuddyId = b.Id,
                FullName = b.User?.FullName ?? string.Empty,
                Email = b.User?.Email ?? string.Empty,
                RegisteredAt = b.CreatedAt
            }).ToList(),

            RecentBookings = recentBookings.Select(b => new RecentBuddyBookingDto
            {
                BookingId = b.Id,
                BuddyName = b.Buddy?.User?.FullName ?? string.Empty,
                TravelerName = b.TravelerUser?.FullName ?? string.Empty,
                StatusName = b.Status.ToString(),
                TotalAmount = b.TotalAmount,
                CreatedAt = b.CreatedAt
            }).ToList(),

            RecentSubscriptions = recentSubs.Select(s => new RecentBuddySubscriptionDto
            {
                BuddyId = s.BuddyId,
                BuddyName = s.Buddy?.User?.FullName ?? string.Empty,
                PackageName = s.ServicePackage?.Name ?? string.Empty,
                AmountPaid = s.AmountPaid,
                Currency = s.Currency,
                PaidAt = s.PaidAt
            }).ToList()
        };
    }

    // ─── Buddy Growth Trend (fixed 4 weeks) ─────────────────────────────────

    public async Task<BuddyGrowthTrendDto> GetBuddyGrowthTrendAsync()
    {
        var now = DateTime.UtcNow;
        var daysSinceMonday = ((int)now.DayOfWeek + 6) % 7;
        var currentWeekStart = now.Date.AddDays(-daysSinceMonday);
        var fourWeeksAgo = currentWeekStart.AddDays(-21);

        var newBuddies = await _uow.Buddies.GetQueryable()
            .Where(b => b.CreatedAt >= fourWeeksAgo)
            .Select(b => b.CreatedAt)
            .ToListAsync();

        var newSubs = await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active && s.PaidAt >= fourWeeksAgo)
            .Select(s => s.PaidAt!.Value)
            .ToListAsync();

        var completedBookings = await _uow.Bookings.GetQueryable()
            .Where(b => b.Status == BookingStatus.Completed && b.UpdatedAt >= fourWeeksAgo)
            .Select(b => b.UpdatedAt!.Value)
            .ToListAsync();

        var weeks = new List<WeeklyBuddyGrowthDto>();
        for (int i = 0; i < 4; i++)
        {
            var weekStart = fourWeeksAgo.AddDays(i * 7);
            var weekEnd = weekStart.AddDays(7);

            weeks.Add(new WeeklyBuddyGrowthDto
            {
                WeekLabel = $"Week {i + 1}",
                WeekStart = weekStart,
                WeekEnd = weekEnd,
                NewBuddies = newBuddies.Count(d => d >= weekStart && d < weekEnd),
                NewSubscriptions = newSubs.Count(d => d >= weekStart && d < weekEnd),
                CompletedBookings = completedBookings.Count(d => d >= weekStart && d < weekEnd)
            });
        }

        return new BuddyGrowthTrendDto { Weeks = weeks };
    }

    // ─── Buddy Risk Alerts ──────────────────────────────────────────────────

    public async Task<BuddyRiskAlertsDto> GetBuddyRiskAlertsAsync()
    {
        var now = DateTime.UtcNow;

        // 1. Idle buddies: active but zero bookings ever
        var buddyIdsWithBookings = await _uow.Bookings.GetQueryable()
            .Select(b => b.BuddyId)
            .Distinct()
            .ToListAsync();

        var idleBuddies = await _uow.Buddies.GetQueryable()
            .Include(b => b.User)
            .Where(b => b.IsActive && !buddyIdsWithBookings.Contains(b.Id))
            .Take(10)
            .ToListAsync();

        // 2. Low rated buddies (rating < 3.0 and has rating)
        var lowRated = await _uow.Buddies.GetQueryable()
            .Include(b => b.User)
            .Where(b => b.IsActive && b.Rate > 0 && b.Rate < 3.0m)
            .OrderBy(b => b.Rate)
            .Take(10)
            .ToListAsync();

        // 3. Buddies with open incidents
        var buddyIdsWithOpenIncidents = await _uow.Incidents.GetQueryable()
            .Where(i => i.Status == IncidentStatus.Open || i.Status == IncidentStatus.InReview)
            .Include(i => i.Booking)
            .Select(i => i.Booking!.BuddyId)
            .Distinct()
            .ToListAsync();

        var buddiesWithIncidents = await _uow.Buddies.GetQueryable()
            .Include(b => b.User)
            .Where(b => buddyIdsWithOpenIncidents.Contains(b.Id))
            .Take(10)
            .ToListAsync();

        // 4. Expiring subscriptions (within 7 days)
        var expiringDate = now.AddDays(7);
        var expiringSubs = await _uow.BuddySubscriptions.GetQueryable()
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate <= expiringDate && s.EndDate > now)
            .Include(s => s.Buddy).ThenInclude(b => b.User)
            .Include(s => s.ServicePackage)
            .Take(10)
            .ToListAsync();

        return new BuddyRiskAlertsDto
        {
            IdleBuddies = idleBuddies.Select(b => new BuddyAlertDto
            {
                BuddyId = b.Id,
                FullName = b.User?.FullName ?? string.Empty,
                ProfilePicture = b.User?.ProfilePicture,
                AlertReason = "Active buddy with zero bookings",
                Rating = b.Rate,
                RelevantDate = b.CreatedAt
            }).ToList(),

            LowRatedBuddies = lowRated.Select(b => new BuddyAlertDto
            {
                BuddyId = b.Id,
                FullName = b.User?.FullName ?? string.Empty,
                ProfilePicture = b.User?.ProfilePicture,
                AlertReason = $"Low rating: {b.Rate:0.0}/5.0",
                Rating = b.Rate,
                RelevantDate = b.UpdatedAt ?? b.CreatedAt
            }).ToList(),

            BuddiesWithOpenIncidents = buddiesWithIncidents.Select(b => new BuddyAlertDto
            {
                BuddyId = b.Id,
                FullName = b.User?.FullName ?? string.Empty,
                ProfilePicture = b.User?.ProfilePicture,
                AlertReason = "Has unresolved incident(s)",
                Rating = b.Rate
            }).ToList(),

            ExpiringSubscriptions = expiringSubs.Select(s => new BuddyAlertDto
            {
                BuddyId = s.BuddyId,
                FullName = s.Buddy?.User?.FullName ?? string.Empty,
                ProfilePicture = s.Buddy?.User?.ProfilePicture,
                AlertReason = $"Subscription '{s.ServicePackage?.Name}' expires {s.EndDate:yyyy-MM-dd}",
                Rating = s.Buddy?.Rate,
                RelevantDate = s.EndDate
            }).ToList()
        };
    }
}
