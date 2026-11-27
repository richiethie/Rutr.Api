namespace Rutr.Api.Models.Entities;

public class IntegrationConnection
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public bool IsConnected { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset? ConnectedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
