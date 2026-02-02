using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.UserDtos;
using BonddyPlatform.Services.Helpers;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BonddyPlatform.Services.Implements;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;

    public UserService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
    {
        var users = await _uow.Users.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<PagedResultDto<UserResponseDto>> GetPagedAsync(UserSearchRequestDto request)
    {
        // Get queryable from repository
        var query = _uow.Users.GetQueryable();

        // Apply search across multiple fields
        query = query.ApplySearchByProperties(
            request.Search,
            nameof(User.FullName),
            nameof(User.Email),
            nameof(User.PhoneNumber),
            nameof(User.Address)
        );

        // Apply custom filters
        if (request.Gender.HasValue)
        {
            query = query.Where(u => u.Gender == request.Gender.Value);
        }

        if (request.IsEmailVerified.HasValue)
        {
            query = query.Where(u => u.IsEmailVerified == request.IsEmailVerified.Value);
        }

        if (request.CreatedFrom.HasValue)
        {
            query = query.Where(u => u.CreatedAt >= request.CreatedFrom.Value);
        }

        if (request.CreatedTo.HasValue)
        {
            query = query.Where(u => u.CreatedAt <= request.CreatedTo.Value);
        }

        // Get total count before paging
        var totalCount = await query.CountAsync();

        // Apply sorting (default to CreatedAt desc if not specified)
        query = query.ApplySorting(
            request.SortBy ?? nameof(User.CreatedAt),
            request.SortOrder
        );

        // Apply paging
        var pagedQuery = query.ApplyPaging(request.Page, request.PageSize);
        var items = await pagedQuery.ToListAsync();

        return new PagedResultDto<UserResponseDto>
        {
            Items = items.Select(MapToDto),
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserResponseDto> CreateAsync(UserCreateRequestDto dto)
    {
        // Check unique email
        var existingUser = await _uow.Users.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email đã tồn tại.");

        var user = new User
        {
            Email = dto.Email.Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            Gender = dto.Gender,
            Role = dto.Role,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Address = dto.Address?.Trim(),
            DateOfBirth = dto.DateOfBirth,
            aboutMe = dto.AboutMe?.Trim(),
            ProfilePicture = dto.ProfilePicture?.Trim(),
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserResponseDto> UpdateAsync(int id, UserUpdateRequestDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy User.");

        user.FullName = dto.FullName.Trim();
        user.Gender = dto.Gender;
        user.Role = dto.Role;
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.Address = dto.Address?.Trim();
        user.DateOfBirth = dto.DateOfBirth;
        user.aboutMe = dto.AboutMe?.Trim();
        user.ProfilePicture = dto.ProfilePicture?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy User.");

        _uow.Users.Remove(user);
        await _uow.SaveChangesAsync();
    }

    private static UserResponseDto MapToDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Gender = user.Gender,
        Role = user.Role,
        PhoneNumber = user.PhoneNumber,
        Address = user.Address,
        DateOfBirth = user.DateOfBirth,
        AboutMe = user.aboutMe,
        ProfilePicture = user.ProfilePicture,
        IsEmailVerified = user.IsEmailVerified,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
