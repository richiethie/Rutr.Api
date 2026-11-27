using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Rutr.Api.Data;

namespace Rutr.Api.Components;

/// <summary>
/// Accepts X-Demo-Auth: pm for local staff UI when JWT is not yet stored.
/// </summary>
public class DemoAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Demo";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Demo-Auth", out var value) || value != "pm")
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, SeedData.PmUserId.ToString()),
            new Claim("sub", SeedData.PmUserId.ToString()),
            new Claim(ClaimTypes.Email, "pm@rutters.demo"),
            new Claim("name", "Alex Rivera"),
            new Claim("org", SeedData.OrgId.ToString()),
            new Claim(ClaimTypes.Role, "Pm")
        };
        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
