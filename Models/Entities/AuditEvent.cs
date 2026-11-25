namespace Rutr.Api.Models.Entities;

public class AuditEvent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? JobId { get; set; }
    public Guid? DraftId { get; set; }
    public Guid? StaffUserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
