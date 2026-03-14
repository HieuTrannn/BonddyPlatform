using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class ChatRoomRepository : GenericRepository<ChatRoom>, IChatRoomRepository
{
    public ChatRoomRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<ChatRoom?> GetByIdWithParticipantsAsync(Guid id)
    {
        return await _context.ChatRooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}

