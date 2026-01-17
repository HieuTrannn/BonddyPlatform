using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonddyPlatform.Services.DTOs.ContactDtos;

namespace BonddyPlatform.Services.Interfaces
{
    public interface IContactService
    {
        Task<IEnumerable<ContactResponseDto>> GetAllAsync();
        Task<ContactResponseDto?> GetByIdAsync(int id);
        Task<ContactResponseDto> CreateAsync(ContactCreateRequestDto dto);
        Task<ContactResponseDto> UpdateAsync(int id, ContactUpdateRequestDto dto);
        Task DeleteAsync(int id);
    }
}
