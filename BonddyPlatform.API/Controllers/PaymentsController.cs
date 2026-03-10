using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.PaymentDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize(Roles = "Admin,CS")]
[Route("api/[controller]")]
[ApiController]
public class PaymentsController : ApiControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    /// <summary>Tạo payment record cho một booking: POST /api/Bookings/{bookingId}/payment</summary>
    [HttpPost("/api/Bookings/{bookingId:guid}/payment")]
    [SwaggerOperation(Summary = "Tạo payment cho một booking")]
    public async Task<IActionResult> Create(Guid bookingId, [FromBody] PaymentCreateRequestDto dto)
    {
        dto.BookingId = bookingId;
        try
        {
            var result = await _paymentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                ApiResponse<PaymentResponseDto>.SuccessResponse(result));
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

    /// <summary>Lấy payment của một booking: GET /api/Bookings/{bookingId}/payment</summary>
    [HttpGet("/api/Bookings/{bookingId:guid}/payment")]
    [SwaggerOperation(Summary = "Lấy payment của một booking")]
    public async Task<IActionResult> GetByBooking(Guid bookingId)
    {
        var result = await _paymentService.GetByBookingIdAsync(bookingId);
        if (result is null) return NotFound("Payment not found.");
        return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result));
    }

    /// <summary>Lấy payment theo id: GET /api/Payments/{id}</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Lấy payment theo id")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _paymentService.GetByIdAsync(id);
        if (result is null) return NotFound("Payment not found.");
        return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result));
    }

    /// <summary>Chuyển trạng thái payment (escrow): PATCH /api/Payments/{id}/status</summary>
    [HttpPatch("{id:guid}/status")]
    [SwaggerOperation(Summary = "Chuyển trạng thái payment (escrow)")]
    public async Task<IActionResult> Transition(Guid id, [FromBody] PaymentTransitionRequestDto dto)
    {
        try
        {
            var result = await _paymentService.TransitionAsync(id, dto);
            if (result is null) return NotFound("Payment not found.");
            return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
