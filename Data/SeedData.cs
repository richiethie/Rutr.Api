using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Data;

public static class SeedData
{
    public static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PmUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Job1Id = Guid.Parse("33333333-3333-3333-3333-333333333331");
    public static readonly Guid Job2Id = Guid.Parse("33333333-3333-3333-3333-333333333332");
    public static readonly Guid Job3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");
    public const string DemoPassword = "rutters-demo";

    public static async Task EnsureSeededAsync(RutrDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);
        if (await db.Organizations.AnyAsync(ct))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var org = new Organization
        {
            Id = OrgId,
            Name = "Rutters Roofing & Exteriors",
            Slug = "rutters",
            CreatedAt = now.AddDays(-120)
        };

        var pm = new StaffUser
        {
            Id = PmUserId,
            OrganizationId = OrgId,
            Email = "pm@rutters.demo",
            DisplayName = "Alex Rivera",
            Role = "Pm",
            PasswordHash = HashPassword(DemoPassword)
        };

        var jobs = new[]
        {
            new Job
            {
                Id = Job1Id,
                OrganizationId = OrgId,
                ExternalJobNimbusId = "jn-18402",
                AddressLine1 = "1428 Maple Ridge Dr",
                City = "Cedar Falls",
                State = "IA",
                PostalCode = "50613",
                HomeownerName = "Dana & Chris Keller",
                HomeownerEmail = "keller@example.com",
                HomeownerPhone = "+13195550142",
                Status = "InProgress",
                Stage = "Build",
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddHours(-3)
            },
            new Job
            {
                Id = Job2Id,
                OrganizationId = OrgId,
                ExternalJobNimbusId = "jn-18455",
                AddressLine1 = "88 Prairie View Ct",
                City = "Waterloo",
                State = "IA",
                PostalCode = "50701",
                HomeownerName = "Morgan Ellis",
                HomeownerEmail = "morgan.ellis@example.com",
                HomeownerPhone = "+13195550188",
                Status = "InProgress",
                Stage = "Prepare",
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddHours(-1)
            },
            new Job
            {
                Id = Job3Id,
                OrganizationId = OrgId,
                ExternalJobNimbusId = "jn-18310",
                AddressLine1 = "409 Oak Street",
                City = "Waverly",
                State = "IA",
                PostalCode = "50677",
                HomeownerName = "Pat Nguyen",
                HomeownerEmail = "pat.nguyen@example.com",
                HomeownerPhone = "+13195550109",
                Status = "Complete",
                Stage = "Complete",
                CreatedAt = now.AddDays(-45),
                UpdatedAt = now.AddDays(-2)
            }
        };

        var photos = new[]
        {
            new PhotoEvidence
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444441"),
                OrganizationId = OrgId,
                JobId = Job1Id,
                ExternalCompanyCamId = "cc-9001",
                Caption = "Tear-off complete — north slope clear",
                ImageUrl = "https://images.unsplash.com/photo-1632759145351-1d592919f540?w=1200&q=80",
                CustomerVisible = true,
                CapturedAt = now.AddHours(-5)
            },
            new PhotoEvidence
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444442"),
                OrganizationId = OrgId,
                JobId = Job1Id,
                ExternalCompanyCamId = "cc-9002",
                Caption = "Underlayment rolled — ridge line ready",
                ImageUrl = "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=1200&q=80",
                CustomerVisible = false,
                CapturedAt = now.AddHours(-3)
            },
            new PhotoEvidence
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444443"),
                OrganizationId = OrgId,
                JobId = Job2Id,
                ExternalCompanyCamId = "cc-9101",
                Caption = "Materials staged in driveway",
                ImageUrl = "https://images.unsplash.com/photo-1503387762-592deb58ef4e?w=1200&q=80",
                CustomerVisible = true,
                CapturedAt = now.AddHours(-2)
            },
            new PhotoEvidence
            {
                Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                OrganizationId = OrgId,
                JobId = Job3Id,
                ExternalCompanyCamId = "cc-8801",
                Caption = "Final walkthrough — south elevation",
                ImageUrl = "https://images.unsplash.com/photo-1560518883-ce09059eeffa?w=1200&q=80",
                CustomerVisible = true,
                CapturedAt = now.AddDays(-3)
            }
        };

        var drafts = new[]
        {
            new MessageDraft
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555551"),
                OrganizationId = OrgId,
                JobId = Job1Id,
                Channel = "Sms",
                Subject = "Progress update",
                Body = "Hi Dana & Chris — tear-off on the north slope is finished and underlayment is going down. We'll text again when shingles start. Track photos anytime: your portal link is ready.",
                Status = "PendingApproval",
                EvidenceSummary = "CompanyCam cc-9001 (tear-off), cc-9002 (underlayment)",
                CreatedAt = now.AddMinutes(-40)
            },
            new MessageDraft
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555552"),
                OrganizationId = OrgId,
                JobId = Job2Id,
                Channel = "Email",
                Subject = "Materials on site — install starts tomorrow",
                Body = "Morgan, your shingles and ridge vent are staged at 88 Prairie View Ct. Crew arrives tomorrow morning weather permitting. Open your portal for photos and the next milestone.",
                Status = "PendingApproval",
                EvidenceSummary = "CompanyCam cc-9101 (materials staged)",
                CreatedAt = now.AddMinutes(-25)
            },
            new MessageDraft
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555553"),
                OrganizationId = OrgId,
                JobId = Job3Id,
                Channel = "Sms",
                Subject = "Job complete",
                Body = "Pat — final walkthrough photos are in your portal. Thanks for trusting Rutters. Reply if you need warranty paperwork.",
                Status = "Approved",
                EvidenceSummary = "CompanyCam cc-8801 (final)",
                CreatedAt = now.AddDays(-2),
                ApprovedAt = now.AddDays(-2).AddHours(1),
                ApprovedByStaffId = PmUserId
            }
        };

        var portals = new[]
        {
            new PortalToken
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666661"),
                OrganizationId = OrgId,
                JobId = Job1Id,
                Token = "keller-maple-demo",
                ExpiresAt = now.AddDays(30),
                CreatedAt = now.AddDays(-1)
            },
            new PortalToken
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666662"),
                OrganizationId = OrgId,
                JobId = Job2Id,
                Token = "ellis-prairie-demo",
                ExpiresAt = now.AddDays(30),
                CreatedAt = now.AddHours(-6)
            },
            new PortalToken
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666663"),
                OrganizationId = OrgId,
                JobId = Job3Id,
                Token = "nguyen-oak-demo",
                ExpiresAt = now.AddDays(14),
                CreatedAt = now.AddDays(-3)
            }
        };

        db.Organizations.Add(org);
        db.StaffUsers.Add(pm);
        db.Jobs.AddRange(jobs);
        db.Photos.AddRange(photos);
        db.MessageDrafts.AddRange(drafts);
        db.PortalTokens.AddRange(portals);
        await db.SaveChangesAsync(ct);
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"rutr|{password}"));
        return Convert.ToHexString(bytes);
    }
}
