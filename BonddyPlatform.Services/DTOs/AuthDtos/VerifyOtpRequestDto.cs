using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class VerifyOtpRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string OtpCode { get; set; } = string.Empty;

    /// <summary>Register or ForgotPassword</summary>
    [Required]
    public string Purpose { get; set; } = "Register";
}
