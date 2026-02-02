namespace BonddyPlatform.Services.DTOs.AuthDtos;
using BonddyPlatform.Repositories.Models;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public Role Role { get; set; }
    public int ExpiresInSeconds { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
