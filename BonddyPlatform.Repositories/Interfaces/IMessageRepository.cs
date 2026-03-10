using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IMessageRepository : IGenericRepository<Message>
{
    Task<IEnumerable<Message>> GetMessagesForRoomAsync(Guid roomId, int page, int pageSize);
}

