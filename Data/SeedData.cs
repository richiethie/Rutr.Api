using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Rutr.Api.Models.Entities;

namespace Rutr.Api.Data;

public static class SeedData
{
    public static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid PmUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid AdminUserId = Guid.Parse("22222222-2222-2222-2222-222222222221");
    public static readonly Guid SalesUserId = Guid.Parse("22222222-2222-2222-2222-222222222223");
    public static readonly Guid ContactKellerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
    public static readonly Guid ContactChrisId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
    public static readonly Guid ContactEllisId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3");
    public static readonly Guid ContactNguyenId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4");
    public static readonly Guid PropMapleId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
    public static readonly Guid PropPrairieId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");
    public static readonly Guid PropOakId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3");
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
            BrandPrimaryColor = "#B87333",
            QuietHoursStartLocal = 8,
            QuietHoursEndLocal = 20,
            TimeZoneId = "Central Standard Time",
            CreatedAt = now.AddDays(-120)
        };

        var staff = new[]
        {
            new StaffUser
            {
                Id = AdminUserId,
                OrganizationId = OrgId,
                Email = "admin@rutters.demo",
                DisplayName = "Jamie Rutters",
                Role = "Admin",
                PasswordHash = HashPassword(DemoPassword)
            },
            new StaffUser
            {
                Id = PmUserId,
                OrganizationId = OrgId,
                Email = "pm@rutters.demo",
                DisplayName = "Alex Rivera",
                Role = "Pm",
                PasswordHash = HashPassword(DemoPassword)
            },
            new StaffUser
            {
                Id = SalesUserId,
                OrganizationId = OrgId,
                Email = "sales@rutters.demo",
                DisplayName = "Sam Ortiz",
                Role = "Sales",
                PasswordHash = HashPassword(DemoPassword)
            }
        };

        var contacts = new[]
        {
            new Contact
            {
                Id = ContactKellerId, OrganizationId = OrgId, FirstName = "Dana", LastName = "Keller",
                Email = "keller@example.com", Phone = "+13195550142", Role = "Homeowner",
                PreferredChannel = "Sms", Language = "en", CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-1)
            },
            new Contact
            {
                Id = ContactChrisId, OrganizationId = OrgId, FirstName = "Chris", LastName = "Keller",
                Email = "chris.keller@example.com", Phone = "+13195550143", Role = "Spouse",
                PreferredChannel = "Email", Language = "en", CreatedAt = now.AddDays(-20), UpdatedAt = now.AddDays(-1)
            },
            new Contact
            {
                Id = ContactEllisId, OrganizationId = OrgId, FirstName = "Morgan", LastName = "Ellis",
                Email = "morgan.ellis@example.com", Phone = "+13195550188", Role = "Homeowner",
                PreferredChannel = "Email", Language = "en", CreatedAt = now.AddDays(-10), UpdatedAt = now.AddHours(-6)
            },
            new Contact
            {
                Id = ContactNguyenId, OrganizationId = OrgId, FirstName = "Pat", LastName = "Nguyen",
                Email = "pat.nguyen@example.com", Phone = "+13195550109", Role = "Homeowner",
                PreferredChannel = "Sms", Language = "en", CreatedAt = now.AddDays(-50), UpdatedAt = now.AddDays(-3)
            }
        };

        var properties = new[]
        {
            new Property
            {
                Id = PropMapleId, OrganizationId = OrgId, AddressLine1 = "1428 Maple Ridge Dr",
                City = "Cedar Falls", State = "IA", PostalCode = "50613", PropertyType = "Residential",
                YearBuilt = 1998, Notes = "Two-story, architectural shingles", CreatedAt = now.AddDays(-30), UpdatedAt = now.AddDays(-2)
            },
            new Property
            {
                Id = PropPrairieId, OrganizationId = OrgId, AddressLine1 = "88 Prairie View Ct",
                City = "Waterloo", State = "IA", PostalCode = "50701", PropertyType = "Residential",
                YearBuilt = 2007, Notes = "Ranch, steep north slope", CreatedAt = now.AddDays(-14), UpdatedAt = now.AddDays(-1)
            },
            new Property
            {
                Id = PropOakId, OrganizationId = OrgId, AddressLine1 = "409 Oak Street",
                City = "Waverly", State = "IA", PostalCode = "50677", PropertyType = "Residential",
                YearBuilt = 1975, Notes = "Insurance rebuild complete", CreatedAt = now.AddDays(-60), UpdatedAt = now.AddDays(-5)
            }
        };

        var jobs = new[]
        {
            new Job
            {
                Id = Job1Id, OrganizationId = OrgId, PropertyId = PropMapleId, PrimaryContactId = ContactKellerId,
                ExternalJobNimbusId = "jn-18402", Title = "Keller — Maple Ridge reroof", JobType = "Retail",
                AddressLine1 = "1428 Maple Ridge Dr", City = "Cedar Falls", State = "IA", PostalCode = "50613",
                HomeownerName = "Dana & Chris Keller", HomeownerEmail = "keller@example.com", HomeownerPhone = "+13195550142",
                Status = "InProgress", Stage = "Build", CreatedAt = now.AddDays(-14), UpdatedAt = now.AddHours(-3)
            },
            new Job
            {
                Id = Job2Id, OrganizationId = OrgId, PropertyId = PropPrairieId, PrimaryContactId = ContactEllisId,
                ExternalJobNimbusId = "jn-18455", Title = "Ellis — Prairie View install", JobType = "Retail",
                AddressLine1 = "88 Prairie View Ct", City = "Waterloo", State = "IA", PostalCode = "50701",
                HomeownerName = "Morgan Ellis", HomeownerEmail = "morgan.ellis@example.com", HomeownerPhone = "+13195550188",
                Status = "InProgress", Stage = "Prepare", CreatedAt = now.AddDays(-7), UpdatedAt = now.AddHours(-1)
            },
            new Job
            {
                Id = Job3Id, OrganizationId = OrgId, PropertyId = PropOakId, PrimaryContactId = ContactNguyenId,
                ExternalJobNimbusId = "jn-18310", Title = "Nguyen — Oak Street insurance", JobType = "Insurance",
                AddressLine1 = "409 Oak Street", City = "Waverly", State = "IA", PostalCode = "50677",
                HomeownerName = "Pat Nguyen", HomeownerEmail = "pat.nguyen@example.com", HomeownerPhone = "+13195550109",
                Status = "Complete", Stage = "Complete", CreatedAt = now.AddDays(-45), UpdatedAt = now.AddDays(-2)
            }
        };

        var photos = new[]
        {
            Photo(Guid.Parse("44444444-4444-4444-4444-444444444441"), Job1Id, "cc-9001", "Tear-off complete — north slope clear",
                "https://images.unsplash.com/photo-1632759145351-1d592919f540?w=1200&q=80", true, now.AddHours(-5)),
            Photo(Guid.Parse("44444444-4444-4444-4444-444444444442"), Job1Id, "cc-9002", "Underlayment rolled — ridge line ready",
                "https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=1200&q=80", false, now.AddHours(-3)),
            Photo(Guid.Parse("44444444-4444-4444-4444-444444444443"), Job2Id, "cc-9101", "Materials staged in driveway",
                "https://images.unsplash.com/photo-1503387762-592deb58ef4e?w=1200&q=80", true, now.AddHours(-2)),
            Photo(Guid.Parse("44444444-4444-4444-4444-444444444444"), Job3Id, "cc-8801", "Final walkthrough — south elevation",
                "https://images.unsplash.com/photo-1560518883-ce09059eeffa?w=1200&q=80", true, now.AddDays(-3))
        };

        var drafts = new[]
        {
            new MessageDraft
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555551"), OrganizationId = OrgId, JobId = Job1Id,
                Channel = "Sms", Subject = "Progress update",
                Body = "Hi Dana & Chris — tear-off on the north slope is finished and underlayment is going down. We'll text again when shingles start. Track photos anytime: your portal link is ready.",
                Status = "PendingApproval", EvidenceSummary = "CompanyCam cc-9001 (tear-off), cc-9002 (underlayment)",
                CreatedAt = now.AddMinutes(-40)
            },
            new MessageDraft
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555552"), OrganizationId = OrgId, JobId = Job2Id,
                Channel = "Email", Subject = "Materials on site — install starts tomorrow",
                Body = "Morgan, your shingles and ridge vent are staged at 88 Prairie View Ct. Crew arrives tomorrow morning weather permitting. Open your portal for photos and the next milestone.",
                Status = "PendingApproval", EvidenceSummary = "CompanyCam cc-9101 (materials staged)",
                CreatedAt = now.AddMinutes(-25)
            },
            new MessageDraft
            {
                Id = Guid.Parse("55555555-5555-5555-5555-555555555553"), OrganizationId = OrgId, JobId = Job3Id,
                Channel = "Sms", Subject = "Job complete",
                Body = "Pat — final walkthrough photos are in your portal. Thanks for trusting Rutters. Reply if you need warranty paperwork.",
                Status = "Approved", EvidenceSummary = "CompanyCam cc-8801 (final)",
                CreatedAt = now.AddDays(-2), ApprovedAt = now.AddDays(-2).AddHours(1), ApprovedByStaffId = PmUserId
            }
        };

        var portals = new[]
        {
            new PortalToken
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666661"), OrganizationId = OrgId, JobId = Job1Id,
                Token = "keller-maple-demo", ExpiresAt = now.AddDays(30), CreatedAt = now.AddDays(-1)
            },
            new PortalToken
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666662"), OrganizationId = OrgId, JobId = Job2Id,
                Token = "ellis-prairie-demo", ExpiresAt = now.AddDays(30), CreatedAt = now.AddHours(-6)
            },
            new PortalToken
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666663"), OrganizationId = OrgId, JobId = Job3Id,
                Token = "nguyen-oak-demo", ExpiresAt = now.AddDays(14), CreatedAt = now.AddDays(-3)
            }
        };

        var consents = new[]
        {
            Consent(ContactKellerId, Job1Id, true, true, false, "portal_signup", now),
            Consent(ContactChrisId, Job1Id, false, true, false, "spouse_email", now),
            Consent(ContactEllisId, Job2Id, true, true, false, "estimate_form", now),
            Consent(ContactNguyenId, Job3Id, true, true, false, "insurance_intake", now)
        };

        var notes = new[]
        {
            new Note
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777771"), OrganizationId = OrgId, JobId = Job1Id,
                ContactId = ContactKellerId, Body = "Homeowner prefers morning SMS updates. Dog in backyard — use side gate.",
                CreatedByStaffId = PmUserId, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddDays(-3)
            },
            new Note
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777772"), OrganizationId = OrgId, JobId = Job2Id,
                Body = "HOA paint color approved: Driftwood.", CreatedByStaffId = SalesUserId,
                CreatedAt = now.AddDays(-2), UpdatedAt = now.AddDays(-2)
            },
            new Note
            {
                Id = Guid.Parse("77777777-7777-7777-7777-777777777773"), OrganizationId = OrgId, ContactId = ContactEllisId,
                Body = "Follow up on financing quote if install slips past Friday.", CreatedByStaffId = SalesUserId,
                CreatedAt = now.AddHours(-12), UpdatedAt = now.AddHours(-12)
            }
        };

        var tasks = new[]
        {
            new JobTask
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888881"), OrganizationId = OrgId, JobId = Job1Id,
                Title = "Order ridge vent", Description = "Confirm stock at SRS before Thursday", Status = "Open",
                DueAt = now.AddDays(1), AssignedStaffId = PmUserId, CreatedAt = now.AddDays(-1), UpdatedAt = now.AddDays(-1)
            },
            new JobTask
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888882"), OrganizationId = OrgId, JobId = Job2Id,
                Title = "Confirm dumpster drop", Description = "", Status = "Done",
                DueAt = now.AddDays(-1), AssignedStaffId = PmUserId, CreatedAt = now.AddDays(-3), UpdatedAt = now.AddHours(-20)
            },
            new JobTask
            {
                Id = Guid.Parse("88888888-8888-8888-8888-888888888883"), OrganizationId = OrgId, ContactId = ContactKellerId,
                Title = "Send warranty packet", Description = "After final payment", Status = "Open",
                DueAt = now.AddDays(10), AssignedStaffId = AdminUserId, CreatedAt = now.AddHours(-4), UpdatedAt = now.AddHours(-4)
            }
        };

        var milestones = new[]
        {
            Ms(Guid.Parse("99999999-9999-9999-9999-999999999991"), Job1Id, "Contract signed", "Complete", 1, now.AddDays(-12), now.AddDays(-12)),
            Ms(Guid.Parse("99999999-9999-9999-9999-999999999992"), Job1Id, "Tear-off", "Complete", 2, now.AddDays(-1), now.AddHours(-6)),
            Ms(Guid.Parse("99999999-9999-9999-9999-999999999993"), Job1Id, "Dry-in", "InProgress", 3, now.AddDays(0), null),
            Ms(Guid.Parse("99999999-9999-9999-9999-999999999994"), Job1Id, "Final walkthrough", "Pending", 4, now.AddDays(3), null),
            Ms(Guid.Parse("99999999-9999-9999-9999-999999999995"), Job2Id, "Materials staged", "Complete", 1, now.AddHours(-4), now.AddHours(-3)),
            Ms(Guid.Parse("99999999-9999-9999-9999-999999999996"), Job2Id, "Install start", "Pending", 2, now.AddDays(1), null),
            Ms(Guid.Parse("99999999-9999-9999-9999-999999999997"), Job3Id, "Final walkthrough", "Complete", 1, now.AddDays(-4), now.AddDays(-3)),
            Ms(Guid.Parse("99999999-9999-9999-9999-999999999998"), Job3Id, "Close-out docs", "Complete", 2, now.AddDays(-2), now.AddDays(-2))
        };

        var integrations = new[]
        {
            new IntegrationConnection
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc1"), OrganizationId = OrgId,
                Provider = "JobNimbus", DisplayName = "JobNimbus", IsConnected = true,
                ConnectedAt = now.AddDays(-40), UpdatedAt = now.AddDays(-1)
            },
            new IntegrationConnection
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc2"), OrganizationId = OrgId,
                Provider = "CompanyCam", DisplayName = "CompanyCam", IsConnected = true,
                ConnectedAt = now.AddDays(-35), UpdatedAt = now.AddDays(-1)
            },
            new IntegrationConnection
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc3"), OrganizationId = OrgId,
                Provider = "Twilio", DisplayName = "Twilio SMS", IsConnected = false,
                ConnectedAt = null, UpdatedAt = now.AddDays(-10)
            },
            new IntegrationConnection
            {
                Id = Guid.Parse("cccccccc-cccc-cccc-cccc-ccccccccccc4"), OrganizationId = OrgId,
                Provider = "Resend", DisplayName = "Resend Email", IsConnected = false,
                ConnectedAt = null, UpdatedAt = now.AddDays(-10)
            }
        };

        db.Organizations.Add(org);
        db.StaffUsers.AddRange(staff);
        db.Contacts.AddRange(contacts);
        db.Properties.AddRange(properties);
        db.Jobs.AddRange(jobs);
        db.Photos.AddRange(photos);
        db.MessageDrafts.AddRange(drafts);
        db.PortalTokens.AddRange(portals);
        db.HomeownerConsents.AddRange(consents);
        db.Notes.AddRange(notes);
        db.Tasks.AddRange(tasks);
        db.Milestones.AddRange(milestones);
        db.IntegrationConnections.AddRange(integrations);
        await db.SaveChangesAsync(ct);
    }

    private static PhotoEvidence Photo(Guid id, Guid jobId, string ext, string caption, string url, bool visible, DateTimeOffset at) =>
        new()
        {
            Id = id, OrganizationId = OrgId, JobId = jobId, ExternalCompanyCamId = ext,
            Caption = caption, ImageUrl = url, CustomerVisible = visible, CapturedAt = at
        };

    private static HomeownerConsent Consent(Guid contactId, Guid jobId, bool sms, bool email, bool stopped, string source, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(), OrganizationId = OrgId, ContactId = contactId, JobId = jobId,
            SmsOptIn = sms, EmailOptIn = email, SmsStopped = stopped, Source = source,
            DisclosureText = "Demo TCPA disclosure — transactional project updates only.",
            UpdatedAt = now
        };

    private static Milestone Ms(Guid id, Guid jobId, string name, string status, int order, DateTimeOffset? due, DateTimeOffset? done) =>
        new()
        {
            Id = id, OrganizationId = OrgId, JobId = jobId, Name = name, Status = status, SortOrder = order,
            DueAt = due, CompletedAt = done, CreatedAt = due ?? DateTimeOffset.UtcNow, UpdatedAt = done ?? due ?? DateTimeOffset.UtcNow
        };

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"rutr|{password}"));
        return Convert.ToHexString(bytes);
    }
}
