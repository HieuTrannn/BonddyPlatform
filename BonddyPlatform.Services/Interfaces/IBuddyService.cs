using BonddyPlatform.Services.DTOs.BuddyDtos;
using BonddyPlatform.Services.DTOs.Common;
using Microsoft.AspNetCore.Http;

namespace BonddyPlatform.Services.Interfaces;

public interface IBuddyService
{
    Task<string> RegisterAsync(Guid userId, BuddyRegisterRequestDto dto);

    /// <summary>Admin: lấy danh sách user đang chờ duyệt làm buddy (IsActive = false).</summary>
    Task<IEnumerable<BuddyResponseDto>> GetPendingApplicantsAsync();
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
    /// <summary>Toggle IsActive: Active ↔ Inactive. Returns updated DTO.</summary>
    Task<BuddyResponseDto?> ToggleActiveAsync(Guid id);

    /// <summary>Admin duyệt yêu cầu làm buddy: set IsActive = true và cấp role Buddy.</summary>
    Task<BuddyResponseDto?> ApproveBuddyAsync(Guid buddyId);

    /// <summary>
    /// Lấy tất cả buddy active, kèm thông tin gói đang dùng (nếu có).
    /// </summary>
    Task<IEnumerable<BuddyResponseDto>> GetAllActiveWithSubscriptionAsync();

    /// <summary>
    /// Lấy tất cả buddy đã có gói dịch vụ active (đã mua gói) (paged).
    /// </summary>
    Task<PagedResultDto<BuddyResponseDto>> GetBuddiesWithActiveSubscriptionAsync(BuddySearchRequestDto request);
}
