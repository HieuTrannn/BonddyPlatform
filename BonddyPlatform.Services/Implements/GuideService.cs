using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.GuideDtos;
using BonddyPlatform.Services.Interfaces;

namespace BonddyPlatform.Services.Implements;

public class GuideService : IGuideService
{
    private readonly IUnitOfWork _uow;

    public GuideService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<GuideResponseDto> CreateAsync(GuideCreateRequestDto dto)
    {
        var guide = new Guide
        {
            FullName = dto.FullName,
            Phone = dto.Phone,
            Email = dto.Email,
            IsInternal = dto.IsInternal,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };
        
        await _uow.Guides.AddAsync(guide);
        await _uow.SaveChangesAsync();
        return ToDto(guide);
    }

    public async Task<GuideResponseDto?> GetByIdAsync(Guid id)
    {
        var guide = await _uow.Guides.GetByIdAsync(id);
        return guide is null ? null : ToDto(guide);
    }

    public async Task<IEnumerable<GuideResponseDto>> GetAllAsync()
    {
        var guides = await _uow.Guides.GetAllAsync();
        return guides.Select(ToDto);
    }

    public async Task<GuideResponseDto?> UpdateAsync(Guid id, GuideUpdateRequestDto dto)
    {
        var guide = await _uow.Guides.GetByIdAsync(id);
        if (guide is null) return null;

        if (dto.FullName is not null) guide.FullName = dto.FullName;
        if (dto.Phone is not null) guide.Phone = dto.Phone;
        if (dto.Email is not null) guide.Email = dto.Email;
        if (dto.IsInternal.HasValue) guide.IsInternal = dto.IsInternal.Value;
        if (dto.Notes is not null) guide.Notes = dto.Notes;

        _uow.Guides.Update(guide);
        await _uow.SaveChangesAsync();
        return ToDto(guide);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var guide = await _uow.Guides.GetByIdAsync(id);
        if (guide is null) return false;

        _uow.Guides.Remove(guide);
        await _uow.SaveChangesAsync();
        return true;
    }

    private static GuideResponseDto ToDto(Guide g) => new()
    {
        Id = g.Id,
        FullName = g.FullName,
        Phone = g.Phone,
        Email = g.Email,
        IsInternal = g.IsInternal,
        Notes = g.Notes,
        CreatedAt = g.CreatedAt
    };
}
