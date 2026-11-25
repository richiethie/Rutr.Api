namespace Rutr.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "rutr-api";
    public string Audience { get; set; } = "rutr-web";
    public string SigningKey { get; set; } = "local-dev-signing-key-change-me-32chars!!";
    public int ExpiryMinutes { get; set; } = 480;
}
