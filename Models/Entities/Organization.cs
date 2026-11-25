namespace Rutr.Api.Models.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<StaffUser> StaffUsers { get; set; } = new List<StaffUser>();
}
