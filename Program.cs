using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rutr.Api.Authentication;
using Rutr.Api.Components;
using Rutr.Api.Configuration;
using Rutr.Api.Data;
using Rutr.Api.Services;
using Rutr.Api.Services.CompanyCam;
using Rutr.Api.Services.JobNimbus;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
    builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console());

    builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
    builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection(IntegrationOptions.SectionName));

    var connectionString = builder.Configuration.GetConnectionString("Default")
                           ?? "Data Source=rutr-local.db";

    builder.Services.AddDbContext<RutrDbContext>(opt => opt.UseSqlite(connectionString));

    var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
    builder.Services
        .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "Smart";
            options.DefaultChallengeScheme = "Smart";
        })
        .AddPolicyScheme("Smart", "JWT or Demo", options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                if (context.Request.Headers.ContainsKey("X-Demo-Auth"))
                {
                    return DemoAuthHandler.SchemeName;
                }

                return JwtBearerDefaults.AuthenticationScheme;
            };
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt.Issuer,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
            };
        })
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DemoAuthHandler>(
            DemoAuthHandler.SchemeName, _ => { });

    builder.Services.AddAuthorization();
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "RUTR API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins(
                    "http://localhost:3317",
                    "http://127.0.0.1:3317")
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    builder.Services.AddScoped<JwtTokenService>();
    builder.Services.AddScoped<AuthService>();
    builder.Services.AddScoped<JobService>();
    builder.Services.AddScoped<ApprovalService>();
    builder.Services.AddScoped<PortalService>();
    builder.Services.AddSingleton<MockJobNimbusClient>();
    builder.Services.AddSingleton<MockCompanyCamClient>();

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<RutrDbContext>();
        await SeedData.EnsureSeededAsync(db);
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Rutr.Api" }));

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Rutr.Api failed to start");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

public partial class Program;
