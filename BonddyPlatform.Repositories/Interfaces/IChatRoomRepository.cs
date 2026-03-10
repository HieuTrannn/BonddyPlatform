using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IChatRoomRepository : IGenericRepository<ChatRoom>
{
    Task<ChatRoom?> GetByIdWithParticipantsAsync(Guid id);
}

