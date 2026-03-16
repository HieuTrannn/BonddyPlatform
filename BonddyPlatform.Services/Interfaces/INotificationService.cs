using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.NotificationDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface INotificationService
{
    Task<NotificationListResponseDto> GetMyNotificationsAsync(Guid userId, int page, int pageSize);
    Task<int> GetMyUnreadCountAsync(Guid userId);
    Task MarkReadAsync(Guid userId, Guid notificationId);
    Task MarkAllReadAsync(Guid userId);

    /// <summary>
    /// Create a notification for a specific user (and emit realtime event if connected).
    /// </summary>
    Task<NotificationDto> CreateAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? dataJson = null);
}

