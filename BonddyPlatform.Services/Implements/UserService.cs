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

    public async Task<UserResponseDto?> GetByIdAsync(Guid id)
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
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Address = dto.Address?.Trim(),
            DateOfBirth = dto.DateOfBirth,
            aboutMe = dto.AboutMe?.Trim(),
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.Users.AddAsync(user);
        await _uow.SaveChangesAsync();

        return MapToDto(user);
    }

    public Task<UserResponseDto> UpdateAsync(Guid id, UserUpdateRequestDto dto)
        => UpdateUserProfileAsync(id, dto, null);

    public async Task<UserResponseDto> UpdateUserProfileAsync(Guid id, UserUpdateRequestDto dto, IFormFile? profilePicture)
    {
        var userExist = await _uow.Users.GetByIdAsync(id);
        if (userExist == null)
        {
            throw new KeyNotFoundException("Không tìm thấy User.");
        }

        bool isUpdated = false;

        if (dto.FullName != userExist.FullName && dto.FullName != null)
        {
            userExist.FullName = dto.FullName.Trim();
            isUpdated = true;
        }

        if (dto.Gender != userExist.Gender && dto.Gender != null)
        {
            userExist.Gender = dto.Gender.Value;
            isUpdated = true;
        }
        if (dto.PhoneNumber != userExist.PhoneNumber && dto.PhoneNumber != null)
        {
            userExist.PhoneNumber = dto.PhoneNumber?.Trim();
            isUpdated = true;
        }
        if (dto.Address != userExist.Address && dto.Address != null)
        {
        userExist.Address = dto.Address?.Trim();
            isUpdated = true;
        }
        if (dto.DateOfBirth != userExist.DateOfBirth && dto.DateOfBirth != null)
        {
            userExist.DateOfBirth = dto.DateOfBirth;
            isUpdated = true;
        }
        if (dto.AboutMe != userExist.aboutMe && dto.AboutMe != null)
        {
            userExist.aboutMe = dto.AboutMe?.Trim();
            isUpdated = true;
        }

        if (profilePicture != null)
        {
            if (profilePicture.Length > 10 * 1024 * 1024) // 10MB
            {
                throw new InvalidOperationException("Kích thước ảnh vượt quá 10MB.");
            }
            var profilePictureUrl = await _firebaseService.UploadAvatarAsync(profilePicture.OpenReadStream(), profilePicture.FileName);
            userExist.ProfilePicture = profilePictureUrl;
            isUpdated = true;
        }

        if (isUpdated)
        {
            userExist.UpdatedAt = DateTime.UtcNow;
            _uow.Users.Update(userExist);
            await _uow.SaveChangesAsync();
            var userUpdateResponse = new UserResponseDto 
            {
                Id = userExist.Id,
                Email = userExist.Email,
                FullName = userExist.FullName,
                Gender = userExist.Gender,
                PhoneNumber = userExist.PhoneNumber,
                Address = userExist.Address,
                DateOfBirth = userExist.DateOfBirth,
                AboutMe = userExist.aboutMe,
                ProfilePicture = userExist.ProfilePicture,
                IsEmailVerified = userExist.IsEmailVerified,
                CreatedAt = userExist.CreatedAt,
                UpdatedAt = userExist.UpdatedAt
            };
            return userUpdateResponse;
        }
        _logger.LogWarning("Không có thay đổi nào được thực hiện.");
        throw new InvalidOperationException("Không có thay đổi nào được thực hiện.");
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
