using Microsoft.Extensions.Options;
using Rutr.Api.Configuration;

namespace Rutr.Api.Services.JobNimbus;

public class MockJobNimbusClient(IOptions<IntegrationOptions> options, ILogger<MockJobNimbusClient> logger)
{
    public bool IsMockMode => options.Value.UseMocks;

    public Task SyncJobsAsync(Guid organizationId, CancellationToken ct = default)
    {
        if (!IsMockMode)
        {
            logger.LogInformation("Live JobNimbus sync not configured for org {OrgId}", organizationId);
            return Task.CompletedTask;
        }

        logger.LogInformation("Mock JobNimbus sync skipped — seed data already loaded for org {OrgId}", organizationId);
        return Task.CompletedTask;
    }
}
