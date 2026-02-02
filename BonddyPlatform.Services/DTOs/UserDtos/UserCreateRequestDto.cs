using System.ComponentModel.DataAnnotations;
using BonddyPlatform.Repositories.Models;
namespace BonddyPlatform.Services.DTOs.UserDtos;

public class UserCreateRequestDto
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6), MaxLength(500)]
    public string Password { get; set; } = string.Empty;

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
