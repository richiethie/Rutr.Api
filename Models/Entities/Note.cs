namespace Rutr.Api.Models.Entities;

public class Note
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? JobId { get; set; }
    public Job? Job { get; set; }
    public Guid? ContactId { get; set; }
    public Contact? Contact { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? CreatedByStaffId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
