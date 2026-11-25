using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RutrApi.Tests;

public class ApiSmokeTests : IClassFixture<ApiSmokeTests.RutrApiFactory>
{
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiSmokeTests(RutrApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_jobs_approvals_and_portal_work()
    {
        var login = await _client.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "pm@rutters.demo",
            password = "rutters-demo"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await login.Content.ReadFromJsonAsync<LoginPayload>(JsonOptions);
        loginBody.Should().NotBeNull();
        loginBody!.Token.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginBody.Token);

        var jobs = await _client.GetAsync("/api/v1/jobs");
        jobs.StatusCode.Should().Be(HttpStatusCode.OK);
        var jobList = await jobs.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        jobList.GetArrayLength().Should().BeGreaterThan(0);

        var approvals = await _client.GetAsync("/api/v1/approvals");
        approvals.StatusCode.Should().Be(HttpStatusCode.OK);

        var portal = await _client.GetAsync("/api/v1/portal/keller-maple-demo");
        portal.StatusCode.Should().Be(HttpStatusCode.OK);
        var portalBody = await portal.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        portalBody.GetProperty("homeownerName").GetString().Should().Contain("Keller");
    }

    public sealed class RutrApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"rutr-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                    ["Jwt:SigningKey"] = "local-dev-only-signing-key-change-me-before-prod!!"
                });
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            try
            {
                if (File.Exists(_dbPath))
                {
                    File.Delete(_dbPath);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }

    private sealed record LoginPayload(string Token, string DisplayName, string Role, Guid OrganizationId);
}
