using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class ContactService(RutrDbContext db)
{
    public async Task<IReadOnlyList<ContactDto>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        var rows = await db.Contacts.AsNoTracking()
            .Where(c => c.OrganizationId == orgId)
            .ToListAsync(ct);
        return rows.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).Select(Map).ToList();
    }

    public async Task<ContactDto?> GetAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var c = await db.Contacts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        return c is null ? null : Map(c);
    }

    public async Task<ContactDto> CreateAsync(Guid orgId, ContactWriteRequest req, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new Contact
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),
            Email = req.Email.Trim(),
            Phone = req.Phone.Trim(),
            Role = string.IsNullOrWhiteSpace(req.Role) ? "Homeowner" : req.Role.Trim(),
            PreferredChannel = string.IsNullOrWhiteSpace(req.PreferredChannel) ? "Sms" : req.PreferredChannel.Trim(),
            Language = string.IsNullOrWhiteSpace(req.Language) ? "en" : req.Language.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Contacts.Add(entity);
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<ContactDto?> UpdateAsync(Guid orgId, Guid id, ContactWriteRequest req, CancellationToken ct = default)
    {
        var entity = await db.Contacts.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return null;
        entity.FirstName = req.FirstName.Trim();
        entity.LastName = req.LastName.Trim();
        entity.Email = req.Email.Trim();
        entity.Phone = req.Phone.Trim();
        entity.Role = string.IsNullOrWhiteSpace(req.Role) ? entity.Role : req.Role.Trim();
        entity.PreferredChannel = string.IsNullOrWhiteSpace(req.PreferredChannel) ? entity.PreferredChannel : req.PreferredChannel.Trim();
        entity.Language = string.IsNullOrWhiteSpace(req.Language) ? entity.Language : req.Language.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<bool> DeleteAsync(Guid orgId, Guid id, CancellationToken ct = default)
    {
        var entity = await db.Contacts.FirstOrDefaultAsync(x => x.OrganizationId == orgId && x.Id == id, ct);
        if (entity is null) return false;
        db.Contacts.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static ContactDto Map(Contact c) =>
        new(c.Id, c.FirstName, c.LastName, $"{c.FirstName} {c.LastName}".Trim(), c.Email, c.Phone, c.Role,
            c.PreferredChannel, c.Language, c.UpdatedAt);
}
