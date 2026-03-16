using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IChatRoomUserStateRepository : IGenericRepository<ChatRoomUserState>
{
    Task<ChatRoomUserState?> GetByRoomAndUserAsync(Guid roomId, Guid userId);
}

