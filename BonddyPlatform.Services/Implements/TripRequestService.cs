using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.TripDtos;
using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;

namespace BonddyPlatform.Services.Implements;

public class TripRequestService : ITripRequestService
{
    private readonly IUnitOfWork _uow;
    private readonly IChatService _chatService;
    private readonly IEmailSender _emailSender;
    private readonly INotificationService _notificationService;

    public TripRequestService(IUnitOfWork uow, IChatService chatService, IEmailSender emailSender, INotificationService notificationService)
    {
        _uow = uow;
        _chatService = chatService;
        _emailSender = emailSender;
        _notificationService = notificationService;
    }

    public async Task<TripRequestResponseDto> CreateAsync(Guid buddyUserId, TripRequestCreateDto dto)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(buddyUserId)
            ?? throw new KeyNotFoundException("Buddy profile not found. Make sure you have a Buddy profile.");

        var trip = await _uow.Trips.GetByIdAsync(dto.TripId)
            ?? throw new KeyNotFoundException("Trip not found.");

        // Prevent buddy from requesting their own trip (same user has multiple roles)
        if (trip.TravelerUserId == buddyUserId)
            throw new InvalidOperationException("You cannot send a trip request to your own trip.");

        if (trip.Status != TripStatus.Open)
            throw new InvalidOperationException("This trip is no longer accepting requests.");

        // Prevent duplicate request from the same buddy
        var existing = await _uow.TripRequests.GetByTripAndBuddyAsync(trip.Id, buddy.Id);
        if (existing != null)
            throw new InvalidOperationException("You have already sent a request for this trip.");

        if (!dto.IsInboxOnly && dto.OfferedPrice == null)
            throw new InvalidOperationException("Please provide an offered price, or set IsInboxOnly = true to negotiate in chat.");

        // ExpiresAt = Trip's start datetime (UTC)
        var expiresAt = trip.StartDate.ToDateTime(trip.StartTime, DateTimeKind.Utc);

        var entity = new TripRequest
        {
            TripId = trip.Id,
            BuddyId = buddy.Id,
            OfferedPrice = dto.IsInboxOnly ? null : dto.OfferedPrice,
            IsInboxOnly = dto.IsInboxOnly,
            Note = dto.Note?.Trim(),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.TripRequests.AddAsync(entity);
        await _uow.SaveChangesAsync();

        // Send email notification to traveler
        await SendTravelerEmailAsync(trip, buddy, entity, dto);

        // Auto-create direct chat room between traveler and buddy for this request
        var travelerUserId = trip.TravelerUserId;
        var buddyProfileUserId = buddy.UserId;
        var room = await _chatService.GetOrCreateDirectChatAsync(entity.Id, travelerUserId, buddyProfileUserId);

        // Create a dedicated "TripRequestCreated" notification for the traveler (separate from chat messages)
        try
        {
            var buddyUser = await _uow.Users.GetByIdAsync(buddy.UserId);
            var buddyName = buddyUser?.FullName ?? "A buddy";
            var offerText = dto.IsInboxOnly
                ? "Inbox only"
                : $"{dto.OfferedPrice:0.##}";

            var title = "New trip request";
            var body = $"{buddyName} sent a request for your trip to {trip.City} ({trip.StartDate:yyyy-MM-dd}). Offer: {offerText}.";
            var dataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                tripId = trip.Id,
                tripRequestId = entity.Id,
                chatRoomId = room.Id
            });

