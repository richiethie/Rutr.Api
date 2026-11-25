using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Services;

public class OrganizationService(RutrDbContext db)
{
    public async Task<OrganizationDto?> GetAsync(Guid orgId, CancellationToken ct = default)
    {
        var o = await db.Organizations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orgId, ct);
        return o is null
            ? null
            : new OrganizationDto(o.Id, o.Name, o.Slug, o.BrandPrimaryColor, o.QuietHoursStartLocal, o.QuietHoursEndLocal, o.TimeZoneId);
    }

    public async Task<OrganizationDto?> UpdateAsync(Guid orgId, OrganizationUpdateRequest req, CancellationToken ct = default)
    {
        var o = await db.Organizations.FirstOrDefaultAsync(x => x.Id == orgId, ct);
        if (o is null) return null;
        o.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.BrandPrimaryColor)) o.BrandPrimaryColor = req.BrandPrimaryColor.Trim();
        if (req.QuietHoursStartLocal.HasValue) o.QuietHoursStartLocal = req.QuietHoursStartLocal.Value;
        if (req.QuietHoursEndLocal.HasValue) o.QuietHoursEndLocal = req.QuietHoursEndLocal.Value;
        if (!string.IsNullOrWhiteSpace(req.TimeZoneId)) o.TimeZoneId = req.TimeZoneId.Trim();
        await db.SaveChangesAsync(ct);
        return new OrganizationDto(o.Id, o.Name, o.Slug, o.BrandPrimaryColor, o.QuietHoursStartLocal, o.QuietHoursEndLocal, o.TimeZoneId);
    }

    public async Task<IReadOnlyList<StaffMemberDto>> ListStaffAsync(Guid orgId, CancellationToken ct = default)
    {
        return await db.StaffUsers.AsNoTracking()
            .Where(u => u.OrganizationId == orgId)
            .OrderBy(u => u.DisplayName)
            .Select(u => new StaffMemberDto(u.Id, u.Email, u.DisplayName, u.Role, u.OrganizationId))
            .ToListAsync(ct);
    }

    public async Task<StaffMemberDto?> UpdateStaffRoleAsync(Guid orgId, Guid staffId, StaffRoleUpdateRequest req, CancellationToken ct = default)
    {
        var role = req.Role.Trim();
        if (role is not ("Admin" or "Pm" or "Sales"))
        {
            throw new InvalidOperationException("Role must be Admin, Pm, or Sales.");
        }

        var u = await db.StaffUsers.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == staffId, ct);
        if (u is null) return null;
        u.Role = role;
        await db.SaveChangesAsync(ct);
        return new StaffMemberDto(u.Id, u.Email, u.DisplayName, u.Role, u.OrganizationId);
    }
}
