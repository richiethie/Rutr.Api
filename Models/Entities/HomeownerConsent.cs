namespace Rutr.Api.Models.Entities;

/// <summary>Channel consent provenance — preferably keyed to contact (TCPA-minded).</summary>
public class HomeownerConsent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? JobId { get; set; }
    public Job? Job { get; set; }
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }
    public bool SmsOptIn { get; set; } = true;
    public bool EmailOptIn { get; set; } = true;
    public bool SmsStopped { get; set; }
    public string Source { get; set; } = "manual";
    public string? DisclosureText { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
