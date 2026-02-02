using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Repositories.Models;

public enum OtpPurpose
{
    Register = 0,
    ForgotPassword = 1
}

public class OtpVerification
{
    public int Id { get; set; }

    [Required, MaxLength(255), EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string OtpCode { get; set; } = string.Empty;

    public OtpPurpose Purpose { get; set; }

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
