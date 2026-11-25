using Microsoft.Extensions.Options;
using Rutr.Api.Configuration;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services.Messaging;

/// <summary>
/// Stand-in for Twilio SMS / Resend email until live keys are wired.
/// </summary>
public class MockDigestSender(IOptions<IntegrationOptions> options, ILogger<MockDigestSender> logger)
{
    public bool IsMockMode => options.Value.UseMocks;

    public Task SendAsync(MessageDraft draft, Job job, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var channel = draft.Channel.Equals("Email", StringComparison.OrdinalIgnoreCase) ? "email" : "sms";
        var to = channel == "email" ? job.HomeownerEmail : job.HomeownerPhone;
        logger.LogInformation(
            "Mock digest {Channel} to {To} for job {JobId}: {Subject}",
            channel,
            to,
            job.Id,
            draft.Subject);
        return Task.CompletedTask;
    }
}
