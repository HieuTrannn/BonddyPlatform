using BonddyPlatform.Services.DTOs.NotificationDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IChatReadService
{
    Task<ChatUnreadSummaryDto> GetUnreadSummaryAsync(Guid userId);
    Task MarkRoomReadAsync(Guid userId, Guid roomId);
}

