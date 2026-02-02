using BonddyPlatform.Services.DTOs.UserDtos;
using BonddyPlatform.Services.DTOs.Common;

namespace BonddyPlatform.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync();
    Task<PagedResultDto<UserResponseDto>> GetPagedAsync(UserSearchRequestDto request);
    Task<UserResponseDto?> GetByIdAsync(int id);
    Task<UserResponseDto> CreateAsync(UserCreateRequestDto dto);
    Task<UserResponseDto> UpdateAsync(int id, UserUpdateRequestDto dto);
    Task DeleteAsync(int id);
}
