using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.API.BackgroundJobs;

/// <summary>
/// Runs every 5 minutes and moves time-driven statuses forward:
/// - Trip (Open/Closed) → Expired once StartDate+StartTime has passed.
/// - Booking (PendingCustomerConfirm/PendingPayment) → Expired once BookedDate+BookedStartTime has passed.
/// - Booking (Confirmed) → InProgress once BookedDate+BookedStartTime is reached.
/// - Booking (InProgress) → Completed once BookedDate+BookedStartTime+BookedDurationHours has passed.
/// All times are compared in UTC.
/// </summary>
public class StatusExpiryJob : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StatusExpiryJob> _logger;

    public StatusExpiryJob(IServiceScopeFactory scopeFactory, ILogger<StatusExpiryJob> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StatusExpiryJob started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(Interval, stoppingToken);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<BonddyDbContext>();
                var result = await ApplyTransitionsAsync(db, DateTime.UtcNow, stoppingToken);

                if (result.Total > 0)
                {
                    _logger.LogInformation(
                        "StatusExpiryJob: {Expired} trip(s) expired | {BookingExpired} booking(s) expired | " +
                        "{Started} booking(s) → InProgress | {Completed} booking(s) → Completed.",
                        result.TripsExpired, result.BookingsExpired, result.BookingsStarted, result.BookingsCompleted);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in StatusExpiryJob.");
            }
        }

        _logger.LogInformation("StatusExpiryJob stopped.");
    }

    public readonly record struct TransitionResult(
        int TripsExpired, int BookingsExpired, int BookingsStarted, int BookingsCompleted)
    {
        public int Total => TripsExpired + BookingsExpired + BookingsStarted + BookingsCompleted;
    }

    /// <summary>Applies all time-based status transitions as of <paramref name="now"/> (UTC) and saves once.</summary>
    public static async Task<TransitionResult> ApplyTransitionsAsync(BonddyDbContext db, DateTime now, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        // 1. Trips whose start time has passed
        var tripsToExpire = await db.Trips
            .Where(t => (t.Status == TripStatus.Open || t.Status == TripStatus.Closed)
                     && (t.StartDate < today
                         || (t.StartDate == today && t.StartTime <= currentTime)))
            .ToListAsync(ct);

        foreach (var trip in tripsToExpire)
        {
            trip.Status = TripStatus.Expired;
            trip.UpdatedAt = now;
        }

        // 2. Bookings never confirmed/paid before the tour started
        var bookingsToExpire = await db.Bookings
            .Where(b => (b.Status == BookingStatus.PendingCustomerConfirm
                      || b.Status == BookingStatus.PendingPayment)
                     && (b.BookedDate < today
                         || (b.BookedDate == today && b.BookedStartTime <= currentTime)))
            .ToListAsync(ct);

        foreach (var booking in bookingsToExpire)
        {
            booking.Status = BookingStatus.Expired;
            booking.UpdatedAt = now;
        }

        // 3. Confirmed → InProgress when the tour starts
        var bookingsToStart = await db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed
                     && (b.BookedDate < today
                         || (b.BookedDate == today && b.BookedStartTime <= currentTime)))
            .ToListAsync(ct);

        foreach (var booking in bookingsToStart)
        {
            booking.Status = BookingStatus.InProgress;
            booking.UpdatedAt = now;
        }

        // 4. InProgress → Completed when start + duration has passed (evaluated in memory: date math is not translatable).
        var bookingsInProgress = await db.Bookings
            .Where(b => b.Status == BookingStatus.InProgress)
            .ToListAsync(ct);

        var bookingsToComplete = bookingsInProgress
            .Where(b => now >= b.BookedDate.ToDateTime(b.BookedStartTime).AddHours(b.BookedDurationHours))
            .ToList();

        foreach (var booking in bookingsToComplete)
        {
            booking.Status = BookingStatus.Completed;
            booking.UpdatedAt = now;
        }

        var result = new TransitionResult(
            tripsToExpire.Count, bookingsToExpire.Count, bookingsToStart.Count, bookingsToComplete.Count);

        if (result.Total > 0)
            await db.SaveChangesAsync(ct);

        return result;
    }
}
