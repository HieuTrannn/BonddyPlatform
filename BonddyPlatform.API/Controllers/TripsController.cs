using BonddyPlatform.API.Common;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.TripDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class TripsController : ApiControllerBase
{
    private readonly ITripService _tripService;

    public TripsController(ITripService tripService)
    {
        _tripService = tripService;
    }

    [HttpPost]
    [Authorize(Roles = nameof(Role.User))]
    [SwaggerOperation(Summary = "Create trip (traveler)", Description = "Traveler creates a new trip and the system auto-creates a chat room.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<TripResponseDto>))]
    public async Task<IActionResult> Create([FromBody] TripCreateRequestDto dto)
    {
        var travelerUserId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<TripResponseDto>.SuccessResponse(await _tripService.CreateAsync(travelerUserId, dto), "Trip created successfully"));
    }

    [HttpGet("my")]
    [Authorize(Roles = nameof(Role.User))]
    [SwaggerOperation(Summary = "Get my trips (traveler)", Description = "Returns trips created by the current traveler.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<TripResponseDto>>))]
    public async Task<IActionResult> GetMyTrips([FromQuery] TripSearchRequestDto request)
    {
        var travelerUserId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<PagedResultDto<TripResponseDto>>.SuccessResponse(await _tripService.GetMyTripsAsync(travelerUserId, request), "Trips fetched successfully"));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "Get trips for CS/Admin", Description = "List trips with filters for CS and Admin.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<TripResponseDto>>))]
    public async Task<IActionResult> GetForOps([FromQuery] TripSearchRequestDto request)
        => Ok(ApiResponse<PagedResultDto<TripResponseDto>>.SuccessResponse(await _tripService.GetTripsForOpsAsync(request), "Trips fetched successfully"));

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(Role.User))]
    [SwaggerOperation(Summary = "Update trip (traveler)", Description = "Traveler edits trip details while it is still in Submitted status.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<TripResponseDto>))]
    public async Task<IActionResult> Update(Guid id, [FromBody] TripUpdateRequestDto dto)
    {
        var travelerUserId = GetCurrentUserIdOrThrow();
        return Ok(ApiResponse<TripResponseDto>.SuccessResponse(await _tripService.UpdateAsync(id, travelerUserId, dto), "Trip updated successfully"));
    }

    [HttpGet("open")]
    [Authorize(Roles = nameof(Role.Buddy))]
    [SwaggerOperation(Summary = "Get open trips (buddy)", Description = "Returns Submitted trips that buddies can browse and send requests to.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<TripResponseDto>>))]
    public async Task<IActionResult> GetOpenTrips([FromQuery] TripSearchRequestDto request)
        => Ok(ApiResponse<PagedResultDto<TripResponseDto>>.SuccessResponse(await _tripService.GetOpenTripsForBuddyAsync(request), "Open trips fetched successfully"));

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get trip by id", Description = "Traveler can access own trip, CS/Admin can access any trip.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<TripResponseDto>))]
    [SwaggerResponse(404, "Not found", typeof(ApiResponse))]
    public async Task<IActionResult> GetById(Guid id)
    {
        var travelerUserId = GetCurrentUserIdOrThrow();
        var trip = await _tripService.GetByIdAsync(id);
        if (trip == null) return NotFound(ApiResponse.ErrorResponse("Trip not found."));
        return Ok(ApiResponse<TripResponseDto>.SuccessResponse(trip, "Trip fetched successfully"));
    }

    /// <summary>
    /// Traveler xóa trip (soft delete — chỉ đổi status sang Deleted).
    /// Không cho phép xóa nếu đang có booking active.
    /// DELETE /api/Trips/{id}
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(Role.User))]
    [SwaggerOperation(Summary = "Delete trip (soft delete)", Description = "Sets trip status to Deleted. Not allowed if trip has active bookings.")]
    [SwaggerResponse(200, "Deleted successfully", typeof(ApiResponse))]
    [SwaggerResponse(400, "Has active bookings", typeof(ApiResponse))]
    [SwaggerResponse(404, "Not found", typeof(ApiResponse))]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var travelerUserId = GetCurrentUserIdOrThrow();
            await _tripService.DeleteAsync(id, travelerUserId);
            return Ok(ApiResponse.SuccessResponse("Trip deleted successfully."));
        }
        catch (KeyNotFoundException ex)        { return NotFound(ApiResponse.ErrorResponse(ex.Message)); }
        catch (UnauthorizedAccessException)   { return Forbid(); }
        catch (InvalidOperationException ex)   { return BadRequest(ApiResponse.ErrorResponse(ex.Message)); }
    }
}
