using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Repositories.Interfaces;

public interface IChatParticipantRepository : IGenericRepository<ChatRoomParticipant>
{
    Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId);
}

