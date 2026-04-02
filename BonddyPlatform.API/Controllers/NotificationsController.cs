using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.NotificationDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class NotificationsController : ApiControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Get my notifications with paging.
    /// GET /api/Notifications?page=1&pageSize=20
    /// </summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get my notifications (paging)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<NotificationListResponseDto>))]
    public async Task<IActionResult> GetMy([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserIdOrThrow();
        return await ExecuteAsync(
            () => _notificationService.GetMyNotificationsAsync(userId, page, pageSize),
            result => Ok(ApiResponse<NotificationListResponseDto>.SuccessResponse(result, "Notifications fetched successfully")));
    }

    /// <summary>
    /// Get my unread notification count.
    /// GET /api/Notifications/unread-count
    /// </summary>
    [HttpGet("unread-count")]
    [SwaggerOperation(Summary = "Get my unread notification count")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<UnreadCountDto>))]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserIdOrThrow();
        var count = await _notificationService.GetMyUnreadCountAsync(userId);
        return Ok(ApiResponse<UnreadCountDto>.SuccessResponse(new UnreadCountDto { UnreadCount = count }));
    }

    /// <summary>
    /// Mark a notification as read.
    /// POST /api/Notifications/{id}/read
    /// </summary>
    [HttpPost("{id:guid}/read")]
    [SwaggerOperation(Summary = "Mark notification as read")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse))]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var userId = GetCurrentUserIdOrThrow();
        return await ExecuteAsync(
            () => _notificationService.MarkReadAsync(userId, id),
            Success("Notification marked as read."));
    }

    /// <summary>
    /// Mark all my notifications as read.
    /// POST /api/Notifications/read-all
    /// </summary>
    [HttpPost("read-all")]
    [SwaggerOperation(Summary = "Mark all notifications as read")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse))]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetCurrentUserIdOrThrow();
        return await ExecuteAsync(
            () => _notificationService.MarkAllReadAsync(userId),
            Success("All notifications marked as read."));
    }
}

