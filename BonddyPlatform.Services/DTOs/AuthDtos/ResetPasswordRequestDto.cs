using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class ResetPasswordRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string OtpCode { get; set; } = string.Empty;
}
