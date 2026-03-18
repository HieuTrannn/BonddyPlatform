using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BonddyPlatform.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IPresenceTracker _presenceTracker;

    public ChatHub(IChatService chatService, IPresenceTracker presenceTracker)
    {
        _chatService = chatService;
        _presenceTracker = presenceTracker;
    }

    public override Task OnConnectedAsync()
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
            _presenceTracker.UserConnected(userId.Value);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
            _presenceTracker.UserDisconnected(userId.Value);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRoom(Guid roomId)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("Unauthorized");
        }

        var isParticipant = await _chatService.IsUserInRoomAsync(roomId, userId.Value);
        if (!isParticipant)
        {
            throw new HubException("You are not a participant of this room.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(roomId));
    }

    public async Task LeaveRoom(Guid roomId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(roomId));
    }

    public async Task SendMessage(Guid roomId, string content)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
        {
            throw new HubException("Unauthorized");
        }

        var message = await _chatService.SendMessageAsync(
            roomId,
            userId.Value,
            new Services.DTOs.ChatDtos.SendMessageRequestDto { ContentText = content });

        await Clients.Group(GetGroupName(roomId))
            .SendAsync("MessageCreated", message);
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return null;
        return userId;
    }

    private static string GetGroupName(Guid roomId) => $"room-{roomId}";
}
