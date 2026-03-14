using BonddyPlatform.API.Common;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.TripDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TripRequestsController : ApiControllerBase
{
    private readonly ITripRequestService _service;

    public TripRequestsController(ITripRequestService service)
    {
        _service = service;
    }

    [HttpPost]
    [Authorize(Roles = nameof(Role.Buddy))]
    [SwaggerOperation(
        Summary = "Buddy gửi offer cho 1 Trip",
        Description = "Buddy điền giá offer (OfferedPrice) hoặc chọn IsInboxOnly=true để bàn giá qua chat. " +
                      "Mỗi buddy chỉ gửi được 1 request cho mỗi trip. ExpiresAt tự động = ngày/giờ của trip.")]
    [SwaggerResponse(200, "Created", typeof(ApiResponse<TripRequestResponseDto>))]
    [SwaggerResponse(400, "Bad request", typeof(ApiResponse))]
    public async Task<IActionResult> Create([FromBody] TripRequestCreateDto dto)
    {
        var buddyUserId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<TripRequestResponseDto>.SuccessResponse(await _service.CreateAsync(buddyUserId, dto), "Trip request created successfully"));
    }

    [HttpGet("trip/{tripId:guid}")]
    [Authorize(Roles = nameof(Role.User))]
    [SwaggerOperation(
        Summary = "Traveler xem tất cả offer cho trip của mình",
        Description = "Danh sách offer từ các buddy gửi cho trip này. " +
                      "ExistingChatRoomId != null nghĩa là traveler đã bắt đầu chat với buddy đó rồi.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<TripRequestResponseDto>>))]
    public async Task<IActionResult> GetByTrip(Guid tripId)
    {
        var travelerUserId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<IEnumerable<TripRequestResponseDto>>.SuccessResponse(await _service.GetByTripIdAsync(tripId, travelerUserId), "Trip requests fetched successfully"));
    }

    [HttpGet("my")]
    [Authorize(Roles = nameof(Role.Buddy))]
    [SwaggerOperation(
        Summary = "Buddy xem tất cả offer mình đã gửi",
        Description = "Trả về danh sách tất cả request buddy đã gửi cho các trip.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<TripRequestResponseDto>>))]
    public async Task<IActionResult> GetMyRequests()
    {
        var buddyUserId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<IEnumerable<TripRequestResponseDto>>.SuccessResponse(await _service.GetMyRequestsAsync(buddyUserId), "Trip requests fetched successfully"));
    }

    [HttpPost("{id:guid}/start-chat")]
    [Authorize(Roles = nameof(Role.User))]
    [SwaggerOperation(
        Summary = "Traveler bắt đầu nhắn tin với Buddy từ offer",
        Description = "Tạo (hoặc trả về) chat room 1:1 giữa traveler và buddy dựa trên TripRequest. " +
                      "Idempotent — gọi nhiều lần chỉ trả về cùng 1 room. " +
                      "Sau đó dùng ChatRoomId để gửi/nhận tin nhắn qua /api/Chat.")]
    [SwaggerResponse(200, "Chat room ready", typeof(ApiResponse<StartChatResponseDto>))]
    [SwaggerResponse(400, "Request expired or not found", typeof(ApiResponse))]
    public async Task<IActionResult> StartChat(Guid id)
    {
        var travelerUserId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<StartChatResponseDto>.SuccessResponse(await _service.StartChatAsync(id, travelerUserId), "Chat started successfully"));
    }

    [HttpDelete("{requestId:guid}")]
    [Authorize(Roles = nameof(Role.Buddy))]
    [SwaggerOperation(
        Summary = "Buddy rút lại offer của mình",
        Description = "Buddy xoá request mình đã gửi khi trip chưa hết hạn.")]
    [SwaggerResponse(204, "Withdrawn")]
    [SwaggerResponse(400, "Expired or not found", typeof(ApiResponse))]
    public async Task<IActionResult> Withdraw(Guid requestId)
    {
        var buddyUserId = GetCurrentUserIdOrThrow();
        await _service.DeleteAsync(requestId, buddyUserId);
        return Ok(ApiResponse<object>.SuccessResponse("Trip request withdrawn successfully"));
    }
}
