using System.ComponentModel.DataAnnotations;

namespace BonddyPlatform.Services.DTOs.AuthDtos;

public class FirebaseLoginRequestDto
{
    /// <summary>
    /// Firebase ID token from the client (after signInWithPopup/signInWithRedirect, get idToken from user).
    /// </summary>
    [Required(ErrorMessage = "Firebase ID token is required")]
    public string IdToken { get; set; } = string.Empty;

    /// <summary>
    /// Optional redirect path for the frontend after successful login (e.g. "/dashboard", "/profile").
    /// </summary>
    public string? RedirectPath { get; set; }
}
