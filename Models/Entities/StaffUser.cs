namespace Rutr.Api.Models.Entities;

public class StaffUser
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Organization Organization { get; set; } = null!;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = "Pm";
    public string PasswordHash { get; set; } = string.Empty;
}
