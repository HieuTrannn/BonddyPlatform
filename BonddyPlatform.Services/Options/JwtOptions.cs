namespace BonddyPlatform.Services.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "BonddyPlatform";
    public string Audience { get; set; } = "BonddyPlatform";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
}
