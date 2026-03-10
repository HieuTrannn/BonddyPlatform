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
    public Task<IActionResult> Create([FromBody] TripCreateRequestDto dto)
    {
        var travelerUserId = GetCurrentUserIdOrThrow();
        return ExecuteAsync(() => _tripService.CreateAsync(travelerUserId, dto));
    }

    [HttpGet("my")]
    [Authorize(Roles = nameof(Role.User))]
    [SwaggerOperation(Summary = "Get my trips (traveler)", Description = "Returns trips created by the current traveler.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<TripResponseDto>>))]
    public Task<IActionResult> GetMyTrips([FromQuery] TripSearchRequestDto request)
    {
        var travelerUserId = GetCurrentUserIdOrThrow();
        return ExecuteAsync(() => _tripService.GetMyTripsAsync(travelerUserId, request));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "Get trips for CS/Admin", Description = "List trips with filters for CS and Admin.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<TripResponseDto>>))]
    public Task<IActionResult> GetForOps([FromQuery] TripSearchRequestDto request)
        => ExecuteAsync(() => _tripService.GetTripsForOpsAsync(request));

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get trip by id", Description = "Traveler can access own trip, CS/Admin can access any trip.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<TripResponseDto>))]
    [SwaggerResponse(404, "Not found", typeof(ApiResponse))]
    public Task<IActionResult> GetById(Guid id)
    {
        var currentUserId = GetCurrentUserIdOrThrow();
        var currentRole = GetClaimValue(System.Security.Claims.ClaimTypes.Role);

        return ExecuteAsync(async () =>
        {
            var trip = await _tripService.GetByIdAsync(id);
            if (trip == null) throw new KeyNotFoundException("Trip not found.");

            var isTraveler = string.Equals(currentRole, nameof(Role.User), StringComparison.OrdinalIgnoreCase);
            var isOps = string.Equals(currentRole, nameof(Role.Admin), StringComparison.OrdinalIgnoreCase)
                        || string.Equals(currentRole, nameof(Role.CS), StringComparison.OrdinalIgnoreCase);

            if (isTraveler && trip.TravelerUserId != currentUserId)
                throw new UnauthorizedAccessException("You are not allowed to view this trip.");

            if (!isTraveler && !isOps)
                throw new UnauthorizedAccessException("You are not allowed to view this trip.");

            return trip;
        });
    }
}
