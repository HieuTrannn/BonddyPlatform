using BonddyPlatform.Repositories.Models;
namespace BonddyPlatform.Services.DTOs.UserDtos;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public Role Role { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? AboutMe { get; set; }
    public string? ProfilePicture { get; set; }
    public bool IsEmailVerified { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
