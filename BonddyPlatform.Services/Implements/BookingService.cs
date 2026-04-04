using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.BookingDtos;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.Helpers;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class BookingService : IBookingService
{
    /// <summary>Default platform fee for buddies without any subscription package.</summary>
    private const decimal DefaultPlatformFeeRate = 0.25m;  // 25% — Free (no package)
    private const int PaymentDeadlineHours = 24;           // 24h for traveler to pay

    private readonly IUnitOfWork _uow;
    private readonly IChatService _chatService;

    // State machine: key = current status, value = allowed next statuses
    private static readonly Dictionary<BookingStatus, HashSet<BookingStatus>> AllowedTransitions = new()
    {
        [BookingStatus.PendingCustomerConfirm] = new() { BookingStatus.Cancelled, BookingStatus.Expired },
        [BookingStatus.PendingPayment]         = new() { BookingStatus.Confirmed, BookingStatus.CancelledByTimeout, BookingStatus.Cancelled, BookingStatus.Expired },
        [BookingStatus.Confirmed]              = new() { BookingStatus.InProgress, BookingStatus.Cancelled },
        [BookingStatus.InProgress]             = new() { BookingStatus.Completed, BookingStatus.Cancelled },
        [BookingStatus.Completed]              = new(),
        [BookingStatus.Cancelled]              = new(),
        [BookingStatus.CancelledByTimeout]     = new(),
        [BookingStatus.Expired]                = new()
    };

    public BookingService(IUnitOfWork uow, IChatService chatService)
    {
        _uow = uow;
        _chatService = chatService;
    }

    /// <summary>
    /// Lấy tỉ lệ phí nền tảng từ gói subscription active của buddy.
    /// Free = 25%, Starter = 20%, Pro = 10%, Founder = 0%.
    /// </summary>
    private async Task<decimal> GetPlatformFeeRateForBuddyAsync(Guid buddyId)
    {
        var activeSub = await _uow.BuddySubscriptions.GetActiveByBuddyIdAsync(buddyId);
        if (activeSub?.ServicePackage is null)
            return DefaultPlatformFeeRate; // Free → 25%

        return activeSub.ServicePackage.CommissionRate; // From package: 0.20, 0.10, 0.00
    }

    public async Task<BookingResponseDto> CreateAsync(Guid buddyUserId, BookingCreateRequestDto dto)
    {
        if (dto.TripRequestId.HasValue)
        {
            var fromTripRequestDto = new BookingCreateFromTripRequestDto
            {
                BookedDate = dto.BookedDate,
                BookedStartTime = dto.BookedStartTime,
                BookedDurationHours = dto.BookedDurationHours,
                BookedAdults = dto.BookedAdults,
                BookedChildren = dto.BookedChildren,
                Price = dto.Price,
                Currency = dto.Currency,
                Includes = dto.Includes,
                Excludes = dto.Excludes,
                NoteForCustomer = dto.NoteForCustomer
            };
            return await CreateFromTripRequestAsync(buddyUserId, dto.TripRequestId.Value, fromTripRequestDto);
        }

        if (!dto.ChatRoomId.HasValue)
            throw new InvalidOperationException("ChatRoomId is required when TripRequestId is not provided.");

        return await CreateFromDirectChatAsync(buddyUserId, dto.ChatRoomId.Value, dto);
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

        // 4. Calculate amounts — commission based on buddy's subscription package
        var feeRate = await GetPlatformFeeRateForBuddyAsync(buddy.Id);
        var platformFee = Math.Round(dto.Price * feeRate, 2);
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

    private async Task<BookingResponseDto> CreateFromDirectChatAsync(
        Guid buddyUserId, Guid chatRoomId, BookingCreateRequestDto dto)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(buddyUserId)
            ?? throw new KeyNotFoundException("Buddy profile not found.");

        var room = await _uow.ChatRooms.GetByIdWithParticipantsAsync(chatRoomId)
            ?? throw new KeyNotFoundException("Chat room not found.");

        if (!room.Participants.Any(p => p.UserId == buddyUserId))
            throw new UnauthorizedAccessException("Buddy is not a participant of this chat room.");

        var travelerUserId = room.Participants
            .Where(p => p.UserId != buddyUserId)
            .Select(p => p.UserId)
            .FirstOrDefault();

        if (travelerUserId == Guid.Empty)
            throw new InvalidOperationException("Cannot determine traveler from chat room participants.");

        var bookedStartDateTime = DateTime.SpecifyKind(
            dto.BookedDate.ToDateTime(dto.BookedStartTime),
            DateTimeKind.Utc);

        // Internal supporting Trip to satisfy current schema.
        var trip = new Trip
        {
            TravelerUserId = travelerUserId,
            City = "Direct booking",
            StartDate = dto.BookedDate,
            StartTime = dto.BookedStartTime,
            DurationHours = dto.BookedDurationHours,
            Adults = dto.BookedAdults,
            Children = dto.BookedChildren,
            Notes = dto.NoteForCustomer,
            Status = TripStatus.Closed,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.Trips.AddAsync(trip);
        await _uow.SaveChangesAsync();

        // Internal supporting TripRequest to satisfy current schema.
        var tripRequest = new TripRequest
        {
            TripId = trip.Id,
            BuddyId = buddy.Id,
            OfferedPrice = dto.Price,
            IsInboxOnly = false,
            Note = "Auto-generated from direct chat booking flow.",
            ExpiresAt = bookedStartDateTime,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.TripRequests.AddAsync(tripRequest);
        await _uow.SaveChangesAsync();

        var feeRate = await GetPlatformFeeRateForBuddyAsync(buddy.Id);
        var platformFee = Math.Round(dto.Price * feeRate, 2);
        var total = dto.Price + platformFee;

        var booking = new Booking
        {
            TripId = trip.Id,
            TripRequestId = tripRequest.Id,
            BuddyId = buddy.Id,
            TravelerUserId = travelerUserId,
            ChatRoomId = chatRoomId,

            BookedDate = dto.BookedDate,
            BookedStartTime = dto.BookedStartTime,
            BookedDurationHours = dto.BookedDurationHours,
            BookedAdults = dto.BookedAdults,
            BookedChildren = dto.BookedChildren,

            Price = dto.Price,
            Currency = dto.Currency,
            PlatformFeeAmount = platformFee,
            TotalAmount = total,

            Includes = dto.Includes,
            Excludes = dto.Excludes,
            NoteForCustomer = dto.NoteForCustomer,

            Status = BookingStatus.PendingCustomerConfirm,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Bookings.AddAsync(booking);
        await _uow.SaveChangesAsync();

        await _chatService.SendBookingCardAsync(chatRoomId, buddyUserId, booking.Id);

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

    public async Task<BookingResponseDto> EnsurePendingPaymentAsync(Guid travelerUserId, Guid bookingId)
    {
        var booking = await _uow.Bookings.GetByIdWithDetailsAsync(bookingId)
            ?? throw new KeyNotFoundException("Booking not found.");

        if (booking.TravelerUserId != travelerUserId)
            throw new UnauthorizedAccessException("Only the traveler can confirm this booking.");

        if (booking.Status == BookingStatus.PendingPayment)
            return ToDto(booking);

        if (booking.Status != BookingStatus.PendingCustomerConfirm)
            throw new InvalidOperationException(
                $"Booking cannot move to payment from status '{booking.Status}'.");

        return await ConfirmByTravelerAsync(travelerUserId, bookingId);
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
            if (trip is not null && trip.Status == TripStatus.Open)
            {
                trip.Status    = TripStatus.Closed;
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
            if (trip is not null && trip.Status == TripStatus.Closed)
            {
                var otherActive = (await _uow.Bookings.GetByTripIdAsync(trip.Id))
                    .Any(b => b.Id != id &&
                              b.Status is BookingStatus.PendingCustomerConfirm
                                       or BookingStatus.PendingPayment
                                       or BookingStatus.Confirmed
                                       or BookingStatus.InProgress);
                if (!otherActive)
                {
                    trip.Status    = TripStatus.Open;
                    trip.UpdatedAt = DateTime.UtcNow;
                    _uow.Trips.Update(trip);
                }
            }
        }

        _uow.Bookings.Update(booking);
        await _uow.SaveChangesAsync();
        return ToDto(booking);
    }

    // ─── Admin: GetAllPagedAsync ──────────────────────────────────────────────

    public async Task<PagedResultDto<BookingResponseDto>> GetAllPagedAsync(BookingSearchRequestDto request)
    {
        var query = _uow.Bookings.GetQueryable()
            .Include(b => b.Buddy).ThenInclude(buddy => buddy!.User)
            .Include(b => b.TravelerUser)
            .AsQueryable();

        // Free-text search trên BuddyName, TravelerName
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var kw = request.Search.Trim().ToLower();
            query = query.Where(b =>
                (b.Buddy != null && b.Buddy.User != null && b.Buddy.User.FullName.ToLower().Contains(kw)) ||
                (b.TravelerUser != null && b.TravelerUser.FullName.ToLower().Contains(kw)));
        }

        if (request.Status.HasValue)
            query = query.Where(b => b.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.BuddyName))
        {
            var kw = request.BuddyName.Trim().ToLower();
            query = query.Where(b => b.Buddy != null && b.Buddy.User != null &&
                                     b.Buddy.User.FullName.ToLower().Contains(kw));
        }

        if (!string.IsNullOrWhiteSpace(request.TravelerName))
        {
            var kw = request.TravelerName.Trim().ToLower();
            query = query.Where(b => b.TravelerUser != null &&
                                     b.TravelerUser.FullName.ToLower().Contains(kw));
        }

        if (request.BookedFrom.HasValue)
            query = query.Where(b => b.BookedDate >= request.BookedFrom.Value);

        if (request.BookedTo.HasValue)
            query = query.Where(b => b.BookedDate <= request.BookedTo.Value);

        var totalCount = await query.CountAsync();

        query = query.ApplySorting(request.SortBy ?? nameof(Booking.CreatedAt), request.SortOrder);
        var items = await query.ApplyPaging(request.Page, request.PageSize).ToListAsync();

        return new PagedResultDto<BookingResponseDto>
        {
            Items = items.Select(ToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
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
