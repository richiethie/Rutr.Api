using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace RutrApi.Tests;

[Collection("SerialApi")]
public class OnboardingTests : IClassFixture<RutrApiFactory>
{
    private readonly RutrApiFactory _factory;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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
            password = "secure-pass",
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
            password = "secure-pass",
            displayName = "Jamie Acme",
            organizationName = "Acme Roofing 2"
        });
        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
    }

    [Fact]
    public async Task Admin_can_add_member_invite_and_accept()
    {
        var ownerEmail = $"admin-{Guid.NewGuid():N}@acme.test";
        var signup = await _client.PostAsJsonAsync("/api/v1/auth/signup", new
        {
            email = ownerEmail,
            password = "secure-pass",
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
            password = "secure-pass",
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
            password = "secure-pass",
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
}
