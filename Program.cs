using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Rutr.Api.Authentication;
using Rutr.Api.Components;
using Rutr.Api.Configuration;
using Rutr.Api.Data;
using Rutr.Api.Models.Validators;
using Rutr.Api.Services;
using Rutr.Api.Services.Ai;
using Rutr.Api.Services.CompanyCam;
using Rutr.Api.Services.JobNimbus;
using Rutr.Api.Services.Messaging;
using Rutr.Api.Services.Places;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

// preserveStaticLogger: true keeps WebApplicationFactory hosts from freezing the static logger.
builder.Host.UseSerilog(
    (ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration).WriteTo.Console(),
    preserveStaticLogger: true);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<IntegrationOptions>(builder.Configuration.GetSection(IntegrationOptions.SectionName));
builder.Services.Configure<MessagingOptions>(builder.Configuration.GetSection(MessagingOptions.SectionName));
builder.Services.Configure<PlacesOptions>(builder.Configuration.GetSection(PlacesOptions.SectionName));

// Resolve connection string at scope creation so test hosts can override via configuration.
builder.Services.AddDbContext<RutrDbContext>((sp, opt) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config.GetConnectionString("Default") ?? "Data Source=rutr-local.db";
    opt.UseSqlite(connectionString);
});

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
            // Prefer real JWT when present so signup/login orgs are not overridden by leftover X-Demo-Auth.
            var auth = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(auth)
                && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                && auth.Length > "Bearer ".Length)
            {
                return JwtBearerDefaults.AuthenticationScheme;
            }

            if (context.Request.Headers.ContainsKey("X-Demo-Auth"))
            {
                return DemoAuthHandler.SchemeName;
            }

            return JwtBearerDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role
        };
    })
    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DemoAuthHandler>(
        DemoAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
builder.Services.AddProblemDetails();
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
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<PhotoService>();
builder.Services.AddScoped<EvidenceService>();
builder.Services.AddScoped<ContactService>();
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<NoteService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<MilestoneService>();
builder.Services.AddScoped<ConsentService>();
builder.Services.AddScoped<DraftService>();
builder.Services.AddScoped<OrganizationService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<IntegrationConnectionService>();
builder.Services.AddScoped<ConnectSyncService>();
builder.Services.AddScoped<PlacesService>();
builder.Services.AddHttpClient("GooglePlaces", client =>
{
    client.BaseAddress = new Uri("https://maps.googleapis.com/maps/api/");
    client.Timeout = TimeSpan.FromSeconds(8);
});
builder.Services.AddScoped<MockDigestSender>();
builder.Services.AddSingleton<MockDraftGenerator>();
builder.Services.AddSingleton<MockJobNimbusClient>();
builder.Services.AddSingleton<MockCompanyCamClient>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RutrDbContext>();
    await SeedData.EnsureSeededAsync(db);
}

if (!app.Environment.IsProduction())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Local evidence uploads — portal/staff <img> tags need public /uploads/*
var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
Directory.CreateDirectory(Path.Combine(webRoot, "uploads"));
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot),
    RequestPath = ""
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging();
if (app.Environment.IsProduction())
{
    app.UseExceptionHandler();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "Rutr.Api" }));

await app.RunAsync();

public partial class Program;
