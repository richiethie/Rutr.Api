using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class TaskService(RutrDbContext db)
{
    public async Task<IReadOnlyList<TaskDto>> ListAsync(Guid orgId, Guid? jobId, Guid? contactId, CancellationToken ct = default)
    {
        var q = db.Tasks.AsNoTracking().Where(t => t.OrganizationId == orgId);
        if (jobId.HasValue) q = q.Where(t => t.JobId == jobId);
        if (contactId.HasValue) q = q.Where(t => t.ContactId == contactId);
        var rows = await q.ToListAsync(ct);
        return rows.OrderBy(t => t.Status).ThenBy(t => t.DueAt).Select(Map).ToList();
    }

    public async Task<TaskDto?> GetAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var t = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        return t is null ? null : Map(t);
    }

    public async Task<TaskDto> CreateAsync(Guid orgId, TaskWriteRequest req, CancellationToken ct = default)
    {
        if (req.JobId is null && req.ContactId is null)
        {
            throw new InvalidOperationException("Task requires JobId and/or ContactId.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new JobTask
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            JobId = req.JobId,
            ContactId = req.ContactId,
            Title = req.Title.Trim(),
            Description = req.Description?.Trim() ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(req.Status) ? "Open" : req.Status.Trim(),
            DueAt = req.DueAt,
            AssignedStaffId = req.AssignedStaffId,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Tasks.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<TaskDto?> UpdateAsync(Guid orgId, Guid id, TaskWriteRequest req, CancellationToken ct = default)
    {
        var entity = await db.Tasks.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return null;
        entity.JobId = req.JobId;
        entity.ContactId = req.ContactId;
        entity.Title = req.Title.Trim();
        entity.Description = req.Description?.Trim() ?? string.Empty;
        entity.Status = string.IsNullOrWhiteSpace(req.Status) ? entity.Status : req.Status.Trim();
        entity.DueAt = req.DueAt;
        entity.AssignedStaffId = req.AssignedStaffId;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var entity = await db.Tasks.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return false;
        db.Tasks.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static TaskDto Map(JobTask t) =>
        new(t.Id, t.JobId, t.ContactId, t.Title, t.Description, t.Status, t.DueAt, t.AssignedStaffId, t.UpdatedAt);
}
