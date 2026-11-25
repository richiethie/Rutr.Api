using Microsoft.Extensions.Options;
using Rutr.Api.Configuration;

namespace Rutr.Api.Services.JobNimbus;

public record JobNimbusJobPayload(
    string ExternalId,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    string HomeownerName,
    string HomeownerEmail,
    string HomeownerPhone,
    string Status,
    string Stage);

public class MockJobNimbusClient(IOptions<IntegrationOptions> options, ILogger<MockJobNimbusClient> logger)
{
    public bool IsMockMode => options.Value.UseMocks;

    public Task<IReadOnlyList<JobNimbusJobPayload>> FetchJobsAsync(Guid organizationId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (!IsMockMode)
        {
            logger.LogWarning("Live JobNimbus fetch not configured for org {OrgId}", organizationId);
            return Task.FromResult<IReadOnlyList<JobNimbusJobPayload>>(Array.Empty<JobNimbusJobPayload>());
        }

        logger.LogInformation("Mock JobNimbus returning demo jobs for org {OrgId}", organizationId);
        IReadOnlyList<JobNimbusJobPayload> jobs =
        [
            new(
                "jn-18402",
                "1428 Maple Ridge Dr",
                "Cedar Falls",
                "IA",
                "50613",
                "Dana & Chris Keller",
                "keller@example.com",
                "+13195550142",
                "InProgress",
                "Build"),
            new(
                "jn-18455",
                "88 Prairie View Ct",
                "Waterloo",
                "IA",
                "50701",
                "Morgan Ellis",
                "morgan.ellis@example.com",
                "+13195550188",
                "InProgress",
                "Prepare"),
            new(
                "jn-18310",
                "409 Oak Street",
                "Waverly",
                "IA",
                "50677",
                "Pat Nguyen",
                "pat.nguyen@example.com",
                "+13195550109",
                "Complete",
                "Complete"),
            new(
                "jn-18501",
                "712 Birch Lane",
                "Cedar Falls",
                "IA",
                "50613",
                "Riley Brooks",
                "riley.brooks@example.com",
                "+13195550212",
                "InProgress",
                "Inspect")
        ];
        return Task.FromResult(jobs);
    }
}
