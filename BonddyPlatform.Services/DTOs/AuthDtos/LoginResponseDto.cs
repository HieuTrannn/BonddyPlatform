namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int ExpiresInSeconds { get; set; }
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Optional redirect path for the frontend (e.g. from Firebase login request).
    /// </summary>
    public string? RedirectPath { get; set; }
}
