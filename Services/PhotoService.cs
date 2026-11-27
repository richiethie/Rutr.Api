using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;

namespace Rutr.Api.Services;

public class PhotoService(RutrDbContext db, AuditService audit)
{
    public async Task<PhotoDto?> SetVisibilityAsync(
        Guid organizationId,
        Guid jobId,
        Guid photoId,
        bool customerVisible,
        Guid? staffUserId,
        CancellationToken ct = default)
    {
        var photo = await db.Photos
            .FirstOrDefaultAsync(
                p => p.OrganizationId == organizationId && p.JobId == jobId && p.Id == photoId,
                ct);

        if (photo is null)
        {
            return null;
        }

        photo.CustomerVisible = customerVisible;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(
            organizationId,
            customerVisible ? "photo.made_visible" : "photo.hidden",
            $"Photo {photo.ExternalCompanyCamId}: customerVisible={customerVisible}",
            jobId,
            staffUserId: staffUserId,
            ct: ct);

        return new PhotoDto(photo.Id, photo.Caption, photo.ImageUrl, photo.CustomerVisible, photo.CapturedAt);
    }
}
