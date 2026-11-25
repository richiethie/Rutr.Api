using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class DraftService(RutrDbContext db, AuditService audit)
{
    public async Task<IReadOnlyList<MessageDraftDto>> ListAsync(Guid orgId, Guid? jobId, CancellationToken ct = default)
    {
        var q = db.MessageDrafts.AsNoTracking().Where(d => d.OrganizationId == orgId);
        if (jobId.HasValue) q = q.Where(d => d.JobId == jobId);
        var rows = await q.ToListAsync(ct);
        return rows.OrderByDescending(d => d.CreatedAt).Select(Map).ToList();
    }

    public async Task<MessageDraftDto?> GetAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var d = await db.MessageDrafts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        return d is null ? null : Map(d);
    }

    public async Task<MessageDraftDto> CreateAsync(Guid orgId, Guid? staffId, DraftWriteRequest req, CancellationToken ct = default)
    {
        var entity = new MessageDraft
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            JobId = req.JobId,
            Channel = req.Channel.Trim(),
            Subject = req.Subject.Trim(),
            Body = req.Body.Trim(),
            EvidenceSummary = req.EvidenceSummary?.Trim() ?? string.Empty,
            Status = string.IsNullOrWhiteSpace(req.Status) ? "PendingApproval" : req.Status.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        db.MessageDrafts.Add(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(orgId, "draft.created", $"Manual draft '{entity.Subject}'", entity.JobId, entity.Id, staffId, ct);
        return Map(entity);
    }

    public async Task<MessageDraftDto?> UpdateAsync(Guid orgId, Guid id, DraftWriteRequest req, CancellationToken ct = default)
    {
        var entity = await db.MessageDrafts.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return null;
        if (entity.Status is "Sent" or "Approved")
        {
            throw new InvalidOperationException("Cannot edit approved or sent drafts.");
        }

        entity.JobId = req.JobId;
        entity.Channel = req.Channel.Trim();
        entity.Subject = req.Subject.Trim();
        entity.Body = req.Body.Trim();
        entity.EvidenceSummary = req.EvidenceSummary?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(req.Status)) entity.Status = req.Status.Trim();
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid id, Guid? staffId, CancellationToken ct = default)
    {
        var entity = await db.MessageDrafts.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return false;
        if (entity.Status == "Sent")
        {
            throw new InvalidOperationException("Cannot delete a sent draft.");
        }

        var jobId = entity.JobId;
        db.MessageDrafts.Remove(entity);
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(orgId, "draft.deleted", "Draft deleted", jobId, id, staffId, ct);
        return true;
    }

    private static MessageDraftDto Map(MessageDraft d) =>
        new(d.Id, d.JobId, d.Channel, d.Subject, d.Body, d.Status, d.EvidenceSummary, d.CreatedAt, d.ApprovedAt, d.ApprovedByStaffId);
}
