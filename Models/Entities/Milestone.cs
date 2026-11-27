namespace Rutr.Api.Models.Entities;

public class Milestone
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public int SortOrder { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
