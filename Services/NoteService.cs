using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class NoteService(RutrDbContext db)
{
    public async Task<IReadOnlyList<NoteDto>> ListAsync(Guid orgId, Guid? jobId, Guid? contactId, CancellationToken ct = default)
    {
        var q = db.Notes.AsNoTracking().Where(n => n.OrganizationId == orgId);
        if (jobId.HasValue) q = q.Where(n => n.JobId == jobId);
        if (contactId.HasValue) q = q.Where(n => n.ContactId == contactId);
        var rows = await q.ToListAsync(ct);
        return rows.OrderByDescending(n => n.CreatedAt).Select(Map).ToList();
    }

    public async Task<NoteDto?> GetAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var n = await db.Notes.AsNoTracking().FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        return n is null ? null : Map(n);
    }

    public async Task<NoteDto> CreateAsync(Guid orgId, Guid? staffId, NoteWriteRequest req, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new Note
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            JobId = req.JobId,
            ContactId = req.ContactId,
            Body = req.Body.Trim(),
            CreatedByStaffId = staffId,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Notes.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<NoteDto?> UpdateAsync(Guid orgId, Guid id, NoteWriteRequest req, CancellationToken ct = default)
    {
        var entity = await db.Notes.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return null;
        entity.JobId = req.JobId;
        entity.ContactId = req.ContactId;
        entity.Body = req.Body.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var entity = await db.Notes.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return false;
        db.Notes.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static NoteDto Map(Note n) =>
        new(n.Id, n.JobId, n.ContactId, n.Body, n.CreatedByStaffId, n.CreatedAt, n.UpdatedAt);
}
