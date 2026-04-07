using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.BuddyDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class BuddiesController : ApiControllerBase
{
    private readonly IBuddyService _buddyService;

    public BuddiesController(IBuddyService buddyService)
    {
        _buddyService = buddyService;
    }

    // ─── PUBLIC ──────────────────────────────────────────────────────────────

    /// <summary>Lấy danh sách buddy đang active, kèm thông tin gói dịch vụ (nếu có)</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Lấy danh sách buddy đang active (kèm thông tin gói)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<BuddyResponseDto>>))]
    public async Task<IActionResult> GetAllActive()
    {
        var result = await _buddyService.GetAllActiveWithSubscriptionAsync();
        return Ok(ApiResponse<IEnumerable<BuddyResponseDto>>.SuccessResponse(result, "Buddies fetched successfully"));
    }

    /// <summary>Lấy chi tiết một buddy theo BuddyId</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Lấy chi tiết một buddy")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _buddyService.GetByIdAsync(id);
        if (result is null) return NotFound("Buddy not found.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, "Buddy fetched successfully"));
    }

    /// <summary>Lấy buddy theo UserId</summary>
    [HttpGet("by-user/{userId:guid}")]
    [SwaggerOperation(Summary = "Lấy buddy theo UserId")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> GetByUserId(Guid userId)
    {
        var result = await _buddyService.GetByUserIdAsync(userId);
        if (result is null) return NotFound("Buddy not found for this user.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, "Buddy fetched successfully"));
    }

    // ─── BUDDY (self) ─────────────────────────────────────────────────────────

    /// <summary>Buddy xem full profile của mình</summary>
    [Authorize(Roles = "Buddy")]
    [HttpGet("me")]
    [SwaggerOperation(Summary = "Buddy xem full profile của mình")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _buddyService.GetByUserIdAsync(userId);
        if (result is null) return NotFound("Buddy profile not found for current user.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, "Buddy profile updated successfully"));
    }

    /// <summary>
    /// Buddy cập nhật full profile (User info + Buddy fields) qua JSON.
    /// Activities gửi dạng mảng string, ví dụ: ["Shopping", "FoodAndDining", "ArtAndMuseums"].
    /// Khi gửi Activities, toàn bộ danh sách cũ sẽ được thay thế bằng danh sách mới.
    /// </summary>
    [Authorize(Roles = "Buddy")]
    [HttpPut("update-profile")]
    [SwaggerOperation(Summary = "Buddy cập nhật full profile (user info + buddy fields, JSON)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> UpdateMyProfile([FromBody] BuddySelfUpdateRequestDto dto)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _buddyService.UpdateSelfAsync(userId, dto);
        if (result is null) return NotFound("Buddy profile not found for current user.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, "Buddy avatar uploaded successfully"));
    }

    /// <summary>Buddy upload/thay đổi avatar (form-data, dùng trong update profile)</summary>
    [Authorize(Roles = "Buddy")]
    [HttpPost("upload-avatar")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "Buddy upload/thay đổi avatar (form-data, dùng trong update profile)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> UploadAvatar(IFormFile avatar)
    {
        var userId = GetCurrentUserIdOrThrow();
        var result = await _buddyService.UploadAvatarAsync(userId, avatar);
        if (result is null) return NotFound("Buddy profile not found for current user.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, "Buddy avatar uploaded successfully"));
    }

    // ─── ADMIN ────────────────────────────────────────────────────────────────

    /// <summary>User đăng ký làm buddy (chờ admin duyệt)</summary>
    [Authorize(Roles = "User")]
    [HttpPost("register-as-buddy")]
    [SwaggerOperation(Summary = "User đăng ký làm buddy (hồ sơ sẽ chờ admin duyệt)")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<string>))]
    public async Task<IActionResult> RegisterAsBuddy([FromBody] BuddyRegisterRequestDto dto)
    {
        var userId = GetCurrentUserIdOrThrow();
        var message = await _buddyService.RegisterAsync(userId, dto);
        return Ok(ApiResponse<string>.SuccessResponse(message, "Register successfully"));
    }

    /// <summary>Admin xem danh sách user đang apply làm buddy (chưa được duyệt)</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("pending-applicants")]
    [SwaggerOperation(Summary = "Admin xem danh sách user đang chờ duyệt làm buddy")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<BuddyResponseDto>>))]
    public async Task<IActionResult> GetPendingApplicants()
    {
        var result = await _buddyService.GetPendingApplicantsAsync();
        return Ok(ApiResponse<IEnumerable<BuddyResponseDto>>.SuccessResponse(result, "Pending applicants fetched successfully"));
    }

    /// <summary>Admin cập nhật buddy profile theo BuddyId</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Admin cập nhật buddy profile")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<BuddyResponseDto>))]
    public async Task<IActionResult> Update(Guid id, [FromBody] BuddyUpdateRequestDto dto)
    {
        var result = await _buddyService.UpdateAsync(id, dto);
        if (result is null) return NotFound("Buddy not found.");
        return Ok(ApiResponse<BuddyResponseDto>.SuccessResponse(result, "Buddy updated successfully"));
    }

    /// <summary>Admin xóa buddy profile</summary>
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Admin xóa buddy profile")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<object>))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _buddyService.DeleteAsync(id);
        if (!ok) return NotFound("Buddy not found.");
        return Ok(ApiResponse<object>.SuccessResponse("Buddy deleted successfully"));
    }
}
