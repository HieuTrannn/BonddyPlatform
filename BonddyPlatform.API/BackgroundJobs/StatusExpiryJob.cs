using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.API.BackgroundJobs;

/// <summary>
/// Background job chạy định kỳ mỗi 5 phút để tự động cập nhật status:
/// - Trip (Open/Closed) → Expired khi StartDate+StartTime đã qua.
/// - Booking (PendingCustomerConfirm/PendingPayment) → Expired khi BookedDate+BookedStartTime đã qua.
/// - Booking (Confirmed) → InProgress khi BookedDate+BookedStartTime đã tới.
/// - Booking (InProgress) → Completed khi BookedDate+BookedStartTime+BookedDurationHours đã qua.
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
            await Task.Delay(Interval, stoppingToken);

            try
            {
                await RunAsync(stoppingToken);
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

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BonddyDbContext>();

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        // ── 1. Expire Trips ────────────────────────────────────────────────────
        var tripsToExpire = await db.Trips
            .Where(t => (t.Status == TripStatus.Open || t.Status == TripStatus.Closed)
                     && (t.StartDate < today
                         || (t.StartDate == today && t.StartTime <= currentTime)))
            .ToListAsync(ct);

        foreach (var trip in tripsToExpire)
        {
            trip.Status    = TripStatus.Expired;
            trip.UpdatedAt = now;
        }

        // ── 2. Expire Bookings (chưa confirm / chưa thanh toán) ───────────────
        var bookingsToExpire = await db.Bookings
            .Where(b => (b.Status == BookingStatus.PendingCustomerConfirm
                      || b.Status == BookingStatus.PendingPayment)
                     && (b.BookedDate < today
                         || (b.BookedDate == today && b.BookedStartTime <= currentTime)))
            .ToListAsync(ct);

        foreach (var booking in bookingsToExpire)
        {
            booking.Status    = BookingStatus.Expired;
            booking.UpdatedAt = now;
        }

        // ── 3. Confirmed → InProgress khi đến giờ đi ──────────────────────────
        var bookingsToStart = await db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed
                     && (b.BookedDate < today
                         || (b.BookedDate == today && b.BookedStartTime <= currentTime)))
            .ToListAsync(ct);

        foreach (var booking in bookingsToStart)
        {
            booking.Status    = BookingStatus.InProgress;
            booking.UpdatedAt = now;
        }

        // ── 4. InProgress → Completed khi hết thời gian tour ─────────────────
        // Thời gian kết thúc = BookedDate+BookedStartTime + BookedDurationHours
        var bookingsInProgress = await db.Bookings
            .Where(b => b.Status == BookingStatus.InProgress)
            .ToListAsync(ct);

        var bookingsToComplete = bookingsInProgress
            .Where(b =>
            {
                // Tính DateTime kết thúc tour (UTC naive — cộng giờ đơn giản)
                var startDt = b.BookedDate.ToDateTime(b.BookedStartTime);
                var endDt   = startDt.AddHours(b.BookedDurationHours);
                return now >= endDt;
            })
            .ToList();

        foreach (var booking in bookingsToComplete)
        {
            booking.Status    = BookingStatus.Completed;
            booking.UpdatedAt = now;
        }

        var totalChanged = tripsToExpire.Count + bookingsToExpire.Count
                         + bookingsToStart.Count + bookingsToComplete.Count;

        if (totalChanged > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "StatusExpiryJob: {Expired} trip(s) expired | {BookingExpired} booking(s) expired | " +
                "{Started} booking(s) → InProgress | {Completed} booking(s) → Completed.",
                tripsToExpire.Count, bookingsToExpire.Count,
                bookingsToStart.Count, bookingsToComplete.Count);
        }
    }
}
