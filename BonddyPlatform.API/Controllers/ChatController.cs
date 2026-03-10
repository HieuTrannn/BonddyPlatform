using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.ChatDtos;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ChatController : ApiControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Get or create chat room for a trip.
    /// </summary>
    [HttpPost("trips/{tripId:guid}/room")]
    [SwaggerOperation(Summary = "Get or create trip chat room")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<ChatRoomDto>))]
    public Task<IActionResult> GetOrCreateTripRoom(Guid tripId)
    {
        var userId = GetCurrentUserIdOrThrow();
        return ExecuteAsync(() => _chatService.GetOrCreateTripRoomAsync(tripId, userId));
    }

    /// <summary>
    /// Get messages for a room with paging.
    /// </summary>
    [HttpGet("rooms/{roomId:guid}/messages")]
    [SwaggerOperation(Summary = "Get messages for room with paging")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<MessageDto>>))]
    public Task<IActionResult> GetMessages(
        Guid roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserIdOrThrow();
        return ExecuteAsync(async () =>
        {
            var isParticipant = await _chatService.IsUserInRoomAsync(roomId, userId);
            if (!isParticipant)
            {
                throw new UnauthorizedAccessException("You are not a participant of this room.");
            }

            return await _chatService.GetMessagesAsync(roomId, page, pageSize);
        });
    }

    /// <summary>
    /// Send a text message to a room.
    /// </summary>
    [HttpPost("rooms/{roomId:guid}/messages")]
    [SwaggerOperation(Summary = "Send message to room")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<MessageDto>))]
    public Task<IActionResult> SendMessage(Guid roomId, [FromBody] SendMessageRequestDto request)
    {
        var userId = GetCurrentUserIdOrThrow();
        return ExecuteAsync(() => _chatService.SendMessageAsync(roomId, userId, request));
    }
}
