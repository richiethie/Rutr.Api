using Microsoft.Extensions.Options;
using Rutr.Api.Configuration;

namespace Rutr.Api.Services.CompanyCam;

public class MockCompanyCamClient(IOptions<IntegrationOptions> options, ILogger<MockCompanyCamClient> logger)
{
    public bool IsMockMode => options.Value.UseMocks;

    public Task SyncPhotosAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (!IsMockMode)
        {
            logger.LogInformation("Live CompanyCam sync not configured for org {OrgId}", organizationId);
            return Task.CompletedTask;
        }

        logger.LogInformation("Mock CompanyCam sync skipped — seed evidence already loaded for org {OrgId}", organizationId);
        return Task.CompletedTask;
    }
}
