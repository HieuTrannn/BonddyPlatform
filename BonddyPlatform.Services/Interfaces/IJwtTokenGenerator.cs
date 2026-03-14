namespace BonddyPlatform.Services.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, string fullName);
    string GenerateRefreshTokenValue();
    int AccessTokenExpirySeconds { get; }
}
