using System.Security.Claims;
using Rutr.Api.Data;

namespace Rutr.Api.Components;

public static class StaffRequestExtensions
{
    public static Guid GetOrganizationId(this ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirst("org")?.Value, out var orgId) ? orgId : SeedData.OrgId;

    public static Guid? GetStaffUserId(this ClaimsPrincipal user)
    {
        var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(sub, out var id) ? id : null;
    }
}
