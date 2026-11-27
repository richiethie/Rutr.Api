using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services;

public class IntegrationConnectionService(RutrDbContext db)
{
    public async Task<IReadOnlyList<IntegrationConnectionDto>> ListAsync(Guid orgId, CancellationToken ct = default)
    {
        var rows = await db.IntegrationConnections.AsNoTracking()
            .Where(c => c.OrganizationId == orgId)
            .ToListAsync(ct);
        return rows.OrderBy(c => c.Provider).Select(Map).ToList();
    }

    public async Task<IntegrationConnectionDto?> ConnectAsync(Guid orgId, string provider, CancellationToken ct = default)
    {
        var entity = await FindOrCreateAsync(orgId, provider, ct);
        entity.IsConnected = true;
        entity.ConnectedAt = DateTimeOffset.UtcNow;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task<IntegrationConnectionDto?> DisconnectAsync(Guid orgId, string provider, CancellationToken ct = default)
    {
        var entity = await FindOrCreateAsync(orgId, provider, ct);
        entity.IsConnected = false;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(entity);
    }

    private async Task<IntegrationConnection> FindOrCreateAsync(Guid orgId, string provider, CancellationToken ct)
    {
        var key = provider.Trim();
        var entity = await db.IntegrationConnections
            .FirstOrDefaultAsync(c => c.OrganizationId == orgId && c.Provider == key, ct);
        if (entity is not null) return entity;

        entity = new IntegrationConnection
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Provider = key,
            DisplayName = key,
            IsConnected = false,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.IntegrationConnections.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity;
    }

    private static IntegrationConnectionDto Map(IntegrationConnection c) =>
        new(c.Id, c.Provider, c.DisplayName, c.IsConnected, c.ConnectedAt, c.UpdatedAt);
}
