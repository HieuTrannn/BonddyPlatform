namespace BonddyPlatform.Services.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(int userId, string email);
    string GenerateRefreshTokenValue();
    int AccessTokenExpirySeconds { get; }
}
