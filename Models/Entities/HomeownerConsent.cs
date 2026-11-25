namespace Rutr.Api.Models.Entities;

public class HomeownerConsent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public bool SmsOptIn { get; set; } = true;
    public bool EmailOptIn { get; set; } = true;
    public bool SmsStopped { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
