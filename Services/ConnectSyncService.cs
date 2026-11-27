using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;
using Rutr.Api.Services.Ai;
using Rutr.Api.Services.CompanyCam;
using Rutr.Api.Services.JobNimbus;

namespace Rutr.Api.Services;

/// <summary>
/// Connect MVP sync: JobNimbus jobs → CompanyCam evidence → pending drafts → portal tokens.
/// </summary>
public class ConnectSyncService(
    RutrDbContext db,
    MockJobNimbusClient jobNimbus,
    MockCompanyCamClient companyCam,
    MockDraftGenerator draftGenerator,
    AuditService audit,
    ILogger<ConnectSyncService> logger)
{
    public IntegrationStatusDto Status() => new(
        jobNimbus.IsMockMode,
        jobNimbus.IsMockMode ? "mock" : "live-not-configured",
        companyCam.IsMockMode ? "mock" : "live-not-configured",
        "mock");

    public async Task<SyncResultDto> SyncAsync(Guid organizationId, Guid? staffUserId, CancellationToken ct = default)
    {
        var jobsUpserted = await UpsertJobsAsync(organizationId, ct);
        var (photosAdded, draftsCreated) = await SyncPhotosAndDraftsAsync(organizationId, staffUserId, ct);
        var portals = await EnsurePortalTokensAsync(organizationId, ct);

        var message =
            $"Synced org {organizationId}: {jobsUpserted} jobs, {photosAdded} photos, {draftsCreated} drafts, {portals} portal tokens.";
        logger.LogInformation("{Message}", message);
        await audit.WriteAsync(
            organizationId,
            "integrations.sync",
            message,
            staffUserId: staffUserId,
            ct: ct);

        return new SyncResultDto(
            jobNimbus.IsMockMode,
            jobsUpserted,
            photosAdded,
            draftsCreated,
            portals,
            message);
    }

    private async Task<int> UpsertJobsAsync(Guid organizationId, CancellationToken ct)
    {
        var payloads = await jobNimbus.FetchJobsAsync(organizationId, ct);
        var existing = await db.Jobs
            .Where(j => j.OrganizationId == organizationId)
            .ToListAsync(ct);
        var byExternal = existing.ToDictionary(j => j.ExternalJobNimbusId, StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var upserted = 0;

        foreach (var payload in payloads)
        {
            if (byExternal.TryGetValue(payload.ExternalId, out var job))
            {
                job.AddressLine1 = payload.AddressLine1;
                job.City = payload.City;
                job.State = payload.State;
                job.PostalCode = payload.PostalCode;
                job.HomeownerName = payload.HomeownerName;
                job.HomeownerEmail = payload.HomeownerEmail;
                job.HomeownerPhone = payload.HomeownerPhone;
                job.Status = payload.Status;
                job.Stage = payload.Stage;
                job.UpdatedAt = now;
            }
            else
            {
                job = new Job
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    ExternalJobNimbusId = payload.ExternalId,
                    Title = $"{payload.HomeownerName} — {payload.AddressLine1}",
                    JobType = "Retail",
                    AddressLine1 = payload.AddressLine1,
                    City = payload.City,
                    State = payload.State,
                    PostalCode = payload.PostalCode,
                    HomeownerName = payload.HomeownerName,
                    HomeownerEmail = payload.HomeownerEmail,
                    HomeownerPhone = payload.HomeownerPhone,
                    Status = payload.Status,
                    Stage = payload.Stage,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Jobs.Add(job);
                db.HomeownerConsents.Add(new HomeownerConsent
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    JobId = job.Id,
                    SmsOptIn = true,
                    EmailOptIn = true,
                    SmsStopped = false,
                    UpdatedAt = now
                });
                byExternal[payload.ExternalId] = job;
            }

            upserted++;
        }

        await db.SaveChangesAsync(ct);
        return upserted;
    }

    private async Task<(int PhotosAdded, int DraftsCreated)> SyncPhotosAndDraftsAsync(
        Guid organizationId,
        Guid? staffUserId,
        CancellationToken ct)
    {
        var jobs = await db.Jobs
            .Include(j => j.Photos)
            .Where(j => j.OrganizationId == organizationId)
            .ToListAsync(ct);

        var jobsByExternal = jobs.ToDictionary(j => j.ExternalJobNimbusId, StringComparer.OrdinalIgnoreCase);
        var knownPhotoIds = jobs.SelectMany(j => j.Photos).Select(p => p.ExternalCompanyCamId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var payloads = await companyCam.FetchPhotosAsync(organizationId, knownPhotoIds, ct);

        var photosAdded = 0;
        var newByJob = new Dictionary<Guid, List<PhotoEvidence>>();

        foreach (var payload in payloads)
        {
            if (!jobsByExternal.TryGetValue(payload.JobExternalId, out var job))
            {
                continue;
            }

            var photo = new PhotoEvidence
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                JobId = job.Id,
                ExternalCompanyCamId = payload.ExternalId,
                Caption = payload.Caption,
                ImageUrl = payload.ImageUrl,
                CustomerVisible = false,
                CapturedAt = payload.CapturedAt
            };
            db.Photos.Add(photo);
            job.UpdatedAt = DateTimeOffset.UtcNow;
            photosAdded++;
            if (!newByJob.TryGetValue(job.Id, out var list))
            {
                list = [];
                newByJob[job.Id] = list;
            }

            list.Add(photo);
        }

        await db.SaveChangesAsync(ct);

        var draftsCreated = 0;
        foreach (var (jobId, photos) in newByJob)
        {
            var job = jobs.First(j => j.Id == jobId);
            var evidence = string.Join(", ", photos.Select(p => $"CompanyCam {p.ExternalCompanyCamId} ({p.Caption})"));
            var (subject, body) = draftGenerator.Generate(job, photos);
            var draft = new MessageDraft
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                JobId = jobId,
                Channel = job.HomeownerPhone.Length > 0 ? "Sms" : "Email",
                Subject = subject,
                Body = body,
                Status = "PendingApproval",
                EvidenceSummary = evidence,
                CreatedAt = DateTimeOffset.UtcNow
            };
            db.MessageDrafts.Add(draft);
            draftsCreated++;
            await audit.WriteAsync(
                organizationId,
                "draft.created",
                $"Draft from sync: {evidence}",
                jobId,
                draft.Id,
                staffUserId,
                ct);
        }

        if (draftsCreated > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return (photosAdded, draftsCreated);
    }

    private async Task<int> EnsurePortalTokensAsync(Guid organizationId, CancellationToken ct)
    {
        var jobs = await db.Jobs
            .Include(j => j.PortalTokens)
            .Where(j => j.OrganizationId == organizationId && j.Status != "Complete")
            .ToListAsync(ct);

        var ensured = 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var job in jobs)
        {
            var active = job.PortalTokens.Any(t => t.ExpiresAt > now);
            if (active)
            {
                continue;
            }

            var token = new PortalToken
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                JobId = job.Id,
                Token = BuildPortalSlug(job),
                ExpiresAt = now.AddDays(30),
                CreatedAt = now
            };
            db.PortalTokens.Add(token);
            ensured++;
        }

        if (ensured > 0)
        {
            await db.SaveChangesAsync(ct);
        }

        return ensured;
    }

    private static string BuildPortalSlug(Job job)
    {
        var last = job.HomeownerName
            .Split([' ', '&'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault()
            ?.ToLowerInvariant() ?? "homeowner";
        var street = job.AddressLine1
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .FirstOrDefault()
            ?.ToLowerInvariant() ?? "job";
        return $"{last}-{street}-demo";
    }
}
