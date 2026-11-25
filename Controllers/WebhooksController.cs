using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rutr.Api.Data;
using Rutr.Api.Services;

namespace Rutr.Api.Controllers;

/// <summary>
/// Inbound vendor webhooks (MVP stubs). Signature verification lands with live keys.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/v1/webhooks")]
public class WebhooksController(
    ConnectSyncService sync,
    RutrDbContext db,
    ILogger<WebhooksController> logger) : ControllerBase
{
    [HttpPost("companycam")]
    public async Task<IActionResult> CompanyCam([FromBody] object? payload, CancellationToken ct)
    {
        logger.LogInformation("CompanyCam webhook received ({Bytes} chars payload) — running mock sync for demo org",
            payload?.ToString()?.Length ?? 0);
        var result = await sync.SyncAsync(SeedData.OrgId, null, ct);
        return Accepted(result);
    }

    [HttpPost("jobnimbus")]
    public async Task<IActionResult> JobNimbus([FromBody] object? payload, CancellationToken ct)
    {
        logger.LogInformation("JobNimbus webhook received — running mock sync for demo org");
        var result = await sync.SyncAsync(SeedData.OrgId, null, ct);
        return Accepted(result);
    }

    /// <summary>Twilio-style STOP keyword handler (form or JSON body with From=phone).</summary>
    [HttpPost("twilio/sms")]
    public async Task<IActionResult> TwilioSms([FromForm] string? From, [FromForm] string? Body, CancellationToken ct)
    {
        var text = (Body ?? string.Empty).Trim();
        if (!text.Equals("STOP", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(From))
        {
            return Ok(new { ignored = true });
        }

        var normalized = From.Trim();
        var consents = await db.HomeownerConsents
            .Include(c => c.Job)
            .Where(c => c.OrganizationId == SeedData.OrgId && c.Job != null && c.Job.HomeownerPhone == normalized)
            .ToListAsync(ct);

        foreach (var consent in consents)
        {
            consent.SmsStopped = true;
            consent.SmsOptIn = false;
            consent.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("SMS STOP applied for {Phone} ({Count} jobs)", normalized, consents.Count);
        return Ok(new { stopped = consents.Count });
    }
}
