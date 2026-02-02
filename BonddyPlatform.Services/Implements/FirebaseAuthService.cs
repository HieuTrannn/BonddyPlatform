using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using FirebaseAdmin;
using Microsoft.Extensions.Logging;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;

namespace BonddyPlatform.Services.Implements;

public class FirebaseAuthService : IFirebaseAuthService
{
    private readonly ILogger<FirebaseAuthService> _logger;
    private readonly FirebaseOptions _firebaseOptions;
    private static bool _appInitialized;
    private static readonly object Lock = new();

    public FirebaseAuthService(ILogger<FirebaseAuthService> logger, IOptions<FirebaseOptions> firebaseOptions)
    {
        _logger = logger;
        _firebaseOptions = firebaseOptions.Value;
    }

    public async Task<FirebaseAuthResult?> VerifyIdTokenAsync(string idToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
            return null;

        EnsureFirebaseAppInitialized();

        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken.Trim());
            var email = decoded.Claims.GetValueOrDefault("email")?.ToString() ?? string.Empty;
            var name = decoded.Claims.GetValueOrDefault("name")?.ToString();

            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Firebase token has no email claim");
                return null;
            }

            return new FirebaseAuthResult
            {
                Uid = decoded.Uid,
                Email = email,
                DisplayName = name
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Firebase ID token verification failed");
            return null;
        }
    }

    private void EnsureFirebaseAppInitialized()
    {
        if (_appInitialized)
            return;

        lock (Lock)
        {
            if (_appInitialized)
                return;

            var path = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(path) });
            }
            else if (!string.IsNullOrWhiteSpace(_firebaseOptions.CredentialsPath) && File.Exists(_firebaseOptions.CredentialsPath))
            {
                FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(_firebaseOptions.CredentialsPath) });
            }
            else if (!string.IsNullOrWhiteSpace(_firebaseOptions.CredentialsJson))
            {
                var json = _firebaseOptions.CredentialsJson.Trim();
                if (!json.StartsWith("{"))
                {
                    try
                    {
                        var decoded = Convert.FromBase64String(json);
                        json = System.Text.Encoding.UTF8.GetString(decoded);
                    }
                    catch
                    {
                        throw new InvalidOperationException("Firebase:CredentialsJson must be valid JSON or base64-encoded JSON.");
                    }
                }
                FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromJson(json) });
            }
            else
            {
                throw new InvalidOperationException(
                    "Firebase not configured. Set GOOGLE_APPLICATION_CREDENTIALS or Firebase:CredentialsPath or Firebase:CredentialsJson in config.");
            }

            _appInitialized = true;
        }
    }
}
