using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BonddyPlatform.Services.Interfaces;
using BonddyPlatform.Services.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;

namespace BonddyPlatform.Services.Implements;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly ILogger<JwtTokenGenerator> _logger;

    public JwtTokenGenerator(IOptions<JwtOptions> options, ILogger<JwtTokenGenerator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public int AccessTokenExpirySeconds => _options.AccessTokenExpiryMinutes * 60;

    public string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles, string fullName)
    {
        try
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),
            };

            // Add one ClaimTypes.Role per role so [Authorize(Roles="Buddy")] works natively
            foreach (var role in roles)
                claims.Add(new Claim(ClaimTypes.Role, role));

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_options.AccessTokenExpiryMinutes),
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                SigningCredentials = creds
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating access token");
            throw new InvalidOperationException("Error generating access token", ex);
        }
    }

    public string GenerateRefreshTokenValue()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}
