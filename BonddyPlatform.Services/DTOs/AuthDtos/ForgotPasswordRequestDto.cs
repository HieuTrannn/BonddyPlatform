using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class ForgotPasswordRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;
}
