using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class ConsentService(RutrDbContext db)
{
    public async Task<IReadOnlyList<ConsentDto>> ListAsync(Guid orgId, Guid? contactId, Guid? jobId, CancellationToken ct = default)
    {
        var q = db.HomeownerConsents.AsNoTracking().Where(c => c.OrganizationId == orgId);
        if (contactId.HasValue) q = q.Where(c => c.ContactId == contactId);
        if (jobId.HasValue) q = q.Where(c => c.JobId == jobId);
        var rows = await q.ToListAsync(ct);
        return rows.OrderByDescending(c => c.UpdatedAt).Select(Map).ToList();
    }

    public async Task<ConsentDto?> GetAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var c = await db.HomeownerConsents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        return c is null ? null : Map(c);
    }

    public async Task<ConsentDto> CreateAsync(Guid orgId, ConsentWriteRequest req, CancellationToken ct = default)
    {
        if (req.ContactId is null && req.JobId is null)
        {
            throw new InvalidOperationException("Consent requires ContactId and/or JobId.");
        }

        var entity = new HomeownerConsent
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            JobId = req.JobId,
            ContactId = req.ContactId,
            SmsOptIn = req.SmsOptIn,
            EmailOptIn = req.EmailOptIn,
            SmsStopped = req.SmsStopped,
            Source = string.IsNullOrWhiteSpace(req.Source) ? "manual" : req.Source.Trim(),
            DisclosureText = req.DisclosureText,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.HomeownerConsents.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<ConsentDto?> UpdateAsync(Guid orgId, Guid id, ConsentWriteRequest req, CancellationToken ct = default)
    {
        var entity = await db.HomeownerConsents.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return null;
        entity.JobId = req.JobId;
        entity.ContactId = req.ContactId;
        entity.SmsOptIn = req.SmsOptIn;
        entity.EmailOptIn = req.EmailOptIn;
        entity.SmsStopped = req.SmsStopped;
        if (!string.IsNullOrWhiteSpace(req.Source)) entity.Source = req.Source.Trim();
        entity.DisclosureText = req.DisclosureText;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var entity = await db.HomeownerConsents.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return false;
        db.HomeownerConsents.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static ConsentDto Map(HomeownerConsent c) =>
        new(c.Id, c.JobId, c.ContactId, c.SmsOptIn, c.EmailOptIn, c.SmsStopped, c.Source, c.DisclosureText, c.UpdatedAt);
}
