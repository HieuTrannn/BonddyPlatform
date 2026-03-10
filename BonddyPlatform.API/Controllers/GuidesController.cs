using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.GuideDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Authorize(Roles = "Admin,CS")]
[Route("api/[controller]")]
[ApiController]
public class GuidesController : ApiControllerBase
{
    private readonly IGuideService _guideService;

    public GuidesController(IGuideService guideService)
    {
        _guideService = guideService;
    }

    /// <summary>Lấy danh sách tất cả guides</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Lấy danh sách tất cả guides")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _guideService.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<GuideResponseDto>>.SuccessResponse(result));
    }

    /// <summary>Lấy chi tiết một guide</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Lấy chi tiết một guide")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _guideService.GetByIdAsync(id);
        if (result is null) return NotFound("Guide not found.");
        return Ok(ApiResponse<GuideResponseDto>.SuccessResponse(result));
    }

    /// <summary>Tạo guide mới</summary>
    [HttpPost]
    [SwaggerOperation(Summary = "Tạo guide mới")]
    public async Task<IActionResult> Create([FromBody] GuideCreateRequestDto dto)
    {
        var result = await _guideService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ApiResponse<GuideResponseDto>.SuccessResponse(result));
    }

    /// <summary>Cập nhật thông tin guide</summary>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Cập nhật thông tin guide")]
    public async Task<IActionResult> Update(Guid id, [FromBody] GuideUpdateRequestDto dto)
    {
        var result = await _guideService.UpdateAsync(id, dto);
        if (result is null) return NotFound("Guide not found.");
        return Ok(ApiResponse<GuideResponseDto>.SuccessResponse(result));
    }

    /// <summary>Xóa guide</summary>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Xóa guide")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _guideService.DeleteAsync(id);
        if (!ok) return NotFound("Guide not found.");
        return Ok(ApiResponse<object>.SuccessResponse());
    }
}
