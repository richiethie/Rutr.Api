using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Services;

public class JobService(RutrDbContext db)
{
    public async Task<IReadOnlyList<JobListItemDto>> ListAsync(Guid organizationId, CancellationToken ct = default)
    {
        var rows = await db.Jobs
            .AsNoTracking()
            .Where(j => j.OrganizationId == organizationId)
            .Select(j => new JobListItemDto(
                j.Id,
                j.AddressLine1,
                j.City,
                j.State,
                j.HomeownerName,
                j.Status,
                j.Stage,
                j.MessageDrafts.Count(d => d.Status == "PendingApproval"),
                j.Photos.Count,
                j.UpdatedAt))
            .ToListAsync(ct);

        return rows.OrderByDescending(j => j.UpdatedAt).ToList();
    }

    public async Task<JobDetailDto?> GetAsync(Guid organizationId, Guid jobId, CancellationToken ct = default)
    {
        var job = await db.Jobs
            .AsNoTracking()
            .Include(j => j.Photos)
            .Include(j => j.MessageDrafts)
            .Include(j => j.PortalTokens)
            .FirstOrDefaultAsync(j => j.OrganizationId == organizationId && j.Id == jobId, ct);

        if (job is null)
        {
            return null;
        }

        var portal = job.PortalTokens
            .Where(t => t.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt.UtcDateTime)
            .FirstOrDefault();

        return new JobDetailDto(
            job.Id,
            job.AddressLine1,
            job.City,
            job.State,
            job.PostalCode,
            job.HomeownerName,
            job.HomeownerEmail,
            job.HomeownerPhone,
            job.Status,
            job.Stage,
            job.ExternalJobNimbusId,
            job.UpdatedAt,
            job.Photos
                .OrderByDescending(p => p.CapturedAt.UtcDateTime)
                .Select(p => new PhotoDto(p.Id, p.Caption, p.ImageUrl, p.CustomerVisible, p.CapturedAt))
                .ToList(),
            job.MessageDrafts
                .OrderByDescending(d => d.CreatedAt.UtcDateTime)
                .Select(d => new MessageDraftDto(d.Id, d.Channel, d.Subject, d.Body, d.Status, d.EvidenceSummary, d.CreatedAt, d.ApprovedAt))
                .ToList(),
            portal?.Token);
    }
}
