using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class PropertyService(RutrDbContext db)
{
    public async Task<IReadOnlyList<PropertyDto>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        var rows = await db.Properties.AsNoTracking()
            .Where(p => p.OrganizationId == orgId)
            .Select(p => new { p, JobCount = p.Jobs.Count })
            .ToListAsync(ct);
        return rows.OrderBy(r => r.p.AddressLine1)
            .Select(r => Map(r.p, r.JobCount))
            .ToList();
    }

    public async Task<PropertyDto?> GetAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var row = await db.Properties.AsNoTracking()
            .Where(p => p.OrganizationId == orgId && p.Id == id)
            .Select(p => new { p, JobCount = p.Jobs.Count })
            .FirstOrDefaultAsync(ct);
        return row is null ? null : Map(row.p, row.JobCount);
    }

    public async Task<PropertyDto> CreateAsync(Guid orgId, PropertyWriteRequest req, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new Property
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AddressLine1 = req.AddressLine1.Trim(),
            City = req.City.Trim(),
            State = req.State.Trim(),
            PostalCode = req.PostalCode.Trim(),
            PropertyType = string.IsNullOrWhiteSpace(req.PropertyType) ? "Residential" : req.PropertyType.Trim(),
            YearBuilt = req.YearBuilt,
            Notes = req.Notes,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Properties.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity, 0);
    }

    public async Task<PropertyDto?> UpdateAsync(Guid orgId, Guid id, PropertyWriteRequest req, CancellationToken ct = default)
    {
        var entity = await db.Properties.Include(p => p.Jobs)
            .FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.Id == id, ct);
        if (entity is null) return null;
        entity.AddressLine1 = req.AddressLine1.Trim();
        entity.City = req.City.Trim();
        entity.State = req.State.Trim();
        entity.PostalCode = req.PostalCode.Trim();
        entity.PropertyType = string.IsNullOrWhiteSpace(req.PropertyType) ? entity.PropertyType : req.PropertyType.Trim();
        entity.YearBuilt = req.YearBuilt;
        entity.Notes = req.Notes;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity, entity.Jobs.Count);
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var entity = await db.Properties.FirstOrDefaultAsync(p => p.OrganizationId == orgId && p.Id == id, ct);
        if (entity is null) return false;
        db.Properties.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static PropertyDto Map(Property p, int jobCount) =>
        new(p.Id, p.AddressLine1, p.City, p.State, p.PostalCode, p.PropertyType, p.YearBuilt, p.Notes, jobCount, p.UpdatedAt);
}
