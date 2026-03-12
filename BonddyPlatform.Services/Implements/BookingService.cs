using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.BookingDtos;
using BonddyPlatform.Services.Interfaces;

namespace BonddyPlatform.Services.Implements;

public class BookingService : IBookingService
{
    private readonly IUnitOfWork _uow;

    // Transition rules: key = current status, value = allowed next statuses
    private static readonly Dictionary<BookingStatus, HashSet<BookingStatus>> _allowedTransitions = new()
    {
        [BookingStatus.Pending]    = new() { BookingStatus.Confirmed, BookingStatus.Cancelled },
        [BookingStatus.Confirmed]  = new() { BookingStatus.InProgress, BookingStatus.Cancelled },
        [BookingStatus.InProgress] = new() { BookingStatus.Completed, BookingStatus.Cancelled },
        [BookingStatus.Completed]  = new(),
        [BookingStatus.Cancelled]  = new()
    };

    public BookingService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<BookingResponseDto> CreateForTripAsync(Guid createdByUserId, BookingCreateRequestDto dto)
    {
        var surchargesTotal = dto.Surcharges.Sum(s => s.Amount);
        var subtotal = dto.BaseRatePerHour * (dto.DurationMinutes / 60m);
        var total = subtotal + surchargesTotal + dto.PlatformFeeAmount + dto.TipAmount;

        var booking = new Booking
        {
            TripId = dto.TripId,
            BuddyId = dto.BuddyId,
            CreatedByUserId = createdByUserId,
            BaseRatePerHour = dto.BaseRatePerHour,
            DurationMinutes = dto.DurationMinutes,
            SubtotalAmount = subtotal,
            SurchargesAmount = surchargesTotal,
            PlatformFeeAmount = dto.PlatformFeeAmount,
            TipAmount = dto.TipAmount,
            TotalAmount = total,
            ScheduledStartTime = dto.ScheduledStartTime,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            Surcharges = dto.Surcharges.Select(s => new BookingSurcharge
            {
                Type = s.Type,
                Amount = s.Amount,
                Note = s.Note
            }).ToList()
        };

        await _uow.Bookings.AddAsync(booking);

        // Sync Trip status to InProgress when first booking is created
        var trip = await _uow.Trips.GetByIdAsync(dto.TripId);
        if (trip is not null && trip.Status == TripStatus.Submitted)
        {
            trip.Status = TripStatus.InProgress;
            trip.UpdatedAt = DateTime.UtcNow;
            _uow.Trips.Update(trip);
        }

        await _uow.SaveChangesAsync();

        var full = await _uow.Bookings.GetByIdWithDetailsAsync(booking.Id);
        return ToDto(full!);
    }

    public async Task<IEnumerable<BookingResponseDto>> GetByTripIdAsync(Guid tripId)
    {
        var bookings = await _uow.Bookings.GetByTripIdAsync(tripId);
        return bookings.Select(ToDto);
    }

    public async Task<BookingResponseDto?> GetByIdAsync(Guid id)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(id);
        return booking is null ? null : ToDto(booking);
    }

    public async Task<BookingResponseDto?> UpdateStatusAsync(Guid id, BookingStatus newStatus)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(id);
        if (booking is null) return null;

        if (!_allowedTransitions.TryGetValue(booking.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Cannot transition booking from {booking.Status} to {newStatus}.");

        booking.Status = newStatus;
        booking.UpdatedAt = DateTime.UtcNow;

        // Sync Trip status when booking is Completed or Cancelled
        var trip = await _uow.Trips.GetByIdAsync(booking.TripId);
        if (trip is not null)
        {
            if (newStatus == BookingStatus.Completed)
            {
                trip.Status = TripStatus.Completed;
                trip.UpdatedAt = DateTime.UtcNow;
                _uow.Trips.Update(trip);
            }
            else if (newStatus == BookingStatus.Cancelled)
            {
                // Only revert Trip to Submitted if no other active bookings
                var otherActive = (await _uow.Bookings.GetByTripIdAsync(trip.Id))
                    .Any(b => b.Id != id && b.Status is BookingStatus.Pending
                                         or BookingStatus.Confirmed
                                         or BookingStatus.InProgress);
                if (!otherActive && trip.Status == TripStatus.InProgress)
                {
                    trip.Status = TripStatus.Submitted;
                    trip.UpdatedAt = DateTime.UtcNow;
                    _uow.Trips.Update(trip);
                }
            }
        }

        _uow.Bookings.Update(booking);
        await _uow.SaveChangesAsync();
        return ToDto(booking);
    }

    private static BookingResponseDto ToDto(Booking b) => new()
    {
        Id = b.Id,
        TripId = b.TripId,
        TripCity = b.Trip?.City ?? string.Empty,
        BuddyId = b.BuddyId,
        BuddyName = b.Buddy?.User?.FullName ?? string.Empty,
        CreatedByUserId = b.CreatedByUserId,
        CreatedByUserName = b.CreatedByUser?.FullName ?? string.Empty,
        BaseRatePerHour = b.BaseRatePerHour,
        DurationMinutes = b.DurationMinutes,
        SubtotalAmount = b.SubtotalAmount,
        SurchargesAmount = b.SurchargesAmount,
        PlatformFeeAmount = b.PlatformFeeAmount,
        TipAmount = b.TipAmount,
        TotalAmount = b.TotalAmount,
        ScheduledStartTime = b.ScheduledStartTime,
        Status = b.Status,
        Surcharges = b.Surcharges.Select(s => new BookingSurchargeResponseDto
        {
            Id = s.Id,
            Type = s.Type,
            Amount = s.Amount,
            Note = s.Note
        }).ToList(),
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}
