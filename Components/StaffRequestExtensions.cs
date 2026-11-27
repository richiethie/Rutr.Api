using System.Security.Claims;
using Rutr.Api.Data;

namespace Rutr.Api.Components;

public static class StaffRequestExtensions
{
    public static Guid GetOrganizationId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirst("org")?.Value
                  ?? user.FindFirst("organization_id")?.Value;
        if (Guid.TryParse(raw, out var orgId))
        {
            return orgId;
        }

        // Demo scheme always sets org; never silently fall back to Rutters for JWT sessions.
        if (string.Equals(user.Identity?.AuthenticationType, DemoAuthHandler.SchemeName, StringComparison.Ordinal))
        {
            return SeedData.OrgId;
        }

        throw new InvalidOperationException("Authenticated principal is missing an organization claim.");
    }

    public static Guid? GetStaffUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value
                  ?? user.FindFirst(JwtRegisteredClaimNamesSub)?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }

    public static string GetStaffRole(this ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value
        ?? user.FindFirst("role")?.Value
        ?? "Pm";

    // Avoid hard dependency on System.IdentityModel in this helper for the constant string.
    private const string JwtRegisteredClaimNamesSub = "sub";
}
