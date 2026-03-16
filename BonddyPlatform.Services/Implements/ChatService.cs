using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.ChatDtos;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class ChatService : IChatService
{
    private readonly IUnitOfWork _uow;
    private readonly INotificationService _notificationService;

    public ChatService(IUnitOfWork uow, INotificationService notificationService)
    {
        _uow = uow;
        _notificationService = notificationService;
    }

    public async Task<ChatRoomDto> GetOrCreateTripRoomAsync(Guid tripId, Guid travelerUserId)
    {
        var existing = await _uow.ChatRooms
            .FindAsync(r => r.RoomType == ChatRoomType.Trip && r.TripId == tripId);

        var room = existing.FirstOrDefault();
        if (room == null)
        {
            room = new ChatRoom
            {
                RoomType = ChatRoomType.Trip,
                TripId = tripId,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.ChatRooms.AddAsync(room);
            await _uow.SaveChangesAsync();

            var participant = new ChatRoomParticipant
            {
                RoomId = room.Id,
                UserId = travelerUserId,
                RoleInRoom = RoomParticipantRole.Traveler,
                JoinedAt = DateTime.UtcNow
            };

            await _uow.ChatParticipants.AddAsync(participant);
            await _uow.SaveChangesAsync();
        }

        return MapRoomToDto(room);
    }

    public async Task<ChatRoomDto> GetOrCreateDirectChatAsync(Guid tripRequestId, Guid travelerUserId, Guid buddyUserId)
    {
        // Check if a DirectBuddy room already exists for this TripRequest
        var existing = await _uow.ChatRooms
            .FindAsync(r => r.RoomType == ChatRoomType.DirectBuddy && r.TripRequestId == tripRequestId);

        var room = existing.FirstOrDefault();
        if (room != null)
            return MapRoomToDto(room);

        // Create the room
        room = new ChatRoom
        {
            RoomType = ChatRoomType.DirectBuddy,
            TripRequestId = tripRequestId,
            BuddyUserId = buddyUserId,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.ChatRooms.AddAsync(room);
        await _uow.SaveChangesAsync();

        // Add both participants
        await _uow.ChatParticipants.AddAsync(new ChatRoomParticipant
        {
            RoomId = room.Id,
            UserId = travelerUserId,
            RoleInRoom = RoomParticipantRole.Traveler,
            JoinedAt = DateTime.UtcNow
        });
        await _uow.ChatParticipants.AddAsync(new ChatRoomParticipant
        {
            RoomId = room.Id,
            UserId = buddyUserId,
            RoleInRoom = RoomParticipantRole.Buddy,
            JoinedAt = DateTime.UtcNow
        });
        await _uow.SaveChangesAsync();

        return MapRoomToDto(room);
    }

    public async Task<ChatRoomDto> GetOrCreateDirectChatByBuddyUserIdAsync(Guid travelerUserId, Guid buddyUserId)
    {
        // Try to find any DirectBuddy room between these two users (regardless of TripRequest)
        var existing = await _uow.ChatRooms
            .GetQueryable()
            .Include(r => r.Participants)
            .Where(r => r.RoomType == ChatRoomType.DirectBuddy && r.BuddyUserId == buddyUserId)
            .Where(r => r.Participants.Any(p => p.UserId == travelerUserId))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (existing != null)
            return MapRoomToDto(existing);

        // Create a new direct buddy room without TripRequest
        var room = new ChatRoom
        {
            RoomType = ChatRoomType.DirectBuddy,
            TripRequestId = null,
            BuddyUserId = buddyUserId,
            CreatedAt = DateTime.UtcNow
        };
        await _uow.ChatRooms.AddAsync(room);
        await _uow.SaveChangesAsync();

        await _uow.ChatParticipants.AddAsync(new ChatRoomParticipant
        {
            RoomId = room.Id,
            UserId = travelerUserId,
            RoleInRoom = RoomParticipantRole.Traveler,
            JoinedAt = DateTime.UtcNow
        });
        await _uow.ChatParticipants.AddAsync(new ChatRoomParticipant
        {
            RoomId = room.Id,
            UserId = buddyUserId,
            RoleInRoom = RoomParticipantRole.Buddy,
            JoinedAt = DateTime.UtcNow
        });
        await _uow.SaveChangesAsync();

        return MapRoomToDto(room);
    }

    public async Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId)
    {
        return await _uow.ChatParticipants.IsUserInRoomAsync(roomId, userId);
    }

    public async Task<PagedResultDto<MessageDto>> GetMessagesAsync(Guid roomId, int page, int pageSize)
    {
        var items = await _uow.Messages.GetMessagesForRoomAsync(roomId, page, pageSize);

        var totalCount = await _uow.Messages
            .GetQueryable()
            .Where(m => m.RoomId == roomId)
            .CountAsync();

        return new PagedResultDto<MessageDto>
        {
            Items = items.Select(MapMessageToDto),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<MessageDto> SendMessageAsync(Guid roomId, Guid senderUserId, SendMessageRequestDto request)
    {
        if (!await _uow.ChatParticipants.IsUserInRoomAsync(roomId, senderUserId))
        {
            throw new InvalidOperationException("User is not a participant of this room.");
        }

        var contentText = request.ContentText.Trim();
        if (string.IsNullOrWhiteSpace(contentText))
            throw new InvalidOperationException("Message content cannot be empty.");

        var message = new Message
        {
            RoomId = roomId,
            SenderUserId = senderUserId,
            ContentType = MessageContentType.Text,
            ContentText = contentText,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Messages.AddAsync(message);
        await _uow.SaveChangesAsync();

        // Create notifications for other participants in the room
        var room = await _uow.ChatRooms.GetByIdWithParticipantsAsync(roomId);
        if (room != null)
        {
            var dataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                roomId,
                messageId = message.Id
            });

            var title = "New message";
            var body = contentText.Length > 160 ? contentText[..160] + "..." : contentText;

            foreach (var p in room.Participants.Where(p => p.UserId != senderUserId))
            {
                await _notificationService.CreateAsync(p.UserId, NotificationType.ChatMessage, title, body, dataJson);
            }
        }

        return MapMessageToDto(message);
    }

    public async Task<MessageDto> SendBookingCardAsync(Guid roomId, Guid senderUserId, Guid bookingId)
    {
        if (!await _uow.ChatParticipants.IsUserInRoomAsync(roomId, senderUserId))
            throw new InvalidOperationException("User is not a participant of this room.");

        var message = new Message
        {
            RoomId = roomId,
            SenderUserId = senderUserId,
            ContentType = MessageContentType.BookingCard,
            ContentText = null,
            BookingId = bookingId,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Messages.AddAsync(message);
        await _uow.SaveChangesAsync();

        // Notify other participants that a booking card was sent
        var room = await _uow.ChatRooms.GetByIdWithParticipantsAsync(roomId);
        if (room != null)
        {
            var dataJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                roomId,
                messageId = message.Id,
                bookingId
            });

            foreach (var p in room.Participants.Where(p => p.UserId != senderUserId))
            {
                await _notificationService.CreateAsync(
                    p.UserId,
                    NotificationType.System,
                    "New booking offer",
                    "A booking offer was sent in chat.",
                    dataJson);
            }
        }

        return MapMessageToDto(message);
    }

        public async Task<IEnumerable<ChatRoomDto>> GetRoomsForUserAsync(Guid userId)
        {
            var query = _uow.ChatRooms
                .GetQueryable()
                .Include(r => r.Participants)
                .Where(r => r.Participants.Any(p => p.UserId == userId));

            var rooms = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return rooms.Select(MapRoomToDto);
        }

    private static ChatRoomDto MapRoomToDto(ChatRoom room) => new()
    {
        Id = room.Id,
        RoomType = room.RoomType.ToString(),
        TripId = room.TripId,
        TripRequestId = room.TripRequestId,
        BuddyUserId = room.BuddyUserId
    };

    private static MessageDto MapMessageToDto(Message message) => new()
    {
        Id = message.Id,
        RoomId = message.RoomId,
        SenderUserId = message.SenderUserId,
        ContentType = message.ContentType.ToString(),
        ContentText = message.ContentText,
        BookingId = message.BookingId,
        CreatedAt = message.CreatedAt
    };
}