            await _notificationService.CreateAsync(travelerUserId, NotificationType.TripRequestCreated, title, body, dataJson);
        }
        catch
        {
            // Notification is non-critical; do not block main flow.
        }

        // Send the first message in chat summarizing the trip and the offer
        var introLines = new List<string>
        {
            "📌 New trip request from buddy",
            $"Trip: {trip.City} on {trip.StartDate:yyyy-MM-dd} at {trip.StartTime}",
            $"Group: {trip.Adults} adults, {trip.Children} children, duration {trip.DurationHours} hours",
            dto.IsInboxOnly
                ? "Offer: Inbox only (buddy prefers to discuss price in chat)."
                : $"Offer price: {dto.OfferedPrice:0.##}",
        };
        if (!string.IsNullOrWhiteSpace(dto.Note))
        {
            introLines.Add("");
            introLines.Add("Message from buddy:");
            introLines.Add(dto.Note.Trim());
        }

        var introText = string.Join(Environment.NewLine, introLines);
        await _chatService.SendMessageAsync(room.Id, buddyProfileUserId, new DTOs.ChatDtos.SendMessageRequestDto
        {
            ContentText = introText
        });

        return MapToDto(entity, trip, buddy, existingChatRoomId: room.Id);
    }

    public async Task<IEnumerable<TripRequestResponseDto>> GetByTripIdAsync(Guid tripId, Guid requesterId)
    {
        var trip = await _uow.Trips.GetByIdAsync(tripId)
            ?? throw new KeyNotFoundException("Trip not found.");

        if (trip.TravelerUserId != requesterId)
            throw new UnauthorizedAccessException("You are not allowed to view requests for this trip.");

        var requests = await _uow.TripRequests.GetByTripIdAsync(tripId);

        // Find which requests already have a chat room started
        var chatRooms = await _uow.ChatRooms.FindAsync(
            r => r.RoomType == ChatRoomType.DirectBuddy && r.TripRequestId != null
              && requests.Select(req => req.Id).Contains(r.TripRequestId!.Value));

        var chatMap = chatRooms.ToDictionary(r => r.TripRequestId!.Value, r => r.Id);

        return requests.Select(r => MapToDto(
            r, r.Trip ?? trip, r.Buddy,
            chatMap.TryGetValue(r.Id, out var roomId) ? roomId : null));
    }

    public async Task<IEnumerable<TripRequestResponseDto>> GetMyRequestsAsync(Guid buddyUserId)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(buddyUserId)
            ?? throw new KeyNotFoundException("Buddy profile not found.");

        var requests = await _uow.TripRequests.GetByBuddyIdAsync(buddy.Id);
        return requests.Select(r => MapToDto(r, r.Trip, r.Buddy, existingChatRoomId: null));
    }

    public async Task DeleteAsync(Guid requestId, Guid buddyUserId)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(buddyUserId)
            ?? throw new KeyNotFoundException("Buddy profile not found.");

        var request = await _uow.TripRequests.GetByIdAsync(requestId)
            ?? throw new KeyNotFoundException("Request not found.");

        if (request.BuddyId != buddy.Id)
            throw new UnauthorizedAccessException("You are not the owner of this request.");

        if (request.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Cannot withdraw an expired request.");

        _uow.TripRequests.Remove(request);
        await _uow.SaveChangesAsync();
    }

    public async Task<StartChatResponseDto> StartChatAsync(Guid requestId, Guid travelerUserId)
    {
        var request = await _uow.TripRequests.GetWithDetailsAsync(requestId)
            ?? throw new KeyNotFoundException("Request not found.");

        if (request.Trip.TravelerUserId != travelerUserId)
            throw new UnauthorizedAccessException("You are not the traveler of this trip.");

        if (request.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("This request has expired (the trip date has passed).");

        var buddyUserId = request.Buddy.UserId;

        var room = await _chatService.GetOrCreateDirectChatAsync(requestId, travelerUserId, buddyUserId);

        return new StartChatResponseDto
        {
            ChatRoomId = room.Id,
            TripRequestId = requestId,
            BuddyUserId = buddyUserId,
            BuddyName = request.Buddy.User?.FullName ?? string.Empty
        };
    }

    private async Task SendTravelerEmailAsync(Trip trip, Buddy buddy, TripRequest request, TripRequestCreateDto dto)
    {
        var traveler = await _uow.Users.GetByIdAsync(trip.TravelerUserId);
        var buddyUser = await _uow.Users.GetByIdAsync(buddy.UserId);
        if (traveler is null || string.IsNullOrWhiteSpace(traveler.Email))
        {
            return;
        }

        var subject = $"[Bonddy] New buddy request for your trip to {trip.City}";

        var buddyName = buddyUser?.FullName ?? "A Bonddy buddy";
        var travelerName = traveler.FullName;

        var body = EmailTemplates.BuildNewTripRequestEmailHtml(
            travelerName: travelerName,
            buddyName: buddyName,
            city: trip.City,
            startDate: trip.StartDate,
            startTime: trip.StartTime,
            adults: trip.Adults,
            children: trip.Children,
            durationHours: trip.DurationHours,
            isInboxOnly: dto.IsInboxOnly,
            offeredPrice: dto.OfferedPrice,
            buddyNote: dto.Note,
            tripId: trip.Id,
            createdAtUtc: DateTime.UtcNow);

        try
        {
            await _emailSender.SendEmailAsync(traveler.Email, subject, body);
        }
        catch
        {
            // Email failures should not block trip request creation or chat setup.
            // Consider logging this exception via a logging service in the future.
        }
    }

    private static TripRequestResponseDto MapToDto(
        TripRequest r, Trip trip, Buddy buddy, Guid? existingChatRoomId) => new()
    {
        Id = r.Id,
        Trip = new TripInfoResponseDto
        {
            Id = trip.Id,
            City = trip.City,
            StartDate = trip.StartDate,
            StartTime = trip.StartTime,
            DurationHours = trip.DurationHours,
            Adults = trip.Adults,
            Children = trip.Children
        },
        Buddy = new BuddyInfoResponseDto
        {
            Id = buddy.Id,
            Name = buddy.User?.FullName ?? string.Empty,
            Avatar = buddy.User?.ProfilePicture
        },
        Offer = new OfferInfoResponseDto
        {
            Price = r.OfferedPrice ?? 0,
            IsInboxOnly = r.IsInboxOnly,
            Note = r.Note
        },
        IsExpired = r.ExpiresAt <= DateTime.UtcNow,
        ExpiresAt = r.ExpiresAt,
        CreatedAt = r.CreatedAt,
        ExistingChatRoomId = existingChatRoomId
    };
}
