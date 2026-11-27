namespace Rutr.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "rutr-api";
    public string Audience { get; set; } = "rutr-web";
    public string SigningKey { get; set; } = "local-dev-only-signing-key-change-me-before-prod!!";
    public int ExpiryMinutes { get; set; } = 480;
}
