using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class ChatRoomUserStateRepository : GenericRepository<ChatRoomUserState>, IChatRoomUserStateRepository
{
    public ChatRoomUserStateRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<ChatRoomUserState?> GetByRoomAndUserAsync(Guid roomId, Guid userId)
    {
        return await _context.ChatRoomUserStates
            .FirstOrDefaultAsync(x => x.RoomId == roomId && x.UserId == userId);
    }
}

