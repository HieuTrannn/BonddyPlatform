namespace BonddyPlatform.Services.DTOs.AuthDtos;
using BonddyPlatform.Repositories.Models;

public class LoginResponseDto
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<Role> Roles { get; set; } = new();
    public bool IsEmailVerified { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}
