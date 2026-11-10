using BonddyPlatform.API.BackgroundJobs;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Tests;

public class StatusExpiryJobTests : IDisposable
{
    // Fixed "now" so every test is deterministic: 10 Mar 2026, 14:00 UTC.
    private static readonly DateTime Now = new(2026, 3, 10, 14, 0, 0, DateTimeKind.Utc);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now);

    private readonly BonddyDbContext _db = new(
        new DbContextOptionsBuilder<BonddyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    public void Dispose() => _db.Dispose();

    private Booking AddBooking(BookingStatus status, DateOnly date, TimeOnly start, int durationHours = 3)
    {
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            Status = status,
            BookedDate = date,
            BookedStartTime = start,
            BookedDurationHours = durationHours
        };
        _db.Bookings.Add(booking);
        _db.SaveChanges();
        return booking;
    }

    private async Task<StatusExpiryJob.TransitionResult> RunAsync(DateTime? now = null)
    {
        var result = await StatusExpiryJob.ApplyTransitionsAsync(_db, now ?? Now);
        _db.ChangeTracker.Clear(); // re-read from the store, like the next job tick would
        return result;
    }

    private BookingStatus StatusOf(Booking b) => _db.Bookings.AsNoTracking().Single(x => x.Id == b.Id).Status;

    [Theory]
    [InlineData(BookingStatus.PendingCustomerConfirm)]
    [InlineData(BookingStatus.PendingPayment)]
    public async Task Unpaid_booking_expires_once_the_tour_start_has_passed(BookingStatus status)
    {
        var yesterday = AddBooking(status, Today.AddDays(-1), new TimeOnly(9, 0));
        var earlierToday = AddBooking(status, Today, new TimeOnly(13, 59));

        var result = await RunAsync();

        Assert.Equal(BookingStatus.Expired, StatusOf(yesterday));
        Assert.Equal(BookingStatus.Expired, StatusOf(earlierToday));
        Assert.Equal(2, result.BookingsExpired);
    }

    [Fact]
    public async Task Unpaid_booking_starting_exactly_now_expires()
    {
        var booking = AddBooking(BookingStatus.PendingPayment, Today, new TimeOnly(14, 0));

        await RunAsync();

        Assert.Equal(BookingStatus.Expired, StatusOf(booking));
    }

    [Theory]
    [InlineData(BookingStatus.PendingCustomerConfirm)]
    [InlineData(BookingStatus.PendingPayment)]
    [InlineData(BookingStatus.Confirmed)]
    public async Task Future_booking_is_left_alone(BookingStatus status)
    {
        var laterToday = AddBooking(status, Today, new TimeOnly(14, 1));
        var tomorrow = AddBooking(status, Today.AddDays(1), new TimeOnly(8, 0));

        var result = await RunAsync();

        Assert.Equal(status, StatusOf(laterToday));
        Assert.Equal(status, StatusOf(tomorrow));
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task Confirmed_booking_moves_to_InProgress_when_the_tour_starts()
    {
        var booking = AddBooking(BookingStatus.Confirmed, Today, new TimeOnly(13, 0), durationHours: 4);

        var result = await RunAsync();

        Assert.Equal(BookingStatus.InProgress, StatusOf(booking));
        Assert.Equal(1, result.BookingsStarted);
    }

    [Fact]
    public async Task InProgress_booking_completes_only_after_start_plus_duration()
    {
        var finished = AddBooking(BookingStatus.InProgress, Today, new TimeOnly(10, 0), durationHours: 4); // ended 14:00
        var running = AddBooking(BookingStatus.InProgress, Today, new TimeOnly(11, 0), durationHours: 4);  // ends 15:00

        var result = await RunAsync();

        Assert.Equal(BookingStatus.Completed, StatusOf(finished));
        Assert.Equal(BookingStatus.InProgress, StatusOf(running));
        Assert.Equal(1, result.BookingsCompleted);
    }

    [Fact]
    public async Task Confirmed_booking_whose_tour_already_ended_goes_through_InProgress_on_the_next_tick()
    {
        var booking = AddBooking(BookingStatus.Confirmed, Today.AddDays(-2), new TimeOnly(9, 0));

        await RunAsync();
        Assert.Equal(BookingStatus.InProgress, StatusOf(booking));

        await RunAsync(Now.AddMinutes(5));
        Assert.Equal(BookingStatus.Completed, StatusOf(booking));
    }

    [Theory]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.CancelledByTimeout)]
    [InlineData(BookingStatus.Expired)]
    public async Task Terminal_statuses_are_never_changed(BookingStatus status)
    {
        var booking = AddBooking(status, Today.AddDays(-5), new TimeOnly(9, 0));

        var result = await RunAsync();

        Assert.Equal(status, StatusOf(booking));
        Assert.Equal(0, result.Total);
    }

    [Fact]
    public async Task Paid_booking_is_never_expired()
    {
        // A Confirmed (paid) booking in the past must start, not be expired like an unpaid one.
        var booking = AddBooking(BookingStatus.Confirmed, Today.AddDays(-1), new TimeOnly(20, 0));

        await RunAsync();

        Assert.NotEqual(BookingStatus.Expired, StatusOf(booking));
    }

    [Fact]
    public async Task Open_and_closed_trips_expire_after_their_start_time()
    {
        var past = new Trip { Id = Guid.NewGuid(), City = "Hoi An", Status = TripStatus.Open, StartDate = Today, StartTime = new TimeOnly(8, 0) };
        var closedPast = new Trip { Id = Guid.NewGuid(), City = "Hue", Status = TripStatus.Closed, StartDate = Today.AddDays(-1), StartTime = new TimeOnly(8, 0) };
        var future = new Trip { Id = Guid.NewGuid(), City = "Da Nang", Status = TripStatus.Open, StartDate = Today.AddDays(1), StartTime = new TimeOnly(8, 0) };
        _db.Trips.AddRange(past, closedPast, future);
        _db.SaveChanges();

        var result = await RunAsync();

        Assert.Equal(2, result.TripsExpired);
        Assert.Equal(TripStatus.Open, _db.Trips.AsNoTracking().Single(t => t.Id == future.Id).Status);
        Assert.Equal(TripStatus.Expired, _db.Trips.AsNoTracking().Single(t => t.Id == past.Id).Status);
    }

    [Fact]
    public async Task Running_twice_is_idempotent()
    {
        AddBooking(BookingStatus.PendingPayment, Today.AddDays(-1), new TimeOnly(9, 0));
        AddBooking(BookingStatus.InProgress, Today.AddDays(-1), new TimeOnly(9, 0));

        var first = await RunAsync();
        var second = await RunAsync();

        Assert.Equal(2, first.Total);
        Assert.Equal(0, second.Total);
    }
}
