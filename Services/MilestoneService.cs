using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class MilestoneService(RutrDbContext db)
{
    public async Task<IReadOnlyList<MilestoneDto>> ListAsync(Guid orgId, Guid? jobId, CancellationToken ct = default)
    {
        var q = db.Milestones.AsNoTracking().Where(m => m.OrganizationId == orgId);
        if (jobId.HasValue) q = q.Where(m => m.JobId == jobId);
        var rows = await q.ToListAsync(ct);
        return rows.OrderBy(m => m.SortOrder).ThenBy(m => m.Name).Select(Map).ToList();
    }

    public async Task<MilestoneDto?> GetAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var m = await db.Milestones.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        return m is null ? null : Map(m);
    }

    public async Task<MilestoneDto> CreateAsync(Guid orgId, MilestoneWriteRequest req, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new Milestone
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            JobId = req.JobId,
            Name = req.Name.Trim(),
            Status = string.IsNullOrWhiteSpace(req.Status) ? "Pending" : req.Status.Trim(),
            SortOrder = req.SortOrder ?? 0,
            DueAt = req.DueAt,
            CompletedAt = req.CompletedAt,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Milestones.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<MilestoneDto?> UpdateAsync(Guid orgId, Guid id, MilestoneWriteRequest req, CancellationToken ct = default)
    {
        var entity = await db.Milestones.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return null;
        entity.JobId = req.JobId;
        entity.Name = req.Name.Trim();
        entity.Status = string.IsNullOrWhiteSpace(req.Status) ? entity.Status : req.Status.Trim();
        entity.SortOrder = req.SortOrder ?? entity.SortOrder;
        entity.DueAt = req.DueAt;
        entity.CompletedAt = req.CompletedAt;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var entity = await db.Milestones.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return false;
        db.Milestones.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static MilestoneDto Map(Milestone m) =>
        new(m.Id, m.JobId, m.Name, m.Status, m.SortOrder, m.DueAt, m.CompletedAt, m.UpdatedAt);
}
