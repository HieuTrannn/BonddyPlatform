using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.API.BackgroundJobs;

/// <summary>
/// Background job chạy định kỳ mỗi 5 phút để tự động cập nhật status:
/// - Trip (Open/Closed) → Expired khi StartDate+StartTime đã qua.
/// - Booking (PendingCustomerConfirm/PendingPayment) → Expired khi BookedDate+BookedStartTime đã qua.
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
        // Chỉ xét Trip đang Open hoặc Closed (Draft/Expired/Deleted bỏ qua).
        // Điều kiện: StartDate < today  HOẶC  (StartDate == today AND StartTime <= currentTime)
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

        // ── 2. Expire Bookings ─────────────────────────────────────────────────
        // Chỉ xét Booking chưa được xử lý (chưa confirm / chưa thanh toán).
        // Điều kiện: BookedDate+BookedStartTime đã qua.
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

        if (tripsToExpire.Count > 0 || bookingsToExpire.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            _logger.LogInformation(
                "StatusExpiryJob: {TripCount} trip(s) and {BookingCount} booking(s) marked Expired.",
                tripsToExpire.Count, bookingsToExpire.Count);
        }
    }
}
