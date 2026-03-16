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
    /// Get all chat rooms for the current user.
    /// </summary>
    [HttpGet("rooms")]
    [SwaggerOperation(Summary = "Get all chat rooms of current user")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<ChatRoomDto>>))]
    public async Task<IActionResult> GetMyRooms()
    {
        var userId = GetCurrentUserIdOrThrow();
        var rooms = await _chatService.GetRoomsForUserAsync(userId);
        return Ok(ApiResponse<IEnumerable<ChatRoomDto>>.SuccessResponse(rooms, "Chat rooms fetched successfully"));
    }

    /// <summary>
    /// Get or create chat room for a trip.
    /// </summary>
    [HttpPost("trips/{tripId:guid}/room")]
    [SwaggerOperation(Summary = "Get or create trip chat room")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<ChatRoomDto>))]
    public async Task<IActionResult> GetOrCreateTripRoom(Guid tripId)
    {
        var userId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<ChatRoomDto>.SuccessResponse(await _chatService.GetOrCreateTripRoomAsync(tripId, userId), "Trip chat room created successfully"));
    }

    /// <summary>
    /// Get messages for a room with paging.
    /// </summary>
    [HttpGet("rooms/{roomId:guid}/messages")]
    [SwaggerOperation(Summary = "Get messages for room with paging")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<MessageDto>>))]
    public async Task<IActionResult> GetMessages(
        Guid roomId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = GetCurrentUserIdOrThrow();
        var isParticipant = await _chatService.IsUserInRoomAsync(roomId, userId);
        if (!isParticipant)
        {
            return Unauthorized(ApiResponse.ErrorResponse("You are not a participant of this room."));
        }
        return Ok(ApiResponse<PagedResultDto<MessageDto>>.SuccessResponse(await _chatService.GetMessagesAsync(roomId, page, pageSize), "Messages fetched successfully"));
    }

    /// <summary>
    /// Send a text message to a room.
    /// </summary>
    [HttpPost("rooms/{roomId:guid}/messages")]
    [SwaggerOperation(Summary = "Send message to room")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<MessageDto>))]
    public async Task<IActionResult> SendMessage(Guid roomId, [FromBody] SendMessageRequestDto request)
    {
        var userId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<MessageDto>.SuccessResponse(await _chatService.SendMessageAsync(roomId, userId, request), "Message sent successfully"));
    }
}
