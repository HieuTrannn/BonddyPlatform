using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Repositories.Persistences;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Repositories.Implements;

public class MessageRepository : GenericRepository<Message>, IMessageRepository
{
    public MessageRepository(BonddyDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Message>> GetMessagesForRoomAsync(Guid roomId, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;

        return await _context.Messages
            .AsNoTracking()
            .Where(m => m.RoomId == roomId)
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }
}

