namespace BonddyPlatform.Services.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email, string role, string fullName);
    string GenerateRefreshTokenValue();
    int AccessTokenExpirySeconds { get; }
}
