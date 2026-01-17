using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.ContactDtos;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace BonddyPlatform.Services.Implements
{
    public class ContactService : IContactService
    {
        private readonly IUnitOfWork _uow;

        public ContactService(IUnitOfWork uow)
        {
            _uow = uow;
        }

        public async Task<IEnumerable<ContactResponseDto>> GetAllAsync()
        {
            var contacts = await _uow.Contacts.GetAllAsync();
            return contacts.Select(MapToDto);
        }

        public async Task<ContactResponseDto?> GetByIdAsync(int id)
        {
            var entity = await _uow.Contacts.GetByIdAsync(id);
            return entity == null ? null : MapToDto(entity);
        }

        public async Task<ContactResponseDto> CreateAsync(ContactCreateRequestDto dto)
        {
            // check unique
            if (await _uow.Contacts.GetByGmailAsync(dto.Gmail) != null)
                throw new InvalidOperationException("Gmail đã tồn tại.");

            if (await _uow.Contacts.GetByPhoneAsync(dto.PhoneNumber) != null)
                throw new InvalidOperationException("Số điện thoại đã tồn tại.");

            var entity = new Contact
            {
                Name = dto.Name.Trim(),
                Gmail = dto.Gmail.Trim(),
                PhoneNumber = dto.PhoneNumber.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Contacts.AddAsync(entity);
            await _uow.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task<ContactResponseDto> UpdateAsync(int id, ContactUpdateRequestDto dto)
        {
            var entity = await _uow.Contacts.GetByIdAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy Contact.");

            // unique checks (exclude current)
            var byEmail = await _uow.Contacts.GetByGmailAsync(dto.Gmail);
            if (byEmail != null && byEmail.Id != id)
                throw new InvalidOperationException("Gmail đã tồn tại.");

            var byPhone = await _uow.Contacts.GetByPhoneAsync(dto.PhoneNumber);
            if (byPhone != null && byPhone.Id != id)
                throw new InvalidOperationException("Số điện thoại đã tồn tại.");

            entity.Name = dto.Name.Trim();
            entity.Gmail = dto.Gmail.Trim();
            entity.PhoneNumber = dto.PhoneNumber.Trim();

            _uow.Contacts.Update(entity);
            await _uow.SaveChangesAsync();

            return MapToDto(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _uow.Contacts.GetByIdAsync(id);
            if (entity == null) throw new KeyNotFoundException("Không tìm thấy Contact.");

            _uow.Contacts.Remove(entity);
            await _uow.SaveChangesAsync();
        }

        private static ContactResponseDto MapToDto(Contact x) => new()
        {
            Id = x.Id,
            Name = x.Name,
            Gmail = x.Gmail,
            PhoneNumber = x.PhoneNumber,
            CreatedAt = x.CreatedAt
        };
    }
}
