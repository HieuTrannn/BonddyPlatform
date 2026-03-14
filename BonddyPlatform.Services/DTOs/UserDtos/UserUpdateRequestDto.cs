using System.ComponentModel.DataAnnotations;
using BonddyPlatform.Repositories.Models;

namespace BonddyPlatform.Services.DTOs.UserDtos;

public class UserUpdateRequestDto
{
    public string? FullName { get; set; }

    public Gender? Gender { get; set; }

    [MaxLength(20)]
    public string? PhoneNumber { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(1000)]
    public string? AboutMe { get; set; }
}
