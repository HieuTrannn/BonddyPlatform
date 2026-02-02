using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class ChangePasswordRequestDto
{
    [Required, MinLength(6)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
    
}
