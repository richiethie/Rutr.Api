using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Rutr.Api.Authentication;
using Rutr.Api.Data;
using Rutr.Api.Models.Dtos;
using Rutr.Api.Models.Entities;
using Rutr.Api.Services.JobNimbus;

namespace Rutr.Api.Services;

public class OnboardingService(
    RutrDbContext db,
    JwtTokenService tokens,
    MockJobNimbusClient jobNimbus,
    ILogger<OnboardingService> logger)
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "Pm", "Sales"
    };

    public async Task<SignupResponse> SignupAsync(SignupRequest request, CancellationToken ct = default)
    {
        var email = NormalizeEmail(request.Email);
        if (await db.StaffUsers.AnyAsync(u => u.Email == email, ct))
        {
            throw new InvalidOperationException("Email already registered.");
        }

        var orgName = request.OrganizationName.Trim();
        var slug = await UniqueSlugAsync(Slugify(orgName), ct);
        var now = DateTimeOffset.UtcNow;
        var org = new Organization
        {
            Id = Guid.NewGuid(),
            Name = orgName,
            Slug = slug,
            BrandPrimaryColor = string.IsNullOrWhiteSpace(request.BrandPrimaryColor)
                ? "#B87333"
                : request.BrandPrimaryColor.Trim(),
            QuietHoursStartLocal = request.QuietHoursStartLocal ?? 8,
            QuietHoursEndLocal = request.QuietHoursEndLocal ?? 20,
            TimeZoneId = string.IsNullOrWhiteSpace(request.TimeZoneId)
                ? "Central Standard Time"
                : request.TimeZoneId.Trim(),
            CreatedAt = now
        };

        var user = new StaffUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = org.Id,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            Role = "Admin",
            PasswordHash = SeedData.HashPassword(request.Password)
        };

        db.Organizations.Add(org);
        db.StaffUsers.Add(user);
        await db.SaveChangesAsync(ct);

        return ToSignupResponse(user, org);
    }

    public async Task<StaffMemberDto> AddMemberAsync(
        Guid orgId,
        string actorRole,
        AddStaffMemberRequest request,
        CancellationToken ct = default)
    {
        EnsureAdmin(actorRole);
        var role = NormalizeRole(request.Role);
        var email = NormalizeEmail(request.Email);
        if (await db.StaffUsers.AnyAsync(u => u.Email == email, ct))
        {
            throw new InvalidOperationException("Email already registered.");
        }

        var user = new StaffUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            Role = role,
            PasswordHash = SeedData.HashPassword(request.TemporaryPassword)
        };
        db.StaffUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return new StaffMemberDto(user.Id, user.Email, user.DisplayName, user.Role, user.OrganizationId);
    }

    public async Task<InviteDto> InviteAsync(
        Guid orgId,
        string actorRole,
        InviteStaffRequest request,
        CancellationToken ct = default)
    {
        EnsureAdmin(actorRole);
        var role = NormalizeRole(request.Role);
        var email = NormalizeEmail(request.Email);
        if (await db.StaffUsers.AnyAsync(u => u.Email == email, ct))
        {
            throw new InvalidOperationException("Email already registered.");
        }

        var pending = await db.StaffInvites
            .FirstOrDefaultAsync(i => i.OrganizationId == orgId && i.Email == email && i.Status == "Pending", ct);
        if (pending is not null)
        {
            pending.Role = role;
            if (!string.IsNullOrWhiteSpace(request.DisplayName))
            {
                pending.DisplayName = request.DisplayName.Trim();
            }

            pending.ExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
            await db.SaveChangesAsync(ct);
            return MapInvite(pending);
        }

        var invite = new StaffInvite
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            Email = email,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? email.Split('@')[0]
                : request.DisplayName.Trim(),
            Role = role,
            InviteToken = $"inv_{Guid.NewGuid():N}",
            Status = "Pending",
            CreatedAt = DateTimeOffset.UtcNow,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };
        db.StaffInvites.Add(invite);
        await db.SaveChangesAsync(ct);
        return MapInvite(invite);
    }

    public async Task<IReadOnlyList<InviteDto>> ListInvitesAsync(Guid orgId, string actorRole, CancellationToken ct = default)
    {
        EnsureAdmin(actorRole);
        // Order in-memory — SQLite cannot ORDER BY DateTimeOffset in SQL.
        var rows = await db.StaffInvites.AsNoTracking()
            .Where(i => i.OrganizationId == orgId)
            .ToListAsync(ct);
        return rows
            .OrderByDescending(i => i.CreatedAt)
            .Select(MapInvite)
            .ToList();
    }

    public async Task<SignupResponse> AcceptInviteAsync(AcceptInviteRequest request, CancellationToken ct = default)
    {
        var invite = await db.StaffInvites
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.InviteToken == request.InviteToken.Trim(), ct);

        if (invite is null || invite.Status != "Pending")
        {
            throw new InvalidOperationException("Invite not found or already used.");
        }

        if (invite.ExpiresAt < DateTimeOffset.UtcNow)
        {
            invite.Status = "Expired";
            await db.SaveChangesAsync(ct);
            throw new InvalidOperationException("Invite has expired.");
        }

        if (await db.StaffUsers.AnyAsync(u => u.Email == invite.Email, ct))
        {
            throw new InvalidOperationException("Email already registered.");
        }

        var displayName = string.IsNullOrWhiteSpace(request.DisplayName)
            ? invite.DisplayName
            : request.DisplayName.Trim();

        var user = new StaffUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = invite.OrganizationId,
            Email = invite.Email,
            DisplayName = displayName,
            Role = invite.Role,
            PasswordHash = SeedData.HashPassword(request.Password)
        };
        invite.Status = "Accepted";
        invite.AcceptedAt = DateTimeOffset.UtcNow;
        db.StaffUsers.Add(user);
        await db.SaveChangesAsync(ct);
        return ToSignupResponse(user, invite.Organization);
    }

    public async Task<OnboardingSeedResult> SeedAsync(Guid orgId, OnboardingSeedRequest request, CancellationToken ct = default)
    {
        var mode = (request.Mode ?? "empty").Trim().ToLowerInvariant();
        if (mode is not ("empty" or "sample"))
        {
            throw new InvalidOperationException("Mode must be empty or sample.");
        }

        if (mode == "empty")
        {
            return new OnboardingSeedResult("empty", 0, 0, 0, "Organization left empty.");
        }

        var now = DateTimeOffset.UtcNow;
        var contact = new Contact
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            FirstName = "Sample",
            LastName = "Homeowner",
            Email = $"sample+{orgId:N}@rutr.local".ToLowerInvariant(),
            Phone = "+13195550000",
            Role = "Homeowner",
            PreferredChannel = "Sms",
            Language = "en",
            CreatedAt = now,
            UpdatedAt = now
        };
        var property = new Property
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            AddressLine1 = "100 Sample Street",
            City = "Cedar Falls",
            State = "IA",
            PostalCode = "50613",
            PropertyType = "Residential",
            YearBuilt = 2000,
            Notes = "Sample property from onboarding seed",
            CreatedAt = now,
            UpdatedAt = now
        };
        var job = new Job
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            PropertyId = property.Id,
            PrimaryContactId = contact.Id,
            ExternalJobNimbusId = $"sample-{Guid.NewGuid():N}"[..16],
            Title = "Sample — first job",
            JobType = "Retail",
            AddressLine1 = property.AddressLine1,
            City = property.City,
            State = property.State,
            PostalCode = property.PostalCode,
            HomeownerName = $"{contact.FirstName} {contact.LastName}",
            HomeownerEmail = contact.Email,
            HomeownerPhone = contact.Phone,
            Status = "InProgress",
            Stage = "Inspect",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Contacts.Add(contact);
        db.Properties.Add(property);
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);
        return new OnboardingSeedResult("sample", 1, 1, 1, "Seeded sample contact, property, and job.");
    }

    public async Task<OnboardingImportResult> ImportAsync(Guid orgId, OnboardingImportRequest request, CancellationToken ct = default)
    {
        var source = (request.Source ?? "").Trim().ToLowerInvariant();
        return source switch
        {
            "jobnimbus" => await ImportJobNimbusAsync(orgId, ct),
            "csv" => await ImportCsvAsync(orgId, request.CsvContent, ct),
            _ => throw new InvalidOperationException("Source must be jobnimbus or csv.")
        };
    }

    private async Task<OnboardingImportResult> ImportJobNimbusAsync(Guid orgId, CancellationToken ct)
    {
        var payloads = await jobNimbus.FetchJobsAsync(orgId, ct);
        var warnings = new List<string>();
        var contacts = 0;
        var properties = 0;
        var jobs = 0;
        var skipped = 0;
        var now = DateTimeOffset.UtcNow;

        foreach (var payload in payloads)
        {
            if (await db.Jobs.AnyAsync(
                    j => j.OrganizationId == orgId && j.ExternalJobNimbusId == payload.ExternalId, ct))
            {
                skipped++;
                continue;
            }

            var (first, last) = SplitName(payload.HomeownerName);
            var contact = await db.Contacts.FirstOrDefaultAsync(
                c => c.OrganizationId == orgId && c.Email == payload.HomeownerEmail.ToLowerInvariant(), ct);
            if (contact is null)
            {
                contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    FirstName = first,
                    LastName = last,
                    Email = payload.HomeownerEmail.Trim().ToLowerInvariant(),
                    Phone = payload.HomeownerPhone,
                    Role = "Homeowner",
                    PreferredChannel = "Sms",
                    Language = "en",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Contacts.Add(contact);
                contacts++;
            }

            var property = await db.Properties.FirstOrDefaultAsync(
                p => p.OrganizationId == orgId
                     && p.AddressLine1 == payload.AddressLine1
                     && p.PostalCode == payload.PostalCode, ct);
            if (property is null)
            {
                property = new Property
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    AddressLine1 = payload.AddressLine1,
                    City = payload.City,
                    State = payload.State,
                    PostalCode = payload.PostalCode,
                    PropertyType = "Residential",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Properties.Add(property);
                properties++;
            }

            db.Jobs.Add(new Job
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                PropertyId = property.Id,
                PrimaryContactId = contact.Id,
                ExternalJobNimbusId = payload.ExternalId,
                Title = $"{payload.HomeownerName} — {payload.AddressLine1}",
                JobType = "Retail",
                AddressLine1 = payload.AddressLine1,
                City = payload.City,
                State = payload.State,
                PostalCode = payload.PostalCode,
                HomeownerName = payload.HomeownerName,
                HomeownerEmail = payload.HomeownerEmail,
                HomeownerPhone = payload.HomeownerPhone,
                Status = payload.Status,
                Stage = payload.Stage,
                CreatedAt = now,
                UpdatedAt = now
            });
            jobs++;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation(
            "Onboarding JN import for org {OrgId}: contacts={Contacts} properties={Properties} jobs={Jobs} skipped={Skipped}",
            orgId, contacts, properties, jobs, skipped);

        return new OnboardingImportResult(
            "jobnimbus",
            contacts,
            properties,
            jobs,
            skipped,
            warnings,
            "Imported mock JobNimbus snapshot.");
    }

    private async Task<OnboardingImportResult> ImportCsvAsync(Guid orgId, string? csvContent, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(csvContent))
        {
            throw new InvalidOperationException("csvContent is required when source is csv.");
        }

        var lines = csvContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2)
        {
            throw new InvalidOperationException("CSV must include a header row and at least one data row.");
        }

        var header = ParseCsvLine(lines[0]).Select(h => h.Trim().ToLowerInvariant()).ToArray();
        string Col(string[] row, string name)
        {
            var idx = Array.IndexOf(header, name.ToLowerInvariant());
            return idx >= 0 && idx < row.Length ? row[idx].Trim() : "";
        }

        var warnings = new List<string>();
        var contacts = 0;
        var properties = 0;
        var jobs = 0;
        var skipped = 0;
        var now = DateTimeOffset.UtcNow;

        for (var i = 1; i < lines.Length; i++)
        {
            var row = ParseCsvLine(lines[i]);
            if (row.Count == 0 || row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var externalId = Col(row.ToArray(), "externalId");
            if (string.IsNullOrWhiteSpace(externalId))
            {
                externalId = $"csv-{Guid.NewGuid():N}"[..16];
            }

            if (await db.Jobs.AnyAsync(j => j.OrganizationId == orgId && j.ExternalJobNimbusId == externalId, ct))
            {
                skipped++;
                continue;
            }

            var firstName = Col(row.ToArray(), "firstName");
            var lastName = Col(row.ToArray(), "lastName");
            var email = Col(row.ToArray(), "email").ToLowerInvariant();
            var phone = Col(row.ToArray(), "phone");
            var address = Col(row.ToArray(), "addressLine1");
            var city = Col(row.ToArray(), "city");
            var state = Col(row.ToArray(), "state");
            var postal = Col(row.ToArray(), "postalCode");
            var jobTitle = Col(row.ToArray(), "jobTitle");
            var jobType = Col(row.ToArray(), "jobType");
            var status = Col(row.ToArray(), "status");
            var stage = Col(row.ToArray(), "stage");

            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(email))
            {
                warnings.Add($"Row {i + 1}: skipped — email and addressLine1 are required.");
                skipped++;
                continue;
            }

            var contact = await db.Contacts.FirstOrDefaultAsync(
                c => c.OrganizationId == orgId && c.Email == email, ct);
            if (contact is null)
            {
                contact = new Contact
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    FirstName = string.IsNullOrWhiteSpace(firstName) ? "Imported" : firstName,
                    LastName = string.IsNullOrWhiteSpace(lastName) ? "Contact" : lastName,
                    Email = email,
                    Phone = phone,
                    Role = "Homeowner",
                    PreferredChannel = "Sms",
                    Language = "en",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Contacts.Add(contact);
                contacts++;
            }

            var property = await db.Properties.FirstOrDefaultAsync(
                p => p.OrganizationId == orgId && p.AddressLine1 == address && p.PostalCode == postal, ct);
            if (property is null)
            {
                property = new Property
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = orgId,
                    AddressLine1 = address,
                    City = city,
                    State = state,
                    PostalCode = postal,
                    PropertyType = "Residential",
                    CreatedAt = now,
                    UpdatedAt = now
                };
                db.Properties.Add(property);
                properties++;
            }

            var homeownerName = $"{contact.FirstName} {contact.LastName}".Trim();
            db.Jobs.Add(new Job
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                PropertyId = property.Id,
                PrimaryContactId = contact.Id,
                ExternalJobNimbusId = externalId,
                Title = string.IsNullOrWhiteSpace(jobTitle) ? $"{homeownerName} — {address}" : jobTitle,
                JobType = string.IsNullOrWhiteSpace(jobType) ? "Retail" : jobType,
                AddressLine1 = address,
                City = city,
                State = state,
                PostalCode = postal,
                HomeownerName = homeownerName,
                HomeownerEmail = email,
                HomeownerPhone = phone,
                Status = string.IsNullOrWhiteSpace(status) ? "InProgress" : status,
                Stage = string.IsNullOrWhiteSpace(stage) ? "Inspect" : stage,
                CreatedAt = now,
                UpdatedAt = now
            });
            jobs++;
        }

        await db.SaveChangesAsync(ct);
        return new OnboardingImportResult(
            "csv",
            contacts,
            properties,
            jobs,
            skipped,
            warnings,
            "Imported CSV migration snapshot.");
    }

    private SignupResponse ToSignupResponse(StaffUser user, Organization org) =>
        new(
            tokens.CreateToken(user),
            user.Id,
            user.DisplayName,
            user.Role,
            org.Id,
            new OrganizationDto(
                org.Id,
                org.Name,
                org.Slug,
                org.BrandPrimaryColor,
                org.QuietHoursStartLocal,
                org.QuietHoursEndLocal,
                org.TimeZoneId));

    private async Task<string> UniqueSlugAsync(string baseSlug, CancellationToken ct)
    {
        var slug = baseSlug;
        var n = 2;
        while (await db.Organizations.AnyAsync(o => o.Slug == slug, ct))
        {
            slug = $"{baseSlug}-{n}";
            n++;
        }

        return slug;
    }

    private static InviteDto MapInvite(StaffInvite i) =>
        new(i.Id, i.Email, i.DisplayName, i.Role, i.Status, i.InviteToken, i.ExpiresAt, i.CreatedAt);

    private static void EnsureAdmin(string? role)
    {
        if (!string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Admin role required.");
        }
    }

    private static string NormalizeRole(string role)
    {
        var trimmed = role.Trim();
        var match = AllowedRoles.FirstOrDefault(r => r.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            throw new InvalidOperationException("Role must be Admin, Pm, or Sales.");
        }

        return match;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static string Slugify(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"[^a-z0-9]+", "-");
        normalized = normalized.Trim('-');
        return string.IsNullOrWhiteSpace(normalized) ? "org" : normalized[..Math.Min(normalized.Length, 48)];
    }

    private static (string First, string Last) SplitName(string full)
    {
        var parts = full.Split([' ', '&'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return ("Imported", "Contact");
        if (parts.Length == 1) return (parts[0], "Contact");
        return (parts[0], parts[^1]);
    }

    private static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        result.Add(sb.ToString());
        return result;
    }
}
