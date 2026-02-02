namespace BonddyPlatform.Services.Options;

public class FirebaseOptions
{
    public const string SectionName = "Firebase";

    /// <summary>
    /// Path to Firebase service account JSON file (e.g. firebase-adminsdk.json).
    /// Alternatively set GOOGLE_APPLICATION_CREDENTIALS env var.
    /// </summary>
    public string? CredentialsPath { get; set; }

    /// <summary>
    /// Firebase service account JSON content (base64 or raw JSON string).
    /// Use when you cannot use file path (e.g. in cloud).
    /// </summary>
    public string? CredentialsJson { get; set; }
}
