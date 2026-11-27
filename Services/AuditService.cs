using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class AuditService(RutrDbContext db)
{
    public async Task WriteAsync(
        Guid organizationId,
        string action,
        string detail,
        Guid? jobId = null,
        Guid? draftId = null,
        Guid? staffUserId = null,
        CancellationToken ct = default)
    {
        db.AuditEvents.Add(new AuditEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            JobId = jobId,
            DraftId = draftId,
            StaffUserId = staffUserId,
            Action = action,
            Detail = detail,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AuditEventDto>> ListForJobAsync(
        Guid organizationId,
        Guid jobId,
        CancellationToken ct = default)
    {
        // SQLite cannot ORDER BY DateTimeOffset — sort in memory.
        var rows = await db.AuditEvents
            .AsNoTracking()
            .Where(e => e.OrganizationId == organizationId && e.JobId == jobId)
            .ToListAsync(ct);

        return rows
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .Select(e => new AuditEventDto(e.Id, e.JobId, e.DraftId, e.Action, e.Detail, e.CreatedAt))
            .ToList();
    }
}
