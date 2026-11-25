using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Rutr.Api.Configuration;
using Rutr.Api.Data;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Services.Messaging;

public record DigestSendResult(bool Sent, string Detail);

/// <summary>
/// Stand-in for Twilio SMS / Resend email until live keys are wired.
/// Enforces SMS STOP + quiet hours for Connect MVP.
/// </summary>
public class MockDigestSender(
    RutrDbContext db,
    IOptions<IntegrationOptions> integrations,
    IOptions<MessagingOptions> messaging,
    ILogger<MockDigestSender> logger)
{
    public bool IsMockMode => integrations.Value.UseMocks;

    public async Task<DigestSendResult> SendAsync(MessageDraft draft, Job job, CancellationToken ct = default)
    {
        var channel = draft.Channel.Equals("Email", StringComparison.OrdinalIgnoreCase) ? "email" : "sms";
        var consent = await db.HomeownerConsents
            .FirstOrDefaultAsync(c => c.OrganizationId == job.OrganizationId && c.JobId == job.Id, ct);

        if (channel == "sms")
        {
            if (consent?.SmsStopped == true || consent?.SmsOptIn == false)
            {
                return new DigestSendResult(false, "SMS blocked: homeowner STOP/opt-out.");
            }

            if (IsQuietHours())
            {
                return new DigestSendResult(false, "SMS deferred: quiet hours.");
            }
        }
        else if (consent?.EmailOptIn == false)
        {
            return new DigestSendResult(false, "Email blocked: homeowner opted out.");
        }

        var to = channel == "email" ? job.HomeownerEmail : job.HomeownerPhone;
        logger.LogInformation(
            "Mock digest {Channel} to {To} for job {JobId}: {Subject}",
            channel,
            to,
            job.Id,
            draft.Subject);
        return new DigestSendResult(true, $"Mock {channel} accepted for {to}.");
    }

    private bool IsQuietHours()
    {
        var opts = messaging.Value;
        var tz = ResolveTimeZone(opts.TimeZoneId);
        var local = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        var hour = local.Hour;
        return hour < opts.QuietHoursStartLocal || hour >= opts.QuietHoursEndLocal;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        }
    }
}
