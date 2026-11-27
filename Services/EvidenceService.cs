using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class EvidenceService(RutrDbContext db, AuditService audit)
{
    public async Task<IReadOnlyList<EvidenceDto>> ListAsync(Guid orgId, Guid? jobId, bool includeDeleted, CancellationToken ct = default)
    {
        IQueryable<PhotoEvidence> q = includeDeleted
            ? db.Photos.IgnoreQueryFilters().AsNoTracking().Where(p => p.OrganizationId == orgId)
            : db.Photos.AsNoTracking().Where(p => p.OrganizationId == orgId);
        if (jobId.HasValue) q = q.Where(p => p.JobId == jobId);
        var rows = await q.ToListAsync(ct);
        return rows.OrderByDescending(p => p.CapturedAt).Select(Map).ToList();
    }

    public async Task<EvidenceDto?> GetAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var p = await db.Photos.IgnoreQueryFilters().AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        return p is null ? null : Map(p);
    }

    public async Task<EvidenceDto> CreateAsync(Guid orgId, EvidenceWriteRequest req, CancellationToken ct = default)
    {
        var entity = new PhotoEvidence
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            JobId = req.JobId,
            Caption = req.Caption.Trim(),
            ImageUrl = req.ImageUrl.Trim(),
            ExternalCompanyCamId = string.IsNullOrWhiteSpace(req.ExternalCompanyCamId)
                ? $"manual-{Guid.NewGuid():N}"[..16]
                : req.ExternalCompanyCamId.Trim(),
            CustomerVisible = req.CustomerVisible ?? false,
            CapturedAt = req.CapturedAt ?? DateTimeOffset.UtcNow
        };
        db.Photos.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    /// <summary>Save an uploaded image under wwwroot/uploads and create evidence.</summary>
    public async Task<EvidenceDto> CreateFromUploadAsync(
        Guid orgId,
        Guid jobId,
        string caption,
        IFormFile file,
        string? externalCompanyCamId,
        bool customerVisible,
        string publicBaseUrl,
        string webRootPath,
        CancellationToken ct = default)
    {
        if (file.Length <= 0)
        {
            throw new InvalidOperationException("Empty file.");
        }

        if (file.Length > 12 * 1024 * 1024)
        {
            throw new InvalidOperationException("Image must be 12 MB or smaller.");
        }

        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (!contentType.StartsWith("image/"))
        {
            throw new InvalidOperationException("Only image files are allowed.");
        }

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || ext.Length > 8)
        {
            ext = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg",
            };
        }

        var uploadsDir = Path.Combine(webRootPath, "uploads");
        Directory.CreateDirectory(uploadsDir);
        var id = Guid.NewGuid();
        var fileName = $"{id:N}{ext.ToLowerInvariant()}";
        var diskPath = Path.Combine(uploadsDir, fileName);
        await using (var stream = File.Create(diskPath))
        {
            await file.CopyToAsync(stream, ct);
        }

        var imageUrl = $"{publicBaseUrl.TrimEnd('/')}/uploads/{fileName}";
        return await CreateAsync(
            orgId,
            new EvidenceWriteRequest(
                jobId,
                caption,
                imageUrl,
                externalCompanyCamId,
                customerVisible,
                DateTimeOffset.UtcNow),
            ct);
    }

    public async Task<EvidenceDto?> UpdateAsync(Guid orgId, Guid id, EvidenceWriteRequest req, CancellationToken ct = default)
    {
        var entity = await db.Photos.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return null;
        entity.JobId = req.JobId;
        entity.Caption = req.Caption.Trim();
        entity.ImageUrl = req.ImageUrl.Trim();
        if (!string.IsNullOrWhiteSpace(req.ExternalCompanyCamId))
            entity.ExternalCompanyCamId = req.ExternalCompanyCamId.Trim();
        if (req.CustomerVisible.HasValue) entity.CustomerVisible = req.CustomerVisible.Value;
        if (req.CapturedAt.HasValue) entity.CapturedAt = req.CapturedAt.Value;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<EvidenceDto?> SetVisibilityAsync(
        Guid orgId, Guid id, bool customerVisible, Guid? staffId, CancellationToken ct = default)
    {
        var entity = await db.Photos.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return null;
        entity.CustomerVisible = customerVisible;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(orgId, customerVisible ? "photo.made_visible" : "photo.hidden",
            $"Photo {entity.ExternalCompanyCamId}", entity.JobId, staffUserId: staffId, ct: ct);
        return Map(entity);
    }

    public async Task<bool> SoftDeleteAsync(Guid orgId, Guid id, Guid? staffId, CancellationToken ct = default)
    {
        var entity = await db.Photos.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return false;
        entity.SoftDeletedAt = DateTimeOffset.UtcNow;
        entity.CustomerVisible = false;
        await db.SaveChangesAsync(ct);
        await audit.WriteAsync(orgId, "photo.soft_deleted", $"Photo {entity.ExternalCompanyCamId}",
            entity.JobId, staffUserId: staffId, ct: ct);
        return true;
    }

    private static EvidenceDto Map(PhotoEvidence p) =>
        new(p.Id, p.JobId, p.ExternalCompanyCamId, p.Caption, p.ImageUrl, p.CustomerVisible, p.CapturedAt,
            p.SoftDeletedAt is not null);
}
