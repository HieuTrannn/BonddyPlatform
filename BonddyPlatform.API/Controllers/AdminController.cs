using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.BookingDtos;
using BonddyPlatform.Services.DTOs.GuideDtos;
using BonddyPlatform.Services.DTOs.IncidentDtos;
using BonddyPlatform.Services.DTOs.ReviewDtos;
using BonddyPlatform.Services.DTOs.TripDtos;
using BonddyPlatform.Services.DTOs.UserDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize(Roles = "Admin")]
[Route("api/admin")]
[ApiController]
public class AdminController : ApiControllerBase
{
    private readonly IUserService _userService;
    private readonly ITripService _tripService;
    private readonly IBookingService _bookingService;
    private readonly IGuideService _guideService;
    private readonly IReviewService _reviewService;
    private readonly IIncidentService _incidentService;

    public AdminController(
        IUserService userService,
        ITripService tripService,
        IBookingService bookingService,
        IGuideService guideService,
        IReviewService reviewService,
        IIncidentService incidentService)
    {
        _userService = userService;
        _tripService = tripService;
        _bookingService = bookingService;
        _guideService = guideService;
        _reviewService = reviewService;
        _incidentService = incidentService;
    }

    // ─── USERS ───────────────────────────────────────────────────────────────

    [HttpGet("users")]
    [SwaggerOperation(Summary = "Admin: lấy danh sách users (paged)")]
    public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequestDto request)
    {
        var result = await _userService.GetPagedAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpGet("users/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: lấy chi tiết một user")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        if (result is null) return NotFound("User not found.");
        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(result));
    }

    [HttpPut("users/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: cập nhật thông tin user")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateRequestDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(result));
    }

    [HttpDelete("users/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: xóa user")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        await _userService.DeleteAsync(id);
        return Ok(ApiResponse<object>.SuccessResponse());
    }

    // ─── TRIPS ───────────────────────────────────────────────────────────────

    [HttpGet("trips")]
    [SwaggerOperation(Summary = "Admin: lấy danh sách trips (cho CS/Admin)")]
    public async Task<IActionResult> GetTrips([FromQuery] TripSearchRequestDto request)
    {
        var result = await _tripService.GetTripsForOpsAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [HttpGet("trips/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: lấy chi tiết một trip")]
    public async Task<IActionResult> GetTrip(Guid id)
    {
        var result = await _tripService.GetByIdAsync(id);
        if (result is null) return NotFound("Trip not found.");
        return Ok(ApiResponse<TripResponseDto>.SuccessResponse(result));
    }

    // ─── BOOKINGS ────────────────────────────────────────────────────────────

    [HttpGet("trips/{tripId:guid}/bookings")]
    [SwaggerOperation(Summary = "Admin: lấy danh sách bookings theo trip")]
    public async Task<IActionResult> GetBookingsByTrip(Guid tripId)
    {
        var result = await _bookingService.GetByTripIdAsync(tripId);
        return Ok(ApiResponse<IEnumerable<BookingResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("bookings/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: lấy chi tiết một booking")]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var result = await _bookingService.GetByIdAsync(id);
        if (result is null) return NotFound("Booking not found.");
        return Ok(ApiResponse<BookingResponseDto>.SuccessResponse(result));
    }

    [HttpPatch("bookings/{id:guid}/status")]
    [SwaggerOperation(Summary = "Admin: cập nhật trạng thái booking")]
    public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromBody] BookingUpdateStatusRequestDto dto)
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

    // ─── GUIDES ──────────────────────────────────────────────────────────────

    [HttpGet("guides")]
    [SwaggerOperation(Summary = "Admin: lấy danh sách guides")]
    public async Task<IActionResult> GetGuides()
    {
        var result = await _guideService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<GuideResponseDto>>.SuccessResponse(result));
    }

    [HttpGet("guides/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: lấy chi tiết một guide")]
    public async Task<IActionResult> GetGuide(Guid id)
    {
        var result = await _guideService.GetByIdAsync(id);
        if (result is null) return NotFound("Guide not found.");
        return Ok(ApiResponse<GuideResponseDto>.SuccessResponse(result));
    }

    [HttpPost("guides")]
    [SwaggerOperation(Summary = "Admin: tạo guide mới")]
    public async Task<IActionResult> CreateGuide([FromBody] GuideCreateRequestDto dto)
    {
        var result = await _guideService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetGuide), new { id = result.Id },
            ApiResponse<GuideResponseDto>.SuccessResponse(result));
    }

    [HttpPut("guides/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: cập nhật guide")]
    public async Task<IActionResult> UpdateGuide(Guid id, [FromBody] GuideUpdateRequestDto dto)
    {
        var result = await _guideService.UpdateAsync(id, dto);
        if (result is null) return NotFound("Guide not found.");
        return Ok(ApiResponse<GuideResponseDto>.SuccessResponse(result));
    }

    [HttpDelete("guides/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: xóa guide")]
    public async Task<IActionResult> DeleteGuide(Guid id)
    {
        var ok = await _guideService.DeleteAsync(id);
        if (!ok) return NotFound("Guide not found.");
        return Ok(ApiResponse<object>.SuccessResponse());
    }

    // ─── REVIEWS ─────────────────────────────────────────────────────────────

    [HttpGet("bookings/{bookingId:guid}/review")]
    [SwaggerOperation(Summary = "Admin: xem review của một booking")]
    public async Task<IActionResult> GetReview(Guid bookingId)
    {
        var result = await _reviewService.GetByBookingIdAsync(bookingId);
        if (result is null) return NotFound("Review not found.");
        return Ok(ApiResponse<ReviewResponseDto>.SuccessResponse(result));
    }

    // ─── INCIDENTS ───────────────────────────────────────────────────────────

    [HttpGet("bookings/{bookingId:guid}/incidents")]
    [SwaggerOperation(Summary = "Admin: xem danh sách incidents của một booking")]
    public async Task<IActionResult> GetIncidents(Guid bookingId)
    {
        var result = await _incidentService.GetByBookingIdAsync(bookingId);
        return Ok(ApiResponse<IEnumerable<IncidentResponseDto>>.SuccessResponse(result));
    }

    [HttpPatch("incidents/{id:guid}/resolve")]
    [SwaggerOperation(Summary = "Admin: cập nhật trạng thái incident")]
    public async Task<IActionResult> ResolveIncident(Guid id, [FromBody] IncidentResolveRequestDto dto)
    {
        var result = await _incidentService.ResolveAsync(id, dto);
        if (result is null) return NotFound("Incident not found.");
        return Ok(ApiResponse<IncidentResponseDto>.SuccessResponse(result));
    }
}
