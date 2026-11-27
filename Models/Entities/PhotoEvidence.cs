namespace Rutr.Api.Models.Entities;

public class PhotoEvidence
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string ExternalCompanyCamId { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool CustomerVisible { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public DateTimeOffset? SoftDeletedAt { get; set; }
}
