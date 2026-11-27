namespace Rutr.Api.Models.Entities;

public class MessageDraft
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string Channel { get; set; } = "Sms";
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = "PendingApproval";
    public string EvidenceSummary { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? ApprovedByStaffId { get; set; }
}
