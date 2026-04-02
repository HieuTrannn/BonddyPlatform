using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.ReviewDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class ReviewsController : ApiControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    /// <summary>Traveler tạo review cho booking đã Completed: POST /api/Bookings/{bookingId}/review</summary>
    [HttpPost("/api/Bookings/{bookingId:guid}/review")]
    [SwaggerOperation(Summary = "Traveler tạo review cho booking đã Completed")]
    [Authorize(Roles = "User")]
    [SwaggerResponse(201, "Created", typeof(ApiResponse<ReviewResponseDto>))]
    public async Task<IActionResult> Create(Guid bookingId, [FromBody] ReviewCreateRequestDto dto)
    {
        dto.BookingId = bookingId;
        var userId = GetCurrentUserIdOrThrow();

        try
        {
            var result = await _reviewService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetByBooking), new { bookingId },
                ApiResponse<ReviewResponseDto>.SuccessResponse(result));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Lấy review của booking: GET /api/Bookings/{bookingId}/review</summary>
    [HttpGet("/api/Bookings/{bookingId:guid}/review")]
    [SwaggerOperation(Summary = "Lấy review của một booking")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<ReviewResponseDto>))]
    public async Task<IActionResult> GetByBooking(Guid bookingId)
    {
        var result = await _reviewService.GetByBookingIdAsync(bookingId);
        if (result is null) return NotFound("Review not found.");
        return Ok(ApiResponse<ReviewResponseDto>.SuccessResponse(result));
    }

    /// <summary>
    /// Get public reviews for a buddy profile with paging.
    /// GET /api/Buddies/{buddyId}/reviews?page=1&pageSize=10
    /// </summary>
    [HttpGet("/api/Buddies/{buddyId:guid}/reviews")]
    [SwaggerOperation(Summary = "Get public reviews for a buddy (paging)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<ReviewResponseDto>>))]
    public async Task<IActionResult> GetPublicByBuddy(Guid buddyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _reviewService.GetPublicByBuddyIdAsync(buddyId, page, pageSize);
        return Ok(ApiResponse<PagedResultDto<ReviewResponseDto>>.SuccessResponse(result));
    }
}
