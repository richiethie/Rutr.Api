using Microsoft.Extensions.Options;
using Rutr.Api.Configuration;

namespace Rutr.Api.Services.CompanyCam;

public record CompanyCamPhotoPayload(
    string ExternalId,
    string JobExternalId,
    string Caption,
    string ImageUrl,
    DateTimeOffset CapturedAt);

public class MockCompanyCamClient(IOptions<IntegrationOptions> options, ILogger<MockCompanyCamClient> logger)
{
    public bool IsMockMode => options.Value.UseMocks;

    public Task<IReadOnlyList<CompanyCamPhotoPayload>> FetchPhotosAsync(
        Guid organizationId,
        IReadOnlyCollection<string> knownExternalIds,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsMockMode)
        {
            logger.LogWarning("Live CompanyCam fetch not configured for org {OrgId}", organizationId);
            return Task.FromResult<IReadOnlyList<CompanyCamPhotoPayload>>(Array.Empty<CompanyCamPhotoPayload>());
        }

        var catalog = BuildCatalog();
        var fresh = catalog.Where(p => !knownExternalIds.Contains(p.ExternalId)).ToList();
        logger.LogInformation(
            "Mock CompanyCam returning {Count} new photos for org {OrgId}",
            fresh.Count,
            organizationId);
        return Task.FromResult<IReadOnlyList<CompanyCamPhotoPayload>>(fresh);
    }

    private static IReadOnlyList<CompanyCamPhotoPayload> BuildCatalog()
    {
        var now = DateTimeOffset.UtcNow;
        return
        [
            new("cc-9001", "jn-18402", "Tear-off complete — north slope clear",
                "https://images.unsplash.com/photo-1632759145351-1d592919f540?w=1200&q=80", now.AddHours(-5)),
            new("cc-9002", "jn-18402", "Underlayment rolled — ridge line ready",
                "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=1200&q=80", now.AddHours(-3)),
            new("cc-9101", "jn-18455", "Materials staged in driveway",
                "https://images.unsplash.com/photo-1503387762-592deb58ef4e?w=1200&q=80", now.AddHours(-2)),
            new("cc-8801", "jn-18310", "Final walkthrough — south elevation",
                "https://images.unsplash.com/photo-1560518883-ce09059eeffa?w=1200&q=80", now.AddDays(-3)),
            // Incremental sync payload — appears after first seed so POST /integrations/sync creates a new draft.
            new("cc-9003", "jn-18402", "Shingles started on north elevation",
                "https://images.unsplash.com/photo-1486406146926-c627a92ad1ab?w=1200&q=80", now.AddMinutes(-15)),
            new("cc-9201", "jn-18501", "Inspection ladder set — north eaves",
                "https://images.unsplash.com/photo-1504307651254-35680f356dfd?w=1200&q=80", now.AddMinutes(-10))
        ];
    }
}
