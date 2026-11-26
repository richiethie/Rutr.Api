using Microsoft.EntityFrameworkCore;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Data;

public class RutrDbContext(DbContextOptions<RutrDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();
    public DbSet<StaffInvite> StaffInvites => Set<StaffInvite>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<PhotoEvidence> Photos => Set<PhotoEvidence>();
    public DbSet<MessageDraft> MessageDrafts => Set<MessageDraft>();
    public DbSet<PortalToken> PortalTokens => Set<PortalToken>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<HomeownerConsent> HomeownerConsents => Set<HomeownerConsent>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<JobTask> Tasks => Set<JobTask>();
    public DbSet<Milestone> Milestones => Set<Milestone>();
    public DbSet<IntegrationConnection> IntegrationConnections => Set<IntegrationConnection>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Organization>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Slug).IsUnique();
        });

        modelBuilder.Entity<StaffUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Organization).WithMany(o => o.StaffUsers).HasForeignKey(x => x.OrganizationId);
        });

        modelBuilder.Entity<StaffInvite>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.InviteToken).IsUnique();
            e.HasIndex(x => new { x.OrganizationId, x.Email });
            e.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId);
        });

        modelBuilder.Entity<Contact>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.Email });
            e.HasOne(x => x.Organization).WithMany(o => o.Contacts).HasForeignKey(x => x.OrganizationId);
        });

        modelBuilder.Entity<Property>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.PostalCode });
            e.HasOne(x => x.Organization).WithMany(o => o.Properties).HasForeignKey(x => x.OrganizationId);
        });

        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.ExternalJobNimbusId });
            e.HasOne(x => x.Organization).WithMany(o => o.Jobs).HasForeignKey(x => x.OrganizationId);
            e.HasOne(x => x.Property).WithMany(p => p.Jobs).HasForeignKey(x => x.PropertyId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.PrimaryContact).WithMany().HasForeignKey(x => x.PrimaryContactId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PhotoEvidence>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Job).WithMany(j => j.Photos).HasForeignKey(x => x.JobId);
            e.HasQueryFilter(x => x.SoftDeletedAt == null);
        });

        modelBuilder.Entity<MessageDraft>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Job).WithMany(j => j.MessageDrafts).HasForeignKey(x => x.JobId);
        });

        modelBuilder.Entity<PortalToken>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasOne(x => x.Job).WithMany(j => j.PortalTokens).HasForeignKey(x => x.JobId);
        });

        modelBuilder.Entity<AuditEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.JobId, x.CreatedAt });
            e.Property(x => x.Action).HasMaxLength(64);
            e.Property(x => x.Detail).HasMaxLength(2000);
        });

        modelBuilder.Entity<HomeownerConsent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.ContactId });
            e.HasIndex(x => new { x.OrganizationId, x.JobId });
            e.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.SetNull);
            e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Note>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Job).WithMany(j => j.Notes).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<JobTask>(e =>
        {
            e.HasKey(x => x.Id);
            e.ToTable("Tasks");
            e.HasOne(x => x.Job).WithMany(j => j.Tasks).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Contact).WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Milestone>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.JobId, x.SortOrder });
            e.HasOne(x => x.Job).WithMany(j => j.Milestones).HasForeignKey(x => x.JobId);
        });

        modelBuilder.Entity<IntegrationConnection>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.Provider }).IsUnique();
        });
    }
}
