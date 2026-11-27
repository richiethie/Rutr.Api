namespace Rutr.Api.Models.Entities;

public class JobTask
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? JobId { get; set; }
    public Job? Job { get; set; }
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Open";
    public DateTimeOffset? DueAt { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
