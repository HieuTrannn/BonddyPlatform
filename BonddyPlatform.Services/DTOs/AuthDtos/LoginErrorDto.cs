namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class LoginErrorDto
{
    public string Email { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
}

