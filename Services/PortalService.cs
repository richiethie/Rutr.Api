using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Services;

public class PortalService(RutrDbContext db)
{
    public async Task<PortalJobDto?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var portal = await db.PortalTokens
            .AsNoTracking()
            .Include(t => t.Job)
                .ThenInclude(j => j.Photos)
            .Include(t => t.Job)
                .ThenInclude(j => j.MessageDrafts)
            .Include(t => t.Job)
                .ThenInclude(j => j.Organization)
            .FirstOrDefaultAsync(t => t.Token == token, ct);

        if (portal is null || portal.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var job = portal.Job;
        var latest = job.MessageDrafts
            .Where(d => d.Status == "Approved")
            .OrderByDescending(d => d.ApprovedAt?.UtcDateTime ?? DateTime.MinValue)
            .Select(d => d.Body)
            .FirstOrDefault()
            ?? "Your project is underway. New verified updates will appear here.";

        return new PortalJobDto(
            job.Organization.Name,
            job.AddressLine1,
            job.City,
            job.State,
            job.HomeownerName,
            job.Status,
            job.Stage,
            latest,
            job.Photos
                .Where(p => p.CustomerVisible)
                .OrderByDescending(p => p.CapturedAt.UtcDateTime)
                .Select(p => new PhotoDto(p.Id, p.Caption, p.ImageUrl, p.CustomerVisible, p.CapturedAt))
                .ToList());
    }
}
