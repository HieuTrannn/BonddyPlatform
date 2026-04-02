using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.BuddyDtos;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.Helpers;
using BonddyPlatform.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class BuddyService : IBuddyService
{
    private readonly IUnitOfWork _uow;
    private readonly FirebaseService _firebaseService;

    public BuddyService(IUnitOfWork uow, FirebaseService firebaseService)
    {
        _uow = uow;
        _firebaseService = firebaseService;
    }

    public async Task<BuddyResponseDto> RegisterAsync(Guid userId, BuddyRegisterRequestDto dto)
    {
        var existing = await _uow.Buddies.GetByUserIdAsync(userId);
        if (existing is not null)
            throw new InvalidOperationException("This user already has a Buddy profile.");

        var user = await _uow.Users.GetByIdAsync(userId);
        if (user is null)
            throw new KeyNotFoundException("User not found for creating Buddy profile.");

        var buddy = new Buddy
        {
            UserId = userId,
            Activities = dto.Activities,
            CostPerHour = dto.CostPerHour,
            Languages = dto.Languages,
            Bio = dto.Bio,
            Rate = 0m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Buddies.AddAsync(buddy);

        // Add Buddy role if user doesn't already have it (user keeps existing roles too)
        if (!await _uow.UserRoles.HasRoleAsync(userId, Role.Buddy))
            await _uow.UserRoles.AddAsync(new UserRole { UserId = userId, Role = Role.Buddy });

        await _uow.SaveChangesAsync();

        var full = await _uow.Buddies.GetByIdWithUserAsync(buddy.Id);
        return ToDto(full!);
    }

    public async Task<PagedResultDto<BuddyResponseDto>> GetPagedAsync(BuddySearchRequestDto request)
    {
        var query = _uow.Buddies.GetQueryable().Include(b => b.User).AsQueryable();

        // Search across User.FullName, User.Email, Bio
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(b =>
                (b.User != null && (
                    b.User.FullName.ToLower().Contains(search) ||
                    b.User.Email.ToLower().Contains(search)
                )) ||
                (b.Bio != null && b.Bio.ToLower().Contains(search))
            );
        }

        // Filter
        if (request.IsActive.HasValue)
            query = query.Where(b => b.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync();

        // Sort
        query = (request.SortBy?.ToLower()) switch
        {
            "fullname" => request.SortOrder == "asc"
                ? query.OrderBy(b => b.User != null ? b.User.FullName : "")
                : query.OrderByDescending(b => b.User != null ? b.User.FullName : ""),
            "costperhour" => request.SortOrder == "asc"
                ? query.OrderBy(b => b.CostPerHour)
                : query.OrderByDescending(b => b.CostPerHour),
            "rate" => request.SortOrder == "asc"
                ? query.OrderBy(b => b.Rate)
                : query.OrderByDescending(b => b.Rate),
            _ => request.SortOrder == "asc"
                ? query.OrderBy(b => b.CreatedAt)
                : query.OrderByDescending(b => b.CreatedAt)
        };

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResultDto<BuddyResponseDto>
        {
            Items = items.Select(ToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<BuddyResponseDto?> GetByIdAsync(Guid id)
    {
        var buddy = await _uow.Buddies.GetByIdWithUserAsync(id);
        return buddy is null ? null : ToDto(buddy);
    }

    public async Task<BuddyResponseDto?> GetByUserIdAsync(Guid userId)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(userId);
        return buddy is null ? null : ToDto(buddy);
    }

    public async Task<IEnumerable<BuddyResponseDto>> GetAllActiveAsync()
    {
        var buddies = await _uow.Buddies.GetAllActiveAsync();
        return buddies.Select(ToDto);
    }

    public async Task<IEnumerable<BuddyResponseDto>> GetAllAsync()
    {
        var buddies = await _uow.Buddies.GetAllWithUserAsync();
        return buddies.Select(ToDto);
    }

    public async Task<BuddyResponseDto?> UpdateAsync(Guid id, BuddyUpdateRequestDto dto)
    {
        var buddy = await _uow.Buddies.GetByIdWithUserAsync(id);
        if (buddy is null) return null;

        if (dto.Activities is not null) buddy.Activities = dto.Activities;
        if (dto.CostPerHour.HasValue) buddy.CostPerHour = dto.CostPerHour.Value;
        if (dto.Languages is not null) buddy.Languages = dto.Languages;
        if (dto.Bio is not null) buddy.Bio = dto.Bio;
        if (dto.IsActive.HasValue) buddy.IsActive = dto.IsActive.Value;
        buddy.UpdatedAt = DateTime.UtcNow;

        _uow.Buddies.Update(buddy);
        await _uow.SaveChangesAsync();
        return ToDto(buddy);
    }

    public async Task<BuddyResponseDto?> UpdateSelfAsync(Guid userId, BuddySelfUpdateRequestDto dto)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(userId);
        if (buddy is null) return null;

        var user = buddy.User ?? await _uow.Users.GetByIdAsync(userId);
        if (user is null) return null;

        // ── Update User fields ────────────────────────────────────────────────
        if (dto.FullName != null) user.FullName = dto.FullName.Trim();
        if (dto.Gender.HasValue) user.Gender = dto.Gender.Value;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber.Trim();
        if (dto.Address != null) user.Address = dto.Address.Trim();
        if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth;
        if (dto.AboutMe != null) user.aboutMe = dto.AboutMe.Trim();
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);

        // ── Update Buddy fields ───────────────────────────────────────────────
        if (dto.Activities is not null) buddy.Activities = dto.Activities;
        if (dto.CostPerHour.HasValue) buddy.CostPerHour = dto.CostPerHour.Value;
        if (dto.Languages is not null) buddy.Languages = dto.Languages;
        if (dto.Bio is not null) buddy.Bio = dto.Bio;
        buddy.UpdatedAt = DateTime.UtcNow;
        _uow.Buddies.Update(buddy);

        await _uow.SaveChangesAsync();

        var refreshed = await _uow.Buddies.GetByIdWithUserAsync(buddy.Id);
        return ToDto(refreshed!);
    }

    public async Task<BuddyResponseDto?> UploadAvatarAsync(Guid userId, IFormFile avatar)
    {
        var buddy = await _uow.Buddies.GetByUserIdAsync(userId);
        if (buddy is null) return null;

        var user = buddy.User ?? await _uow.Users.GetByIdAsync(userId);
        if (user is null) return null;

        if (avatar.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("Kích thước ảnh vượt quá 10MB.");

        user.ProfilePicture = await _firebaseService.UploadAvatarAsync(avatar.OpenReadStream(), avatar.FileName);
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        var refreshed = await _uow.Buddies.GetByIdWithUserAsync(buddy.Id);
        return ToDto(refreshed!);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var buddy = await _uow.Buddies.GetByIdAsync(id);
        if (buddy is null) return false;

        _uow.Buddies.Remove(buddy);
        await _uow.SaveChangesAsync();
        return true;
    }

    private static BuddyResponseDto ToDto(Buddy b) => new()
    {
        Id = b.Id,
        UserId = b.UserId,
        FullName = b.User?.FullName ?? string.Empty,
        Email = b.User?.Email ?? string.Empty,
        PhoneNumber = b.User?.PhoneNumber,
        Gender = b.User?.Gender ?? Gender.Other,
        Address = b.User?.Address,
        DateOfBirth = b.User?.DateOfBirth ?? DateTime.MinValue,
        AboutMe = b.User?.aboutMe,
        ProfilePicture = b.User?.ProfilePicture,
        Activities = b.Activities,
        CostPerHour = b.CostPerHour,
        Rate = b.Rate,
        Languages = b.Languages,
        Bio = b.Bio,
        IsActive = b.IsActive,
        CreatedAt = b.CreatedAt,
        UpdatedAt = b.UpdatedAt
    };
}
