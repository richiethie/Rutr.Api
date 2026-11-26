using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Rutr.Api.Models.Validators;

namespace RutrApi.Tests;

[Collection("SerialApi")]
public class OnboardingTests : IClassFixture<RutrApiFactory>
{
    private readonly RutrApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private const string StrongPassword = "secure-pass1";

    public OnboardingTests(RutrApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Signup_creates_org_admin_and_returns_token()
    {
        var email = $"owner-{Guid.NewGuid():N}@acme.test";
        var signup = await _client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            email,
            password = StrongPassword,
            displayName = "Jamie Acme",
            organizationName = "Acme Roofing"
        });
        signup.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await signup.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
        body.GetProperty("role").GetString().Should().Be("Admin");
        body.GetProperty("organization").GetProperty("name").GetString().Should().Be("Acme Roofing");

        var dup = await _client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            email,
            password = StrongPassword,
            displayName = "Jamie Acme",
            organizationName = "Acme Roofing 2"
        });
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Signup_rejects_weak_passwords_with_clear_messages()
    {
        async Task<JsonElement> SignupWith(string password)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/auth/signup", new
            {
                email = $"weak-{Guid.NewGuid():N}@acme.test",
                password,
                displayName = "Weak User",
                organizationName = "Weak Org"
            });
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            return await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        }

        var tooShort = await SignupWith("Ab1!");
        PasswordErrors(tooShort).Should().Contain(StaffPasswordRules.MinLengthMessage);

        var noNumber = await SignupWith("abcdefg!");
        PasswordErrors(noNumber).Should().Contain(StaffPasswordRules.NumberMessage);

        var noLetter = await SignupWith("1234567!");
        PasswordErrors(noLetter).Should().Contain(StaffPasswordRules.LetterMessage);

        var noSpecial = await SignupWith("secure1pass");
        PasswordErrors(noSpecial).Should().Contain(StaffPasswordRules.SpecialMessage);
        PasswordErrors(noSpecial).Should().ContainSingle()
            .Which.Should().Be("Password must include at least one special character.");
    }

    [Fact]
    public async Task Accept_invite_rejects_weak_password()
    {
        var signup = await _client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            email = $"admin-{Guid.NewGuid():N}@acme.test",
            password = StrongPassword,
            displayName = "Owner",
            organizationName = $"Org {Guid.NewGuid():N}"[..20]
        });
        signup.EnsureSuccessStatusCode();
        var token = (await signup.Content.ReadFromJsonAsync<JsonElement>(JsonOptions)).GetProperty("token").GetString()!;

        using var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invite = await authed.PostAsJsonAsync("/api/v1/organizations/current/invites", new
        {
            email = $"sales-{Guid.NewGuid():N}@acme.test",
            displayName = "Sam Sales",
            role = "Sales"
        });
        invite.EnsureSuccessStatusCode();
        var inviteToken = (await invite.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))
            .GetProperty("inviteToken").GetString();

        var weak = await _client.PostAsJsonAsync("/api/v1/auth/accept-invite", new
        {
            inviteToken,
            password = "short1",
            displayName = "Sam Sales"
        });
        weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await weak.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        PasswordErrors(body).Should().Contain(StaffPasswordRules.MinLengthMessage);
    }

    [Fact]
    public async Task Demo_login_still_works_as_rutters_fallback()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "pm@rutters.demo",
            password = "rutters-demo"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        body.GetProperty("organizationName").GetString().Should().Contain("Rutters");
        body.GetProperty("brandPrimaryColor").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Signup_me_and_current_org_return_created_brand_not_rutters()
    {
        var email = $"brand-{Guid.NewGuid():N}@acme.test";
        const string brand = "#228B22";
        var signup = await _client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            email,
            password = StrongPassword,
            displayName = "Brand Owner",
            organizationName = "Greenfield Roofing",
            brandPrimaryColor = brand
        });
        signup.StatusCode.Should().Be(HttpStatusCode.Created);
        var signupBody = await signup.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var token = signupBody.GetProperty("token").GetString()!;
        var orgId = signupBody.GetProperty("organizationId").GetGuid();
        signupBody.GetProperty("organization").GetProperty("name").GetString().Should().Be("Greenfield Roofing");
        signupBody.GetProperty("organization").GetProperty("brandPrimaryColor").GetString().Should().Be(brand);
        orgId.Should().NotBe(Guid.Parse("11111111-1111-1111-1111-111111111111"));

        using var authed = _factory.CreateClient();
        // Leftover demo header must NOT override Bearer JWT (Web shell bug class).
        authed.DefaultRequestHeaders.Add("X-Demo-Auth", "pm");
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var me = await authed.GetAsync("/api/v1/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var meBody = await me.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        meBody.GetProperty("organizationId").GetGuid().Should().Be(orgId);
        meBody.GetProperty("organizationName").GetString().Should().Be("Greenfield Roofing");
        meBody.GetProperty("brandPrimaryColor").GetString().Should().Be(brand);
        meBody.GetProperty("email").GetString().Should().Be(email);

        var current = await authed.GetAsync("/api/v1/organizations/current");
        current.StatusCode.Should().Be(HttpStatusCode.OK);
        var orgBody = await current.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        orgBody.GetProperty("id").GetGuid().Should().Be(orgId);
        orgBody.GetProperty("name").GetString().Should().Be("Greenfield Roofing");
        orgBody.GetProperty("brandPrimaryColor").GetString().Should().Be(brand);
    }

    [Fact]
    public async Task Admin_can_add_member_invite_and_accept()
    {
        var ownerEmail = $"admin-{Guid.NewGuid():N}@acme.test";
        var signup = await _client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            email = ownerEmail,
            password = StrongPassword,
            displayName = "Owner",
            organizationName = $"Org {Guid.NewGuid():N}"[..20]
        });
        signup.EnsureSuccessStatusCode();
        var signupBody = await signup.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var token = signupBody.GetProperty("token").GetString()!;

        using var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var add = await authed.PostAsJsonAsync("/api/v1/organizations/current/members", new
        {
            email = $"pm-{Guid.NewGuid():N}@acme.test",
            displayName = "Alex PM",
            role = "Pm",
            temporaryPassword = "temp-pass-1"
        });
        add.StatusCode.Should().Be(HttpStatusCode.Created);

        var inviteEmail = $"sales-{Guid.NewGuid():N}@acme.test";
        var invite = await authed.PostAsJsonAsync("/api/v1/organizations/current/invites", new
        {
            email = inviteEmail,
            displayName = "Sam Sales",
            role = "Sales"
        });
        invite.StatusCode.Should().Be(HttpStatusCode.Created);
        var inviteBody = await invite.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var inviteToken = inviteBody.GetProperty("inviteToken").GetString();
        inviteToken.Should().StartWith("inv_");

        var listed = await authed.GetAsync("/api/v1/organizations/current/invites");
        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        var listedBody = await listed.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        listedBody.EnumerateArray().Any(x =>
            x.GetProperty("email").GetString() == inviteEmail
            && x.GetProperty("status").GetString() == "Pending").Should().BeTrue();

        var accept = await _client.PostAsJsonAsync("/api/v1/auth/accept-invite", new
        {
            inviteToken,
            password = StrongPassword,
            displayName = "Sam Sales"
        });
        accept.StatusCode.Should().Be(HttpStatusCode.Created);
        var acceptBody = await accept.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        acceptBody.GetProperty("role").GetString().Should().Be("Sales");
        acceptBody.GetProperty("token").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Seed_empty_and_jobnimbus_import_work()
    {
        var signup = await _client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            email = $"import-{Guid.NewGuid():N}@acme.test",
            password = StrongPassword,
            displayName = "Importer",
            organizationName = $"Import Org {Guid.NewGuid():N}"[..24]
        });
        signup.EnsureSuccessStatusCode();
        var signupBody = await signup.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var token = signupBody.GetProperty("token").GetString()!;

        using var authed = _factory.CreateClient();
        authed.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var empty = await authed.PostAsJsonAsync("/api/v1/onboarding/seed", new { mode = "empty" });
        empty.StatusCode.Should().Be(HttpStatusCode.OK);
        var emptyBody = await empty.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        emptyBody.GetProperty("jobsCreated").GetInt32().Should().Be(0);

        var import = await authed.PostAsJsonAsync("/api/v1/onboarding/import", new
        {
            source = "jobnimbus",
            csvContent = (string?)null
        });
        import.StatusCode.Should().Be(HttpStatusCode.OK);
        var importBody = await import.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        importBody.GetProperty("jobsCreated").GetInt32().Should().BeGreaterThan(0);
        importBody.GetProperty("contactsCreated").GetInt32().Should().BeGreaterThan(0);
        importBody.GetProperty("propertiesCreated").GetInt32().Should().BeGreaterThan(0);

        var csv = await authed.PostAsJsonAsync("/api/v1/onboarding/import", new
        {
            source = "csv",
            csvContent =
                "externalId,firstName,lastName,email,phone,addressLine1,city,state,postalCode,jobTitle,jobType,status,stage\n" +
                "csv-9001,Pat,Nguyen,pat.csv@example.com,+13195550999,12 Oak Ave,Waverly,IA,50677,CSV import job,Retail,InProgress,Build"
        });
        csv.StatusCode.Should().Be(HttpStatusCode.OK);
        var csvBody = await csv.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        csvBody.GetProperty("jobsCreated").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Swagger_json_is_healthy()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("/api/v1/auth/signup");
        json.Should().Contain("/api/v1/onboarding/import");
        json.Should().Contain("/api/v1/organizations/current/invites");
    }

    private static IEnumerable<string> PasswordErrors(JsonElement problem)
    {
        if (!problem.TryGetProperty("errors", out var errors))
        {
            yield break;
        }

        foreach (var prop in errors.EnumerateObject())
        {
            if (!prop.Name.Contains("password", StringComparison.OrdinalIgnoreCase)
                && !prop.Name.Contains("TemporaryPassword", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var msg in prop.Value.EnumerateArray())
            {
                var text = msg.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    yield return text!;
                }
            }
        }
    }
}
