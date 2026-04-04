using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.ServicePackageDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ServicePackagesController : ApiControllerBase
{
    private readonly IServicePackageService _servicePackageService;

    public ServicePackagesController(IServicePackageService servicePackageService)
    {
        _servicePackageService = servicePackageService;
    }

    // ─── PUBLIC ──────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả gói dịch vụ đang active (cho buddy xem và chọn gói)</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Lấy tất cả gói dịch vụ đang active")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<ServicePackageResponseDto>>))]
    public async Task<IActionResult> GetAllActive()
    {
        var result = await _servicePackageService.GetAllActiveAsync();
        return Ok(ApiResponse<IEnumerable<ServicePackageResponseDto>>.SuccessResponse(result, "Service packages fetched successfully."));
    }

    /// <summary>Lấy chi tiết một gói dịch vụ theo id</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Lấy chi tiết một gói dịch vụ")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<ServicePackageResponseDto>))]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _servicePackageService.GetByIdAsync(id);
        if (result is null) return NotFound("Service package not found.");
        return Ok(ApiResponse<ServicePackageResponseDto>.SuccessResponse(result, "Service package fetched successfully."));
    }

    // ─── ADMIN ───────────────────────────────────────────────────────────────

    /// <summary>Admin: Lấy tất cả gói (bao gồm inactive)</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    [SwaggerOperation(Summary = "Admin: Lấy tất cả gói dịch vụ (bao gồm inactive)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<ServicePackageResponseDto>>))]
    public async Task<IActionResult> GetAll()
    {
        var result = await _servicePackageService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<ServicePackageResponseDto>>.SuccessResponse(result, "All service packages fetched successfully."));
    }

    /// <summary>Admin: Tạo gói dịch vụ mới</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [SwaggerOperation(Summary = "Admin: Tạo gói dịch vụ mới")]
    [SwaggerResponse(201, "Created", typeof(ApiResponse<ServicePackageResponseDto>))]
    public async Task<IActionResult> Create([FromBody] ServicePackageCreateDto dto)
    {
        var result = await _servicePackageService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<ServicePackageResponseDto>.SuccessResponse(result, "Service package created successfully."));
    }

    /// <summary>Admin: Cập nhật gói dịch vụ</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Admin: Cập nhật gói dịch vụ")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<ServicePackageResponseDto>))]
    public async Task<IActionResult> Update(Guid id, [FromBody] ServicePackageUpdateDto dto)
    {
        var result = await _servicePackageService.UpdateAsync(id, dto);
        if (result is null) return NotFound("Service package not found.");
        return Ok(ApiResponse<ServicePackageResponseDto>.SuccessResponse(result, "Service package updated successfully."));
    }

    /// <summary>Admin: Xóa gói dịch vụ (soft-delete)</summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Admin: Xóa gói dịch vụ (soft-delete)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _servicePackageService.DeleteAsync(id);
        if (!ok) return NotFound("Service package not found.");
        return Ok(ApiResponse<object>.SuccessResponse("Service package deleted successfully."));
    }

    // ─── BUDDY SUBSCRIPTION + PAYMENT ────────────────────────────────────────

    /// <summary>
    /// Buddy đăng ký / nâng cấp / hạ cấp gói dịch vụ.
    /// - Chưa có gói → tạo PayPal/PayOS order (full price)
    /// - Upgrade → tạo PayPal/PayOS order (chênh lệch proration), kích hoạt ngay sau capture/webhook
    /// - Downgrade → schedule đổi gói khi hết kỳ hiện tại (không cần thanh toán)
    /// Body: { servicePackageId, preferredPaymentMethod: "PayPal" | "PayOS" }
    /// </summary>
    [Authorize(Roles = "Buddy")]
    [HttpPost("subscribe")]
    [SwaggerOperation(Summary = "Buddy đăng ký / upgrade / downgrade gói dịch vụ")]
    [SwaggerResponse(201, "Created — PayPal/PayOS order (upgrade/new)")]
    [SwaggerResponse(200, "OK — Downgrade scheduled")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserIdOrThrow();
            var result = await _servicePackageService.SubscribeAsync(userId, dto);

            // Downgrade → trả 200 OK (không cần thanh toán)
            if (result is DowngradeResponseDto downgrade)
            {
                return Ok(ApiResponse<DowngradeResponseDto>.SuccessResponse(downgrade, downgrade.Message));
            }

            // PayOS → trả 201 Created (cần quét QR)
            if (result is SubscriptionPayOsOrderResponseDto payOsResult)
            {
                return CreatedAtAction(nameof(GetMySubscription), new { },
                    ApiResponse<SubscriptionPayOsOrderResponseDto>.SuccessResponse(
                        payOsResult,
                        "PayOS order created. Redirect buddy to checkoutUrl or show QR code to complete payment."));
            }

            // PayPal → trả 201 Created (cần redirect approve)
            return CreatedAtAction(nameof(GetMySubscription), new { },
                ApiResponse<SubscriptionPayPalOrderResponseDto>.SuccessResponse(
                    (SubscriptionPayPalOrderResponseDto)result,
                    "PayPal order created. Redirect buddy to approveUrl to complete payment."));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse.ErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse.ErrorResponse(ex.Message));
        }
    }

    /// <summary>Capture PayPal payment → kích hoạt subscription.</summary>
    [Authorize(Roles = "Buddy")]
    [HttpPost("subscriptions/{subscriptionId:guid}/capture")]
    [SwaggerOperation(Summary = "Capture PayPal payment — kích hoạt subscription", Description = "Capture PayPal payment → kích hoạt subscription. preferredPaymentMethod = PayOS")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddySubscriptionResponseDto>))]
    [SwaggerResponse(400, "Bad Request", typeof(ApiResponse<object>))]
    public async Task<IActionResult> CapturePayment(Guid subscriptionId, [FromBody] SubscriptionCaptureRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserIdOrThrow();
            var result = await _servicePackageService.CaptureSubscriptionPaymentAsync(userId, subscriptionId, dto);
            return Ok(ApiResponse<BuddySubscriptionResponseDto>.SuccessResponse(result, "Payment captured. Subscription is now active."));
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

    /// <summary>
    /// Verify PayOS subscription payment và activate nếu đã thanh toán.
    /// Gọi endpoint này sau khi PayOS redirect về app (thay thế webhook khi dev local).
    /// </summary>
    [Authorize(Roles = "Buddy")]
    [HttpPost("subscriptions/{subscriptionId:guid}/verify-payos")]
    [SwaggerOperation(
        Summary = "Verify PayOS subscription payment",
        Description = "Gọi PayOS API để kiểm tra trạng thái thanh toán. Nếu PAID → activate subscription ngay. Dùng sau khi PayOS redirect về.")]
    [SwaggerResponse(200, "Success — subscription activated", typeof(ApiResponse<BuddySubscriptionResponseDto>))]
    [SwaggerResponse(400, "Payment not completed yet", typeof(ApiResponse<object>))]
    public async Task<IActionResult> VerifyPayOsSubscription(Guid subscriptionId)
    {
        try
        {
            var userId = GetCurrentUserIdOrThrow();
            var result = await _servicePackageService.VerifyPayOsSubscriptionAsync(userId, subscriptionId);
            return Ok(ApiResponse<BuddySubscriptionResponseDto>.SuccessResponse(result, "Payment verified. Subscription is now active."));
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

    /// <summary>Buddy xem gói dịch vụ hiện tại</summary>

    [Authorize(Roles = "Buddy")]
    [HttpGet("my-subscription")]
    [SwaggerOperation(Summary = "Buddy xem gói dịch vụ đang active")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddySubscriptionResponseDto>))]
    public async Task<IActionResult> GetMySubscription()
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _servicePackageService.GetMySubscriptionAsync(userId);
        if (result is null) return NotFound("No active subscription found.");
        return Ok(ApiResponse<BuddySubscriptionResponseDto>.SuccessResponse(result, "Current subscription fetched successfully."));
    }

    /// <summary>
    /// Buddy hủy gói — giữ active đến hết kỳ thanh toán, sau đó không gia hạn.
    /// </summary>
    [Authorize(Roles = "Buddy")]
    [HttpDelete("my-subscription")]
    [SwaggerOperation(Summary = "Buddy hủy gói (giữ đến hết kỳ thanh toán)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddySubscriptionResponseDto>))]
    public async Task<IActionResult> CancelMySubscription()
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _servicePackageService.CancelSubscriptionAsync(userId);
        if (result is null) return NotFound("No active subscription found.");
        return Ok(ApiResponse<BuddySubscriptionResponseDto>.SuccessResponse(
            result,
            $"Gói sẽ giữ active đến {result.EndDate:dd/MM/yyyy}. Sau đó sẽ không gia hạn."));
    }
}
