using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RutrApi.Tests;

public sealed class RutrApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"rutr-test-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("Jwt:SigningKey", "local-dev-only-signing-key-change-me-before-prod!!");
        builder.UseSetting("Integrations:UseMocks", "true");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = $"Data Source={_dbPath}",
                ["Jwt:SigningKey"] = "local-dev-only-signing-key-change-me-before-prod!!",
                ["Integrations:UseMocks"] = "true"
            });
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        try
        {
            foreach (var suffix in new[] { "", "-shm", "-wal" })
            {
                var path = _dbPath + suffix;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
