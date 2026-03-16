using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.BookingDtos;
using BonddyPlatform.Services.Interfaces;

namespace BonddyPlatform.Services.Implements;

public class BookingService : IBookingService
{
    private const double PlatformFeeRate = 0.10;          // 10% platform fee
    private const int PaymentDeadlineHours = 24;           // 24h for traveler to pay

    private readonly IUnitOfWork _uow;
    private readonly IChatService _chatService;

    // State machine: key = current status, value = allowed next statuses
    private static readonly Dictionary<BookingStatus, HashSet<BookingStatus>> AllowedTransitions = new()
    {
        [BookingStatus.PendingCustomerConfirm] = new() { BookingStatus.Cancelled },
        [BookingStatus.PendingPayment]         = new() { BookingStatus.Confirmed, BookingStatus.CancelledByTimeout, BookingStatus.Cancelled },
        [BookingStatus.Confirmed]              = new() { BookingStatus.InProgress, BookingStatus.Cancelled },
        [BookingStatus.InProgress]             = new() { BookingStatus.Completed, BookingStatus.Cancelled },
        [BookingStatus.Completed]              = new(),
        [BookingStatus.Cancelled]              = new(),
        [BookingStatus.CancelledByTimeout]     = new()
    };

    public BookingService(IUnitOfWork uow, IChatService chatService)
    {
        _uow = uow;
        _chatService = chatService;
    }

    // ─── CreateFromTripRequestAsync ──────────────────────────────────────────

    public async Task<BookingResponseDto> CreateFromTripRequestAsync(
        Guid buddyUserId, Guid tripRequestId, BookingCreateFromTripRequestDto dto)
    {
        // 1. Load TripRequest with Trip + Buddy
        var tripRequest = await _uow.TripRequests.GetWithDetailsAsync(tripRequestId)
            ?? throw new KeyNotFoundException("TripRequest not found.");

        var buddy = tripRequest.Buddy;
        if (buddy.UserId != buddyUserId)
            throw new UnauthorizedAccessException("Only the buddy who sent this request can create a booking.");

        var trip = tripRequest.Trip;

        // 2. One booking per TripRequest
        var existing = await _uow.Bookings.GetByTripRequestIdAsync(tripRequestId);
        if (existing is not null)
            throw new InvalidOperationException("A booking already exists for this trip request.");

        // 3. Get chat room (created when buddy sent the TripRequest)
        var chatRooms = await _uow.ChatRooms.FindAsync(r => r.TripRequestId == tripRequestId);
        var chatRoomId = chatRooms.FirstOrDefault()?.Id;

        // 4. Calculate amounts
        var platformFee = Math.Round(dto.Price * (decimal)PlatformFeeRate, 2);
        var total = dto.Price + platformFee;

        // 5. Build Booking entity (snapshot from Trip + buddy's adjustments)
        var booking = new Booking
        {
            TripId           = trip.Id,
            TripRequestId    = tripRequestId,
            BuddyId          = buddy.Id,
            TravelerUserId   = trip.TravelerUserId,
            ChatRoomId       = chatRoomId,

            // Booked (buddy-adjusted)
            BookedDate        = dto.BookedDate,
            BookedStartTime   = dto.BookedStartTime,
            BookedDurationHours = dto.BookedDurationHours,
            BookedAdults      = dto.BookedAdults,
            BookedChildren    = dto.BookedChildren,

            // Price
            Price             = dto.Price,
            Currency          = dto.Currency,
            PlatformFeeAmount = platformFee,
            TotalAmount       = total,

            Includes          = dto.Includes,
            Excludes          = dto.Excludes,
            NoteForCustomer   = dto.NoteForCustomer,

            Status    = BookingStatus.PendingCustomerConfirm,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Bookings.AddAsync(booking);
        await _uow.SaveChangesAsync();

        // 6. Send BookingCard message into the chat room
        if (chatRoomId.HasValue)
        {
            await _chatService.SendBookingCardAsync(chatRoomId.Value, buddyUserId, booking.Id);
        }

        var full = await _uow.Bookings.GetByIdWithDetailsAsync(booking.Id);
        return ToDto(full!);
    }

    // ─── ConfirmByTravelerAsync ───────────────────────────────────────────────

    public async Task<BookingResponseDto> ConfirmByTravelerAsync(Guid travelerUserId, Guid bookingId)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.TravelerUserId != travelerUserId)
            throw new UnauthorizedAccessException("Only the traveler can confirm this booking.");

        if (booking.Status != BookingStatus.PendingCustomerConfirm)
            throw new InvalidOperationException(
                $"Booking cannot be confirmed from status '{booking.Status}'.");

        booking.Status          = BookingStatus.PendingPayment;
        booking.PaymentDeadline = DateTime.UtcNow.AddHours(PaymentDeadlineHours);
        booking.UpdatedAt       = DateTime.UtcNow;

