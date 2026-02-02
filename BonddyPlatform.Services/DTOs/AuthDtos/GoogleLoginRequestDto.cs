using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class GoogleLoginRequestDto
{
    /// <summary>
    /// Google ID token from the client (after Google sign-in, e.g. credential.idToken or user.getIdToken()).
    /// </summary>
    [Required(ErrorMessage = "Google ID token is required")]
    public string IdToken { get; set; } = string.Empty;
}
