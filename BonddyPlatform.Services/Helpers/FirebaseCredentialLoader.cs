using System.Text;
using BonddyPlatform.Services.Options;
using Google.Apis.Auth.OAuth2;

namespace BonddyPlatform.Services.Helpers;

/// <summary>
/// Resolves the Firebase service-account credential in priority order:
/// GOOGLE_APPLICATION_CREDENTIALS file → Firebase:CredentialsPath file → Firebase:CredentialsJson (raw or base64).
/// </summary>
public static class FirebaseCredentialLoader
{
    public static GoogleCredential Load(FirebaseOptions options)
    {
        var path = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
            return CredentialFactory.FromFile<ServiceAccountCredential>(path).ToGoogleCredential();

        if (!string.IsNullOrWhiteSpace(options.CredentialsPath) && File.Exists(options.CredentialsPath))
            return CredentialFactory.FromFile<ServiceAccountCredential>(options.CredentialsPath).ToGoogleCredential();

        if (!string.IsNullOrWhiteSpace(options.CredentialsJson))
            return CredentialFactory.FromJson<ServiceAccountCredential>(DecodeJson(options.CredentialsJson)).ToGoogleCredential();

        throw new InvalidOperationException(
            "Firebase is not configured. Set FIREBASE_CREDENTIALS_JSON, GOOGLE_APPLICATION_CREDENTIALS or Firebase:CredentialsPath.");
    }

    internal static string DecodeJson(string value)
    {
        var json = value.Trim();
        if (json.StartsWith('{'))
            return json;

        try
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(json));
        }
        catch (FormatException)
        {
            throw new InvalidOperationException("Firebase credentials must be valid JSON or base64-encoded JSON.");
        }
    }
}
