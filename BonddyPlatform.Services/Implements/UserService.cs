using BonddyPlatform.Repositories.Interfaces;
using BonddyPlatform.Repositories.Models;
using BonddyPlatform.Services.DTOs.Common;
using BonddyPlatform.Services.DTOs.UserDtos;
using BonddyPlatform.Services.Helpers;
using BonddyPlatform.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BonddyPlatform.Services.Implements;

public class UserService : IUserService
{
    private readonly IUnitOfWork _uow;
    private readonly FirebaseService _firebaseService;
    private readonly ILogger<UserService> _logger;

    public UserService(IUnitOfWork uow, FirebaseService firebaseService, ILogger<UserService> logger)
    {
        _uow = uow;
        _firebaseService = firebaseService;
        _logger = logger;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
    {
        var users = await _uow.Users.GetAllAsync();
        return users.Select(MapToDto);
    }

    public async Task<PagedResultDto<UserResponseDto>> GetPagedAsync(UserSearchRequestDto request)
    {
        var query = _uow.Users.GetQueryable()
            .Where(u => u.Roles.All(r => r.Role != Role.Admin)); // Loại bỏ tài khoản Admin

        query = query.ApplySearchByProperties(
            request.Search,
            nameof(User.FullName),
            nameof(User.Email),
            nameof(User.PhoneNumber),
            nameof(User.Address)
        );

        if (request.Gender.HasValue)
            query = query.Where(u => u.Gender == request.Gender.Value);

        if (request.IsEmailVerified.HasValue)
            query = query.Where(u => u.IsEmailVerified == request.IsEmailVerified.Value);

        if (request.CreatedFrom.HasValue)
            query = query.Where(u => u.CreatedAt >= request.CreatedFrom.Value);

        if (request.CreatedTo.HasValue)
            query = query.Where(u => u.CreatedAt <= request.CreatedTo.Value);

        if (request.Role.HasValue)
            query = query.Where(u => u.Roles.Any(r => r.Role == request.Role.Value));

        if (request.IsActive.HasValue)
            query = query.Where(u => u.IsActive == request.IsActive.Value);

        var totalCount = await query.CountAsync();

        query = query.ApplySorting(
            request.SortBy ?? nameof(User.CreatedAt),
            request.SortOrder
        );

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

    public async Task<UserResponseDto?> GetByIdAsync(Guid id)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        return user == null ? null : MapToDto(user);
    }

    public async Task<UserResponseDto> CreateAsync(UserCreateRequestDto dto)
    {
        var existingUser = await _uow.Users.GetByEmailAsync(dto.Email);
        if (existingUser != null)
            throw new InvalidOperationException("Email đã tồn tại.");

        var user = new User
        {
            Email = dto.Email.Trim(),
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            Gender = dto.Gender,
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Address = dto.Address?.Trim(),
            DateOfBirth = dto.DateOfBirth,
            aboutMe = dto.AboutMe?.Trim(),
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        // Assign the admin-specified role (default to User if not specified)
        await _uow.UserRoles.AddAsync(new UserRole { UserId = user.Id, Role = dto.Role });
        await _uow.SaveChangesAsync();

        // Reload so Roles are populated
        user = (await _uow.Users.GetByIdAsync(user.Id))!;
        return MapToDto(user);
    }

    public async Task<BulkCreateUserResultDto> AdminCreateUserAsync(List<UserCreateRequestDto> dtos)
    {
        var result = new BulkCreateUserResultDto { TotalRequested = dtos.Count };

        foreach (var dto in dtos)
        {
            try
            {
                var existing = await _uow.Users.GetByEmailAsync(dto.Email);
                if (existing != null)
                {
                    result.Failed.Add(new BulkCreateUserFailedItemDto
                    {
                        Email = dto.Email,
                        Reason = "Email already exists."
                    });
                    continue;
                }

                var user = new User
                {
                    Email = dto.Email.Trim(),
                    Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    FullName = dto.FullName.Trim(),
                    Gender = dto.Gender,
                    PhoneNumber = dto.PhoneNumber?.Trim(),
                    Address = dto.Address?.Trim(),
                    DateOfBirth = dto.DateOfBirth,
                    aboutMe = dto.AboutMe?.Trim(),
                    IsEmailVerified = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                await _uow.Users.AddAsync(user);
                await _uow.SaveChangesAsync();

                await _uow.UserRoles.AddAsync(new UserRole { UserId = user.Id, Role = Role.User });
                await _uow.SaveChangesAsync();

                user = (await _uow.Users.GetByIdAsync(user.Id))!;
                result.Succeeded.Add(MapToDto(user));
            }
            catch (Exception ex)
            {
                result.Failed.Add(new BulkCreateUserFailedItemDto
                {
                    Email = dto.Email,
                    Reason = ex.Message
                });
            }
        }

        result.TotalSucceeded = result.Succeeded.Count;
        result.TotalFailed = result.Failed.Count;
        return result;
    }

    public async Task<UserResponseDto> UpdateAsync(Guid id, UserUpdateRequestDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy User.");

        if (dto.FullName != null) user.FullName = dto.FullName.Trim();
        if (dto.Gender.HasValue) user.Gender = dto.Gender.Value;
        if (dto.PhoneNumber != null) user.PhoneNumber = dto.PhoneNumber.Trim();
        if (dto.Address != null) user.Address = dto.Address.Trim();
        if (dto.DateOfBirth.HasValue) user.DateOfBirth = dto.DateOfBirth;
        if (dto.AboutMe != null) user.aboutMe = dto.AboutMe.Trim();

        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserResponseDto> UpdateUserProfileAsync(Guid id, UserUpdateRequestDto dto)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null)
            throw new KeyNotFoundException("Không tìm thấy User.");

        bool isUpdated = false;

        if (dto.FullName != null && dto.FullName != user.FullName) { user.FullName = dto.FullName.Trim(); isUpdated = true; }
        if (dto.Gender.HasValue && dto.Gender.Value != user.Gender) { user.Gender = dto.Gender.Value; isUpdated = true; }
        if (dto.PhoneNumber != null && dto.PhoneNumber != user.PhoneNumber) { user.PhoneNumber = dto.PhoneNumber.Trim(); isUpdated = true; }
        if (dto.Address != null && dto.Address != user.Address) { user.Address = dto.Address.Trim(); isUpdated = true; }
        if (dto.DateOfBirth.HasValue && dto.DateOfBirth != user.DateOfBirth) { user.DateOfBirth = dto.DateOfBirth; isUpdated = true; }
        if (dto.AboutMe != null && dto.AboutMe != user.aboutMe) { user.aboutMe = dto.AboutMe.Trim(); isUpdated = true; }

        if (!isUpdated)
        {
            _logger.LogWarning("Không có thay đổi nào được thực hiện.");
            throw new InvalidOperationException("Không có thay đổi nào được thực hiện.");
        }

        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserResponseDto?> UploadAvatarAsync(Guid id, IFormFile profilePicture)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user == null) return null;

        if (profilePicture.Length > 10 * 1024 * 1024)
            throw new InvalidOperationException("Kích thước ảnh vượt quá 10MB.");
            
        user.ProfilePicture = await _firebaseService.UploadAvatarAsync(profilePicture.OpenReadStream(), profilePicture.FileName);
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();
        return MapToDto(user);
    }

    public async Task DeleteAsync(Guid id)
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
        Roles = user.Roles.Select(r => r.Role).ToList(),
        PhoneNumber = user.PhoneNumber,
        Address = user.Address,
        DateOfBirth = user.DateOfBirth,
        AboutMe = user.aboutMe,
        ProfilePicture = user.ProfilePicture,
        IsEmailVerified = user.IsEmailVerified,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };

    public async Task<UserResponseDto?> ToggleActiveAsync(Guid id)
    {
        var user = await _uow.Users.GetByIdAsync(id);
        if (user is null) return null;

        user.IsActive = !user.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        _uow.Users.Update(user);
        await _uow.SaveChangesAsync();

        _logger.LogInformation(
            "User {UserId} status toggled to {Status}",
            user.Id, user.IsActive ? "Active" : "Inactive");

        return MapToDto(user);
    }
}
