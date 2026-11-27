using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

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
                j.Title,
                j.JobType,
                j.AddressLine1,
                j.City,
                j.State,
                j.HomeownerName,
                j.Status,
                j.Stage,
                j.PropertyId,
                j.PrimaryContactId,
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

        return job is null ? null : MapDetail(job);
    }

    public async Task<JobDetailDto> CreateAsync(Guid orgId, JobWriteRequest req, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var job = ApplyWrite(new Job
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            CreatedAt = now
        }, req, now);
        db.Jobs.Add(job);
        db.HomeownerConsents.Add(new HomeownerConsent
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            JobId = job.Id,
            ContactId = job.PrimaryContactId,
            SmsOptIn = true,
            EmailOptIn = true,
            Source = "job_create",
            UpdatedAt = now
        });
        await db.SaveChangesAsync(ct);
        return (await GetAsync(orgId, job.Id, ct))!;
    }

    public async Task<JobDetailDto?> UpdateAsync(Guid orgId, Guid id, JobWriteRequest req, CancellationToken ct = default)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.OrganizationId == orgId && j.Id == id, ct);
        if (job is null) return null;
        ApplyWrite(job, req, DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return await GetAsync(orgId, id, ct);
    }

    public async Task<JobDetailDto?> UpdateStatusAsync(Guid orgId, Guid id, JobStatusUpdateRequest req, CancellationToken ct = default)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.OrganizationId == orgId && j.Id == id, ct);
        if (job is null) return null;
        job.Status = req.Status.Trim();
        if (!string.IsNullOrWhiteSpace(req.Stage)) job.Stage = req.Stage.Trim();
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return await GetAsync(orgId, id, ct);
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.OrganizationId == orgId && j.Id == id, ct);
        if (job is null) return false;
        db.Jobs.Remove(job);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static Job ApplyWrite(Job job, JobWriteRequest req, DateTimeOffset now)
    {
        job.Title = string.IsNullOrWhiteSpace(req.Title) ? req.AddressLine1.Trim() : req.Title.Trim();
        job.JobType = string.IsNullOrWhiteSpace(req.JobType) ? "Retail" : req.JobType.Trim();
        job.AddressLine1 = req.AddressLine1.Trim();
        job.City = req.City.Trim();
        job.State = req.State.Trim();
        job.PostalCode = req.PostalCode.Trim();
        job.HomeownerName = req.HomeownerName.Trim();
        job.HomeownerEmail = req.HomeownerEmail.Trim();
        job.HomeownerPhone = req.HomeownerPhone.Trim();
        job.Status = string.IsNullOrWhiteSpace(req.Status) ? job.Status is { Length: > 0 } ? job.Status : "Lead" : req.Status.Trim();
        job.Stage = string.IsNullOrWhiteSpace(req.Stage) ? job.Stage is { Length: > 0 } ? job.Stage : "Intake" : req.Stage.Trim();
        job.ExternalJobNimbusId = req.ExternalJobNimbusId?.Trim() ?? job.ExternalJobNimbusId;
        job.PropertyId = req.PropertyId;
        job.PrimaryContactId = req.PrimaryContactId;
        job.UpdatedAt = now;
        return job;
    }

    private static JobDetailDto MapDetail(Job job)
    {
        var portal = job.PortalTokens
            .Where(t => t.ExpiresAt > DateTimeOffset.UtcNow)
            .OrderByDescending(t => t.CreatedAt.UtcDateTime)
            .FirstOrDefault();

        return new JobDetailDto(
            job.Id,
            job.Title,
            job.JobType,
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
            job.PropertyId,
            job.PrimaryContactId,
            job.UpdatedAt,
            job.Photos
                .OrderByDescending(p => p.CapturedAt.UtcDateTime)
                .Select(p => new PhotoDto(p.Id, p.Caption, p.ImageUrl, p.CustomerVisible, p.CapturedAt))
                .ToList(),
            job.MessageDrafts
                .OrderByDescending(d => d.CreatedAt.UtcDateTime)
                .Select(d => new MessageDraftDto(d.Id, d.JobId, d.Channel, d.Subject, d.Body, d.Status, d.EvidenceSummary, d.CreatedAt, d.ApprovedAt, d.ApprovedByStaffId))
                .ToList(),
            portal?.Token);
    }
}
