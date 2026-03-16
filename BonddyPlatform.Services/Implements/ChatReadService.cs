using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.NotificationDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class ChatReadService : IChatReadService
{
    private readonly IUnitOfWork _uow;

    public ChatReadService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<ChatUnreadSummaryDto> GetUnreadSummaryAsync(Guid userId)
    {
        var roomIds = await _uow.ChatParticipants
            .GetQueryable()
            .Where(p => p.UserId == userId)
            .Select(p => p.RoomId)
            .Distinct()
            .ToListAsync();

        if (roomIds.Count == 0)
        {
            return new ChatUnreadSummaryDto
            {
                TotalUnread = 0,
                Rooms = Array.Empty<ChatUnreadRoomDto>()
            };
        }

        var states = await _uow.ChatRoomUserStates
            .GetQueryable()
            .Where(s => s.UserId == userId && roomIds.Contains(s.RoomId))
            .ToListAsync();

        var stateMap = states.ToDictionary(s => s.RoomId, s => s);

        var results = new List<ChatUnreadRoomDto>(roomIds.Count);
        var total = 0;

        foreach (var roomId in roomIds)
        {
            var lastReadAt = stateMap.TryGetValue(roomId, out var st) ? st.LastReadAt : DateTime.UnixEpoch;

            var unreadCount = await _uow.Messages
                .GetQueryable()
                .Where(m => m.RoomId == roomId && m.CreatedAt > lastReadAt && m.SenderUserId != userId)
                .CountAsync();

            total += unreadCount;
            results.Add(new ChatUnreadRoomDto
            {
                RoomId = roomId,
                UnreadCount = unreadCount,
                LastReadAt = lastReadAt
            });
        }

        return new ChatUnreadSummaryDto
        {
            TotalUnread = total,
            Rooms = results.OrderByDescending(r => r.UnreadCount).ToList()
        };
    }

    public async Task MarkRoomReadAsync(Guid userId, Guid roomId)
    {
        var isParticipant = await _uow.ChatParticipants.IsUserInRoomAsync(roomId, userId);
        if (!isParticipant) throw new UnauthorizedAccessException("You are not a participant of this room.");

        var latestCreatedAt = await _uow.Messages
            .GetQueryable()
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => (DateTime?)m.CreatedAt)
            .FirstOrDefaultAsync();

        var newLastReadAt = latestCreatedAt ?? DateTime.UtcNow;

        var state = await _uow.ChatRoomUserStates.GetByRoomAndUserAsync(roomId, userId);
        if (state == null)
        {
            state = new ChatRoomUserState
            {
                RoomId = roomId,
                UserId = userId,
                LastReadAt = newLastReadAt,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _uow.ChatRoomUserStates.AddAsync(state);
        }
        else
        {
            state.LastReadAt = newLastReadAt;
            state.UpdatedAt = DateTime.UtcNow;
            _uow.ChatRoomUserStates.Update(state);
        }

        await _uow.SaveChangesAsync();
    }
}

