using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.ContactDtos;

namespace BonddyPlatform.Services.Interfaces;

public interface IContactService
{
    Task<IEnumerable<ContactResponseDto>> GetAllAsync();
    Task<PagedResultDto<ContactResponseDto>> GetPagedAsync(ContactSearchRequestDto request);
    Task<ContactResponseDto?> GetByIdAsync(Guid id);
    Task<ContactResponseDto> CreateAsync(ContactCreateRequestDto dto);
    Task<ContactResponseDto> UpdateAsync(Guid id, ContactUpdateRequestDto dto);
    Task DeleteAsync(Guid id);
}
