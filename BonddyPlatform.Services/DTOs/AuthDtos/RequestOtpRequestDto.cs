using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class RequestOtpRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>Register or ForgotPassword</summary>
    [Required]
    public string Purpose { get; set; } = "Register"; // "Register" | "ForgotPassword"
}
