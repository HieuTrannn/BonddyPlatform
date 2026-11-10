using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using FirebaseAdmin;
using Microsoft.Extensions.Logging;
using FirebaseAdmin.Auth;
using BonddyPlatform.Services.Helpers;
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

            FirebaseApp.Create(new AppOptions { Credential = FirebaseCredentialLoader.Load(_firebaseOptions) });

            _appInitialized = true;
        }
    }
}
