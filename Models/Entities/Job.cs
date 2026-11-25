namespace Rutr.Api.Models.Entities;

public class Job
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string ExternalJobNimbusId { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string HomeownerName { get; set; } = string.Empty;
    public string HomeownerEmail { get; set; } = string.Empty;
    public string HomeownerPhone { get; set; } = string.Empty;
    public string Status { get; set; } = "InProgress";
    public string Stage { get; set; } = "Build";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<PhotoEvidence> Photos { get; set; } = new List<PhotoEvidence>();
    public ICollection<MessageDraft> MessageDrafts { get; set; } = new List<MessageDraft>();
    public ICollection<PortalToken> PortalTokens { get; set; } = new List<PortalToken>();
}
