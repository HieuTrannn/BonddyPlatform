using BonddyPlatform.Services.DTOs.GuideDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IGuideService
{
    Task<GuideResponseDto> CreateAsync(GuideCreateRequestDto dto);
    Task<GuideResponseDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<GuideResponseDto>> GetAllAsync();
    Task<GuideResponseDto?> UpdateAsync(Guid id, GuideUpdateRequestDto dto);
    Task<bool> DeleteAsync(Guid id);
}
