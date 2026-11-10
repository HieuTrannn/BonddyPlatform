namespace BonddyPlatform.Services.Options;

public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>
    /// Path to a Firebase service account JSON file (local development only, never commit it).
    /// Alternatively set the GOOGLE_APPLICATION_CREDENTIALS env var.
    /// </summary>
    public string? CredentialsPath { get; set; }

    /// <summary>
    /// Firebase service account JSON content (raw JSON or base64). Set via FIREBASE_CREDENTIALS_JSON.
    /// </summary>
    public string? CredentialsJson { get; set; }

    /// <summary>Firebase Storage bucket used for avatar uploads.</summary>
    public string BucketName { get; set; } = "bonddyplatform.firebasestorage.app";
}
