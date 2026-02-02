namespace BonddyPlatform.Services.Interfaces;

public class FirebaseAuthResult
{
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
}

public interface IFirebaseAuthService
{
    Task<FirebaseAuthResult?> VerifyIdTokenAsync(string idToken);
}
