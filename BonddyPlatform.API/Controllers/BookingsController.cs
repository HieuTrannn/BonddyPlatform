using BonddyPlatform.API.Common;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.BookingDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    // ─── Buddy creates booking ────────────────────────────────────────────────

    /// <summary>
    /// Buddy tạo booking từ một TripRequest.
    /// POST /api/TripRequests/{tripRequestId}/booking
    /// </summary>
    [HttpPost("/api/TripRequests/{tripRequestId:guid}/booking")]
    [Authorize(Roles = "Buddy")]
    [SwaggerOperation(Summary = "Buddy tạo booking từ TripRequest (sau khi đã trao đổi trong chat)")]
    public async Task<IActionResult> CreateFromTripRequest(
        Guid tripRequestId, [FromBody] BookingCreateFromTripRequestDto dto)
    {
        try
        {
            var buddyUserId = GetCurrentUserIdOrThrow();
            var result = await _bookingService.CreateFromTripRequestAsync(buddyUserId, tripRequestId, dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ApiResponse<BookingResponseDto>.SuccessResponse(result, "Booking created and sent to traveler."));
        }
        catch (KeyNotFoundException ex)      { return NotFound(ApiResponse.ErrorResponse(ex.Message)); }
        catch (UnauthorizedAccessException)  { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.ErrorResponse(ex.Message)); }
    }

    // ─── Traveler confirms booking ────────────────────────────────────────────

    /// <summary>
    /// Traveler confirm booking → chuyển sang PendingPayment.
    /// POST /api/Bookings/{id}/confirm
    /// </summary>
    [HttpPost("{id:guid}/confirm")]
    [Authorize(Roles = "Traveler")]
    [SwaggerOperation(Summary = "Traveler xác nhận booking → chuyển sang PendingPayment")]
    public async Task<IActionResult> ConfirmByTraveler(Guid id)
    {
        try
        {
            var travelerUserId = GetCurrentUserIdOrThrow();
            var result = await _bookingService.ConfirmByTravelerAsync(travelerUserId, id);
            return Ok(ApiResponse<BookingResponseDto>.SuccessResponse(result, "Booking confirmed. Please complete payment."));
        }
        catch (KeyNotFoundException ex)      { return NotFound(ApiResponse.ErrorResponse(ex.Message)); }
        catch (UnauthorizedAccessException)  { return Forbid(); }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.ErrorResponse(ex.Message)); }
    }

    // ─── Get detail ───────────────────────────────────────────────────────────

    /// <summary>Lấy chi tiết một booking: GET /api/Bookings/{id}</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Lấy chi tiết một booking")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _bookingService.GetByIdAsync(id);
        if (result is null) return NotFound(ApiResponse.ErrorResponse("Booking not found."));
        return Ok(ApiResponse<BookingResponseDto>.SuccessResponse(result));
    }

    // ─── Lấy bookings của một trip (CS/Admin) ─────────────────────────────────

    /// <summary>Lấy tất cả bookings của một trip: GET /api/Trips/{tripId}/bookings</summary>
    [HttpGet("/api/Trips/{tripId:guid}/bookings")]
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "Lấy tất cả bookings của một trip (Admin/CS)")]
    public async Task<IActionResult> GetByTrip(Guid tripId)
    {
        var result = await _bookingService.GetByTripIdAsync(tripId);
        return Ok(ApiResponse<IEnumerable<BookingResponseDto>>.SuccessResponse(result));
    }

    // ─── My bookings ──────────────────────────────────────────────────────────

    /// <summary>Traveler lấy danh sách bookings của mình: GET /api/Bookings/my/traveler</summary>
    [HttpGet("my/traveler")]
    [Authorize(Roles = "Traveler")]
    [SwaggerOperation(Summary = "Traveler lấy danh sách bookings của mình")]
    public async Task<IActionResult> GetMyBookingsAsTraveler()
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _bookingService.GetMyBookingsAsTravelerAsync(userId);
        return Ok(ApiResponse<IEnumerable<BookingResponseDto>>.SuccessResponse(result));
    }

    /// <summary>Buddy lấy danh sách bookings của mình: GET /api/Bookings/my/buddy</summary>
    [HttpGet("my/buddy")]
    [Authorize(Roles = "Buddy")]
    [SwaggerOperation(Summary = "Buddy lấy danh sách bookings của mình")]
    public async Task<IActionResult> GetMyBookingsAsBuddy()
    {
        try
        {
            var userId = GetCurrentUserIdOrThrow();
            var result = await _bookingService.GetMyBookingsAsBuddyAsync(userId);
            return Ok(ApiResponse<IEnumerable<BookingResponseDto>>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex) { return NotFound(ApiResponse.ErrorResponse(ex.Message)); }
    }

    // ─── Admin / CS update status ─────────────────────────────────────────────

    /// <summary>Admin/CS cập nhật trạng thái booking: PATCH /api/Bookings/{id}/status</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "Admin/CS cập nhật trạng thái booking (state machine)")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] BookingUpdateStatusRequestDto dto)
    {
        try
        {
            var result = await _bookingService.UpdateStatusAsync(id, dto.Status);
            if (result is null) return NotFound(ApiResponse.ErrorResponse("Booking not found."));
            return Ok(ApiResponse<BookingResponseDto>.SuccessResponse(result));
        }
        catch (InvalidOperationException ex) { return BadRequest(ApiResponse.ErrorResponse(ex.Message)); }
    }
}
