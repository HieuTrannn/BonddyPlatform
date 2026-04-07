using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.BookingDtos;
using BonddyPlatform.Services.DTOs.BuddyDtos;
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
    private readonly IBuddyService _buddyService;
    private readonly IReviewService _reviewService;
    private readonly IIncidentService _incidentService;

    public AdminController(
        IUserService userService,
        ITripService tripService,
        IBookingService bookingService,
        IBuddyService buddyService,
        IReviewService reviewService,
        IIncidentService incidentService)
    {
        _userService = userService;
        _tripService = tripService;
        _bookingService = bookingService;
        _buddyService = buddyService;
        _reviewService = reviewService;
        _incidentService = incidentService;
    }

    // ─── USERS ───────────────────────────────────────────────────────────────

    [HttpGet("users")]
    [SwaggerOperation(Summary = "Admin: lấy danh sách users (paged)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetUsers([FromQuery] UserSearchRequestDto request)
    {
        var result = await _userService.GetPagedAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(result, "Users fetched successfully"));
    }

    [HttpPost("users")]
    [SwaggerOperation(Summary = "Admin: bulk tạo nhiều tài khoản user mới (không cần verify email)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BulkCreateUserResultDto>))]
    public async Task<IActionResult> CreateUsers([FromBody] List<UserCreateRequestDto> dtos)
    {
        var result = await _userService.AdminCreateUserAsync(dtos);
        return Ok(ApiResponse<BulkCreateUserResultDto>.SuccessResponse(result,
            $"Bulk create completed: {result.TotalSucceeded} succeeded, {result.TotalFailed} failed."));
    }

    [HttpGet("users/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: lấy chi tiết một user")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<UserResponseDto>))]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        if (result is null) return NotFound("User not found.");
        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(result, "User fetched successfully"));
    }

    [HttpPut("users/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: cập nhật thông tin user")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<UserResponseDto>))]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UserUpdateRequestDto dto)
    {
        var result = await _userService.UpdateAsync(id, dto);
        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(result, "User updated successfully"));
    }

    [HttpPatch("users/{id:guid}/toggle-status")]
    [SwaggerOperation(Summary = "Admin: toggle trạng thái user (Active ↔ Inactive)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(404, "User not found")]
    public async Task<IActionResult> ToggleUserStatus(Guid id)
    {
        var result = await _userService.ToggleActiveAsync(id);
        if (result is null) return NotFound("User not found.");
        var status = result.IsActive ? "Active" : "Inactive";
        return Ok(ApiResponse<UserResponseDto>.SuccessResponse(result, $"User status changed to {status}."));
    }

    // ─── TRIPS ───────────────────────────────────────────────────────────────

    [HttpGet("trips")]
    [SwaggerOperation(Summary = "Admin: lấy danh sách trips (cho CS/Admin)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetTrips([FromQuery] TripSearchRequestDto request)
    {
        var result = await _tripService.GetTripsForOpsAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(result, "Trips fetched successfully"));
    }

    [HttpGet("trips/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: lấy chi tiết một trip")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<TripResponseDto>))]
    public async Task<IActionResult> GetTrip(Guid id)
    {
        var result = await _tripService.GetByIdAsync(id);
        if (result is null) return NotFound("Trip not found.");
        return Ok(ApiResponse<TripResponseDto>.SuccessResponse(result, "Trip fetched successfully"));
    }

    // ─── BOOKINGS ────────────────────────────────────────────────────────────

    [HttpGet("bookings")]
    [SwaggerOperation(Summary = "Admin: lấy tất cả bookings (paged, search, filter)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetAllBookings([FromQuery] BookingSearchRequestDto request)
    {
        var result = await _bookingService.GetAllPagedAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(result, "Bookings fetched successfully"));
    }

    [HttpGet("trips/{tripId:guid}/bookings")]
    [SwaggerOperation(Summary = "Admin: lấy danh sách bookings theo trip")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<BookingResponseDto>>))]
    public async Task<IActionResult> GetBookingsByTrip(Guid tripId)
    {
        var result = await _bookingService.GetByTripIdAsync(tripId);
        return Ok(ApiResponse<IEnumerable<BookingResponseDto>>.SuccessResponse(result, "Bookings fetched successfully"));
    }

    [HttpGet("bookings/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: lấy chi tiết một booking")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BookingResponseDto>))]
    public async Task<IActionResult> GetBooking(Guid id)
    {
        var result = await _bookingService.GetByIdAsync(id);
        if (result is null) return NotFound("Booking not found.");
        return Ok(ApiResponse<BookingResponseDto>.SuccessResponse(result, "Booking fetched successfully"));
    }

    [HttpPatch("bookings/{id:guid}/status")]
    [SwaggerOperation(Summary = "Admin: cập nhật trạng thái booking")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BookingResponseDto>))]
    public async Task<IActionResult> UpdateBookingStatus(Guid id, [FromBody] BookingUpdateStatusRequestDto dto)
    {
        try
        {
            var result = await _bookingService.UpdateStatusAsync(id, dto.Status);
            if (result is null) return NotFound("Booking not found.");
            return Ok(ApiResponse<BookingResponseDto>.SuccessResponse(result, "Booking status updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    // ─── BUDDIES ─────────────────────────────────────────────────────────────

    [HttpGet("buddies")]
    [SwaggerOperation(Summary = "Admin: lấy danh sách buddies (paged)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<object>))]
    public async Task<IActionResult> GetBuddies([FromQuery] BuddySearchRequestDto request)
    {
        var result = await _buddyService.GetPagedAsync(request);
        return Ok(ApiResponse<object>.SuccessResponse(result, "Buddies fetched successfully"));
    }

    /// <summary>Lấy danh sách buddy đã mua gói dịch vụ (có active subscription)</summary>
    [HttpGet("buddies/with-subscription")]
    [SwaggerOperation(Summary = "Lấy danh sách buddy đã mua gói dịch vụ")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<BuddyResponseDto>>))]
    public async Task<IActionResult> GetBuddiesWithSubscription()
    {
        var result = await _buddyService.GetBuddiesWithActiveSubscriptionAsync();
        return Ok(ApiResponse<IEnumerable<BuddyResponseDto>>.SuccessResponse(result, "Buddies with subscription fetched successfully"));
    }

    [HttpGet("buddies/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: lấy chi tiết một buddy")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> GetBuddy(Guid id)
    {
        var result = await _buddyService.GetByIdAsync(id);
        if (result is null) return NotFound("Buddy not found.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, "Buddy fetched successfully"));
    }

    [HttpPost("buddies/{userId:guid}")]
    [SwaggerOperation(Summary = "Admin: đăng ký buddy profile mới cho một user")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> RegisterBuddy(Guid userId, [FromBody] BuddyRegisterRequestDto dto)
    {
        var result = await _buddyService.RegisterAsync(userId, dto);
        return Ok(ApiResponse<string>.SuccessResponse(result, "Buddy registered successfully"));
    }

    [HttpPut("buddies/{id:guid}")]
    [SwaggerOperation(Summary = "Admin: cập nhật buddy")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> UpdateBuddy(Guid id, [FromBody] BuddyUpdateRequestDto dto)
    {
        var result = await _buddyService.UpdateAsync(id, dto);
        if (result is null) return NotFound("Buddy not found.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, "Buddy updated successfully"));
    }

    [HttpPatch("buddies/{id:guid}/toggle-status")]
    [SwaggerOperation(Summary = "Admin: toggle trạng thái buddy (Active ↔ Inactive)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    [SwaggerResponse(404, "Buddy not found")]
    public async Task<IActionResult> ToggleBuddyStatus(Guid id)
    {
        var result = await _buddyService.ToggleActiveAsync(id);
        if (result is null) return NotFound("Buddy not found.");
        var status = result.IsActive ? "Active" : "Inactive";
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, $"Buddy status changed to {status}."));
    }

    /// <summary>Admin duyệt hồ sơ đăng ký buddy: kích hoạt profile và cấp role Buddy</summary>
    [HttpPatch("buddies/{id:guid}/approve")]
    [SwaggerOperation(Summary = "Admin: duyệt đăng ký làm buddy (cấp role Buddy + IsActive = true)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    [SwaggerResponse(404, "Buddy not found")]
    public async Task<IActionResult> ApproveBuddy(Guid id)
    {
        var result = await _buddyService.ApproveBuddyAsync(id);
        if (result is null) return NotFound("Buddy not found.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result,
            "Buddy approved successfully."));
    }

    // ─── REVIEWS ─────────────────────────────────────────────────────────────

    [HttpGet("bookings/{bookingId:guid}/review")]
    [SwaggerOperation(Summary = "Admin: xem review của một booking")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<ReviewResponseDto>))]
    public async Task<IActionResult> GetReview(Guid bookingId)
    {
        var result = await _reviewService.GetByBookingIdAsync(bookingId);
        if (result is null) return NotFound("Review not found.");
        return Ok(ApiResponse<ReviewResponseDto>.SuccessResponse(result, "Review fetched successfully"));
    }

    // ─── INCIDENTS ───────────────────────────────────────────────────────────

    [HttpGet("bookings/{bookingId:guid}/incidents")]
    [SwaggerOperation(Summary = "Admin: xem danh sách incidents của một booking")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<IncidentResponseDto>>))]
    public async Task<IActionResult> GetIncidents(Guid bookingId)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _incidentService.GetByBookingIdAsync(userId, bookingId);
        return Ok(ApiResponse<IEnumerable<IncidentResponseDto>>.SuccessResponse(result, "Incidents fetched successfully"));
    }

    [HttpPatch("incidents/{id:guid}/resolve")]
    [SwaggerOperation(Summary = "Admin: cập nhật trạng thái incident")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IncidentResponseDto>))]
    public async Task<IActionResult> ResolveIncident(Guid id, [FromBody] IncidentResolveRequestDto dto)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _incidentService.ResolveAsync(userId, id, dto);
        if (result is null) return NotFound("Incident not found.");
        return Ok(ApiResponse<IncidentResponseDto>.SuccessResponse(result, "Incident resolved successfully"));
    }
}
