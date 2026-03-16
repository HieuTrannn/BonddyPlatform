using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.NotificationDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class NotificationService : INotificationService
{
    private readonly IUnitOfWork _uow;
    private readonly IRealtimeNotifier _realtime;

    public NotificationService(IUnitOfWork uow, IRealtimeNotifier realtime)
    {
        _uow = uow;
        _realtime = realtime;
    }

    public async Task<NotificationDto> CreateAsync(
        Guid userId,
        NotificationType type,
        string title,
        string body,
        string? dataJson = null)
    {
        var entity = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title.Trim(),
            Body = body.Trim(),
            DataJson = dataJson,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Notifications.AddAsync(entity);
        await _uow.SaveChangesAsync();

        var dto = Map(entity);

        await _realtime.NotifyUserAsync(userId.ToString(), "NotificationCreated", dto);

        return dto;
    }

    public async Task<NotificationListResponseDto> GetMyNotificationsAsync(Guid userId, int page, int pageSize)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = _uow.Notifications
            .GetQueryable()
            .Where(n => n.UserId == userId);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new NotificationListResponseDto
        {
            Items = items.Select(Map),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<int> GetMyUnreadCountAsync(Guid userId)
    {
        return await _uow.Notifications
            .GetQueryable()
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkReadAsync(Guid userId, Guid notificationId)
    {
        var entity = await _uow.Notifications.GetByIdAsync(notificationId)
            ?? throw new KeyNotFoundException("Notification not found.");

        if (entity.UserId != userId)
            throw new UnauthorizedAccessException("You are not allowed to update this notification.");

        if (entity.IsRead) return;

        entity.IsRead = true;
        entity.ReadAt = DateTime.UtcNow;
        _uow.Notifications.Update(entity);
        await _uow.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var unread = await _uow.Notifications
            .GetQueryable()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unread.Count == 0) return;

        var now = DateTime.UtcNow;
        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = now;
            _uow.Notifications.Update(n);
        }
        await _uow.SaveChangesAsync();
    }

    private static NotificationDto Map(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type.ToString(),
        Title = n.Title,
        Body = n.Body,
        DataJson = n.DataJson,
        IsRead = n.IsRead,
        CreatedAt = n.CreatedAt,
        ReadAt = n.ReadAt
    };
}

