using Microsoft.EntityFrameworkCore;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Data;

public class RutrDbContext(DbContextOptions<RutrDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<StaffUser> StaffUsers => Set<StaffUser>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<PhotoEvidence> Photos => Set<PhotoEvidence>();
    public DbSet<MessageDraft> MessageDrafts => Set<MessageDraft>();
    public DbSet<PortalToken> PortalTokens => Set<PortalToken>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

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
            e.HasIndex(x => new { x.OrganizationId, x.Email }).IsUnique();
            e.HasOne(x => x.Organization).WithMany(o => o.StaffUsers).HasForeignKey(x => x.OrganizationId);
        });

        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.OrganizationId, x.ExternalJobNimbusId });
            e.HasOne(x => x.Organization).WithMany(o => o.Jobs).HasForeignKey(x => x.OrganizationId);
        });

        modelBuilder.Entity<PhotoEvidence>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasOne(x => x.Job).WithMany(j => j.Photos).HasForeignKey(x => x.JobId);
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
    }
}
