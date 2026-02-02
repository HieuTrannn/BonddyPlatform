using System.ComponentModel.DataAnnotations;
using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.UserDtos;

public class UserUpdateRequestDto
{
    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    public Gender Gender { get; set; }

    public Role Role { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(1000)]
    public string? AboutMe { get; set; }

    [MaxLength(500)]
    public string? ProfilePicture { get; set; }
}