        _uow.Bookings.Update(booking);
        await _uow.SaveChangesAsync();

        return ToDto(booking);
    }

    // ─── Queries ──────────────────────────────────────────────────────────────

    public async Task<BookingResponseDto?> GetByIdAsync(Guid id)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(id);
        return booking is null ? null : ToDto(booking);
    }

    public async Task<IEnumerable<BookingResponseDto>> GetByTripIdAsync(Guid tripId)
    {
        var bookings = await _uow.Bookings.GetByTripIdAsync(tripId);
        return bookings.Select(ToDto);
    }

    public async Task<IEnumerable<BookingResponseDto>> GetMyBookingsAsTravelerAsync(Guid travelerUserId)
    {
        var bookings = await _uow.Bookings.GetByTravelerUserIdAsync(travelerUserId);
        return bookings.Select(ToDto);
    }

    public async Task<IEnumerable<BookingResponseDto>> GetMyBookingsAsBuddyAsync(Guid buddyUserId)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(buddyUserId)
            ?? throw new KeyNotFoundException("Buddy profile not found.");

        var bookings = await _uow.Bookings.GetByBuddyIdAsync(buddy.Id);
        return bookings.Select(ToDto);
    }

    // ─── UpdateStatusAsync (CS / Admin) ──────────────────────────────────────

    public async Task<BookingResponseDto?> UpdateStatusAsync(Guid id, BookingStatus newStatus)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(id);
        if (booking is null) return null;

        if (!AllowedTransitions.TryGetValue(booking.Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidOperationException(
                $"Cannot transition booking from '{booking.Status}' to '{newStatus}'.");

        booking.Status    = newStatus;
        booking.UpdatedAt = DateTime.UtcNow;

        if (newStatus == BookingStatus.Confirmed)
        {
            // Lock the Trip — no more requests
            var trip = await _uow.Trips.GetByIdAsync(booking.TripId);
            if (trip is not null && trip.Status == TripStatus.Active)
            {
                trip.Status    = TripStatus.Deactivate;
                trip.UpdatedAt = DateTime.UtcNow;
                _uow.Trips.Update(trip);
            }
        }
        else if (newStatus == BookingStatus.Completed)
        {
            var trip = await _uow.Trips.GetByIdAsync(booking.TripId);
            if (trip is not null)
            {
                trip.Status    = TripStatus.Expired;
                trip.UpdatedAt = DateTime.UtcNow;
                _uow.Trips.Update(trip);
            }
        }
        else if (newStatus is BookingStatus.Cancelled or BookingStatus.CancelledByTimeout)
        {
            // Re-activate the trip if this was the only active booking
            var trip = await _uow.Trips.GetByIdAsync(booking.TripId);
            if (trip is not null && trip.Status == TripStatus.Deactivate)
            {
                var otherActive = (await _uow.Bookings.GetByTripIdAsync(trip.Id))
                    .Any(b => b.Id != id &&
                              b.Status is BookingStatus.PendingCustomerConfirm
                                       or BookingStatus.PendingPayment
                                       or BookingStatus.Confirmed
                                       or BookingStatus.InProgress);
                if (!otherActive)
                {
                    trip.Status    = TripStatus.Active;
                    trip.UpdatedAt = DateTime.UtcNow;
                    _uow.Trips.Update(trip);
                }
            }
        }

        _uow.Bookings.Update(booking);
        await _uow.SaveChangesAsync();
        return ToDto(booking);
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────

    private static BookingResponseDto ToDto(Booking b) => new()
    {
        Id              = b.Id,
        TripId          = b.TripId,
        TripRequestId   = b.TripRequestId,
        BuddyId         = b.BuddyId,
        BuddyName       = b.Buddy?.User?.FullName ?? string.Empty,
        BuddyAvatar     = b.Buddy?.User?.ProfilePicture,
        TravelerUserId  = b.TravelerUserId,
        TravelerName    = b.TravelerUser?.FullName ?? string.Empty,
        ChatRoomId      = b.ChatRoomId,

        BookedDate        = b.BookedDate,
        BookedStartTime   = b.BookedStartTime,
        BookedDurationHours = b.BookedDurationHours,
        BookedAdults      = b.BookedAdults,
        BookedChildren    = b.BookedChildren,

        Price             = b.Price,
        Currency          = b.Currency,
        PlatformFeeAmount = b.PlatformFeeAmount,
        TotalAmount       = b.TotalAmount,

        Includes          = b.Includes,
        Excludes          = b.Excludes,
        NoteForCustomer   = b.NoteForCustomer,

        Status          = b.Status,
        PaymentDeadline = b.PaymentDeadline,
        CreatedAt       = b.CreatedAt,
        UpdatedAt       = b.UpdatedAt
    };
}
