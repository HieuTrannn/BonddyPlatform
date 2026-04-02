using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.PaymentDtos;
using BonddyPlatform.Services.Interfaces;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize]
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
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "Tạo payment cho một booking")]
    [SwaggerResponse(201, "Created", typeof(ApiResponse<PaymentResponseDto>))]
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
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "Lấy payment của một booking")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PaymentResponseDto>))]
    public async Task<IActionResult> GetByBooking(Guid bookingId)
    {
        var result = await _paymentService.GetByBookingIdAsync(bookingId);
        if (result is null) return NotFound("Payment not found.");
        return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result));
    }

    /// <summary>Lấy payment theo id: GET /api/Payments/{id}</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "Lấy payment theo id")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PaymentResponseDto>))]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _paymentService.GetByIdAsync(id);
        if (result is null) return NotFound("Payment not found.");
        return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result));
    }

    /// <summary>Chuyển trạng thái payment (escrow): PATCH /api/Payments/{id}/status</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,CS")]
    [SwaggerOperation(Summary = "Chuyển trạng thái payment (escrow)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PaymentResponseDto>))]
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

    /// <summary>Create a PayPal order for current user's booking.</summary>
    [HttpPost("paypal/bookings/{bookingId:guid}/create-order")]
    [HttpPost("/api/Bookings/{bookingId:guid}/payment/paypal/create-order")]
    [SwaggerOperation(Summary = "Create PayPal order")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PayPalCreateOrderResponseDto>))]
    public async Task<IActionResult> CreatePayPalOrder(Guid bookingId)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _paymentService.CreatePayPalOrderAsync(bookingId, userId);
        return Ok(ApiResponse<PayPalCreateOrderResponseDto>.SuccessResponse(result, "PayPal order created."));
    }

    /// <summary>Capture PayPal order after customer approves payment.</summary>
    [HttpPost("paypal/{paymentId:guid}/capture")]
    [HttpPost("/api/Bookings/payments/{paymentId:guid}/paypal/capture")]
    [SwaggerOperation(Summary = "Capture PayPal order")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PaymentResponseDto>))]
    [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<PaymentResponseDto>))]
    public async Task<IActionResult> CapturePayPalOrder(Guid paymentId, [FromBody] PayPalCaptureRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserIdOrThrow();
            var result = await _paymentService.CapturePayPalOrderAsync(paymentId, dto.OrderId, userId);
            return Ok(ApiResponse<PaymentResponseDto>.SuccessResponse(result, "PayPal payment captured."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.ErrorResponse(ex.Message));
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>PayPal webhook endpoint (must be publicly accessible).</summary>
    [AllowAnonymous]
    [HttpPost("paypal/webhook")]
    [SwaggerOperation(Summary = "PayPal webhook")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PayPalWebhookResultDto>))]
    public async Task<IActionResult> PayPalWebhook()
    {
        Request.EnableBuffering();
        string rawBody;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
        }

        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var result = await _paymentService.HandlePayPalWebhookAsync(rawBody, headers);
        return Ok(ApiResponse<PayPalWebhookResultDto>.SuccessResponse(result, result.Message));
    }
}
