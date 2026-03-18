using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.IncidentDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
public class IncidentsController : ApiControllerBase
{
    private readonly IIncidentService _incidentService;

    public IncidentsController(IIncidentService incidentService)
    {
        _incidentService = incidentService;
    }

    /// <summary>Báo cáo sự cố: POST /api/Bookings/{bookingId}/incidents</summary>
    [HttpPost("/api/Bookings/{bookingId:guid}/incidents")]
    [SwaggerOperation(Summary = "Traveler/CS/Buddy báo cáo sự cố trên booking")]
    public async Task<IActionResult> Create(Guid bookingId, [FromBody] IncidentCreateRequestDto dto)
    {
        dto.BookingId = bookingId;
        var userId = GetCurrentUserIdOrThrow();

        try
        {
            var result = await _incidentService.CreateAsync(userId, dto);
            return CreatedAtAction(nameof(GetByBooking), new { bookingId },
                ApiResponse<IncidentResponseDto>.SuccessResponse(result));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>Lấy danh sách sự cố theo booking: GET /api/Bookings/{bookingId}/incidents</summary>
    [HttpGet("/api/Bookings/{bookingId:guid}/incidents")]
    [SwaggerOperation(Summary = "Lấy danh sách sự cố theo booking")]
    public async Task<IActionResult> GetByBooking(Guid bookingId)
    {
        var userId = GetCurrentUserIdOrThrow();
        try
        {
            var result = await _incidentService.GetByBookingIdAsync(userId, bookingId);
            return Ok(ApiResponse<IEnumerable<IncidentResponseDto>>.SuccessResponse(result));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>CS/Admin cập nhật trạng thái sự cố: PATCH /api/Incidents/{id}/resolve</summary>
    [HttpPatch("{id:guid}/resolve")]
    [SwaggerOperation(Summary = "CS/Admin cập nhật trạng thái sự cố")]
    [Authorize(Roles = "Admin,CS")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] IncidentResolveRequestDto dto)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _incidentService.ResolveAsync(userId, id, dto);
        if (result is null) return NotFound("Incident not found.");
        return Ok(ApiResponse<IncidentResponseDto>.SuccessResponse(result));
    }

    /// <summary>
    /// CS/Admin list incidents with filters and paging.
    /// GET /api/Incidents?status=Open&type=NoShow&page=1&pageSize=20
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "CS/Admin list incidents (paging + filters)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<IncidentResponseDto>>))]
    public async Task<IActionResult> Search([FromQuery] IncidentSearchRequestDto request)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _incidentService.SearchForOpsAsync(userId, request);
        return Ok(ApiResponse<PagedResultDto<IncidentResponseDto>>.SuccessResponse(result));
    }
}
