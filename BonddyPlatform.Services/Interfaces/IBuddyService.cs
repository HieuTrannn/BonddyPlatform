using BonddyPlatform.Services.DTOs.BuddyDtos;
using BonddyPlatform.Services.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace BonddyPlatform.Services.Interfaces;

public interface IBuddyService
{
    Task<BuddyResponseDto> RegisterAsync(Guid userId, BuddyRegisterRequestDto dto);
    Task<PagedResultDto<BuddyResponseDto>> GetPagedAsync(BuddySearchRequestDto request);
    Task<BuddyResponseDto?> GetByIdAsync(Guid id);
    Task<BuddyResponseDto?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<BuddyResponseDto>> GetAllActiveAsync();
    Task<IEnumerable<BuddyResponseDto>> GetAllAsync();

    /// <summary>Admin-only: update buddy-specific fields only.</summary>
    Task<BuddyResponseDto?> UpdateAsync(Guid id, BuddyUpdateRequestDto dto);

    /// <summary>Buddy updates their own full profile (User info + Buddy fields) in one atomic call.</summary>
    Task<BuddyResponseDto?> UpdateSelfAsync(Guid userId, BuddySelfUpdateRequestDto dto);

    /// <summary>Buddy uploads or replaces their avatar.</summary>
    Task<BuddyResponseDto?> UploadAvatarAsync(Guid userId, IFormFile avatar);

    Task<bool> DeleteAsync(Guid id);
}
