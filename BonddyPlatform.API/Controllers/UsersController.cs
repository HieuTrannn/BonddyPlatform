using BonddyPlatform.API.Common;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.UserDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace BonddyPlatform.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsersController : ApiControllerBase
{
    private readonly IUserService _service;

    public UsersController(IUserService service)
    {
        _service = service;
    }

    [HttpGet]
    [SwaggerOperation(Summary = "Get all users", Description = "Returns a list of all users.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<IEnumerable<UserResponseDto>>))]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Success(result);
    }

    [HttpGet("search")]
    [SwaggerOperation(Summary = "Get users with paging", Description = "Returns users with search, sort, and paging.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<PagedResultDto<UserResponseDto>>))]
    public async Task<IActionResult> GetPaged([FromQuery] UserSearchRequestDto request)
    {
        var result = await _service.GetPagedAsync(request);
        return Success(result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get user by ID", Description = "Returns a single user by ID.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result == null ? NotFound("User not found") : Success(result);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create user", Description = "Creates a new user. Requires unique email.")]
    [SwaggerResponse(201, "User created", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(400, "Bad request (e.g. email already exists)", typeof(ApiResponse))]
    public async Task<IActionResult> Create([FromBody] UserCreateRequestDto dto)
    {
        try
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created, "User created successfully");
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Update user", Description = "Updates an existing user by ID.")]
    [SwaggerResponse(200, "Success", typeof(ApiResponse<UserResponseDto>))]
    [SwaggerResponse(404, "User not found", typeof(ApiResponse))]
    public async Task<IActionResult> Update(Guid id, [FromBody] UserUpdateRequestDto dto)
    {
        var result = await _service.UpdateAsync(id, dto);
        return Success(result, "User updated successfully");
    }

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete user", Description = "Deletes a user by ID.")]
    [SwaggerResponse(204, "No content")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
