using BonddyPlatform.Services.DTOs.ChatDtos;
using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.Interfaces;

public interface IChatService
{
    Task<ChatRoomDto> GetOrCreateTripRoomAsync(Guid tripId, Guid travelerUserId);

    Task<bool> IsUserInRoomAsync(Guid roomId, Guid userId);

    Task<PagedResultDto<MessageDto>> GetMessagesAsync(Guid roomId, int page, int pageSize);

    Task<MessageDto> SendMessageAsync(Guid roomId, Guid senderUserId, SendMessageRequestDto request);
}
