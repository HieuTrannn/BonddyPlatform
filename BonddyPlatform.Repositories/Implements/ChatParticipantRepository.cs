using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class ChatParticipantRepository : GenericRepository<ChatRoomParticipant>, IChatParticipantRepository
{
    public ChatParticipantRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId)
    {
        return await _context.ChatRoomParticipants
            .AsNoTracking()
            .AnyAsync(p => p.RoomId == roomId && p.UserId == userId);
    }
}

