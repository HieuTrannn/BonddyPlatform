using BonddyPlatform.Services.DTOs.UserDtos;
using BonddyPlatform.Services.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace BonddyPlatform.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync();
    Task<PagedResultDto<UserResponseDto>> GetPagedAsync(UserSearchRequestDto request);
    Task<UserResponseDto?> GetByIdAsync(Guid id);
    Task<UserResponseDto> CreateAsync(UserCreateRequestDto dto);
    /// <summary>Admin: bulk tạo nhiều user trực tiếp vào DB, IsEmailVerified = true, không qua flow verify.</summary>
    Task<BulkCreateUserResultDto> AdminCreateUserAsync(List<UserCreateRequestDto> dtos);
    Task<UserResponseDto> UpdateAsync(Guid id, UserUpdateRequestDto dto);
    Task<UserResponseDto> UpdateUserProfileAsync(Guid id, UserUpdateRequestDto dto);
    Task<UserResponseDto?> UploadAvatarAsync(Guid id, IFormFile profilePicture);
    Task DeleteAsync(Guid id);
    /// <summary>Toggle IsActive: Active ↔ Inactive. Returns updated DTO.</summary>
    Task<UserResponseDto?> ToggleActiveAsync(Guid id);
}
