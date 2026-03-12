using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.BookingDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BookingsController : ApiControllerBase
{
    private readonly IBookingService _bookingService;

    public BookingsController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    /// <summary>CS/Admin tạo booking cho một trip: POST /api/Trips/{tripId}/bookings</summary>
    [HttpPost("/api/Trips/{tripId:guid}/bookings")]
    [SwaggerOperation(Summary = "CS/Admin tạo booking cho một trip")]
    public async Task<IActionResult> CreateForTrip(Guid tripId, [FromBody] BookingCreateRequestDto dto)
    {
        dto.TripId = tripId;
        var userId = GetCurrentUserIdOrThrow();
        var result = await _bookingService.CreateForTripAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<BookingResponseDto>.SuccessResponse(result));
    }

    /// <summary>Lấy tất cả bookings của một trip: GET /api/Trips/{tripId}/bookings</summary>
    [HttpGet("/api/Trips/{tripId:guid}/bookings")]
    [SwaggerOperation(Summary = "Lấy tất cả bookings của một trip")]
    public async Task<IActionResult> GetByTrip(Guid tripId)
    {
        var result = await _bookingService.GetByTripIdAsync(tripId);
        return Ok(ApiResponse<IEnumerable<BookingResponseDto>>.SuccessResponse(result));
    }

    /// <summary>Lấy chi tiết một booking: GET /api/Bookings/{id}</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Lấy chi tiết một booking")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _bookingService.GetByIdAsync(id);
        if (result is null) return NotFound("Booking not found.");
        return Ok(ApiResponse<BookingResponseDto>.SuccessResponse(result));
    }

    /// <summary>Cập nhật trạng thái booking: PATCH /api/Bookings/{id}/status</summary>
    [HttpPatch("{id:guid}/status")]
    [SwaggerOperation(Summary = "Cập nhật trạng thái booking")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] BookingUpdateStatusRequestDto dto)
    {
        try
        {
            var result = await _bookingService.UpdateStatusAsync(id, dto.Status);
            if (result is null) return NotFound("Booking not found.");
            return Ok(ApiResponse<BookingResponseDto>.SuccessResponse(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
