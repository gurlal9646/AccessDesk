using System.Text.Json.Serialization;
using AccessDesk.Api.Data;
using AccessDesk.Api.Security;
using AccessDesk.Api.Services;
using AccessDesk.Api.Services.Assistant;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database: SQL Server by default, SQLite for local development and tests (Database:Provider).
var provider = builder.Configuration["Database:Provider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? "Server=localhost,1433;Database=AccessDesk;User Id=sa;Password=<set-in-user-secrets>;TrustServerCertificate=True";
builder.Services.AddDbContext<AccessDeskDbContext>(options =>
{
    if (provider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlite(connectionString);
    }
    else
    {
        options.UseSqlServer(connectionString);
    }
});

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IAccessRequestService, AccessRequestService>();

// AI policy assistant: keyword retrieval over the policy documents, then one call to the Claude API (Assistant:ApiKey).
builder.Services.Configure<AssistantOptions>(builder.Configuration.GetSection("Assistant"));
builder.Services.AddSingleton<IPolicyStore, PolicyStore>();
builder.Services.AddSingleton<PolicyRetriever>();
builder.Services.AddHttpClient<IClaudeClient, AnthropicClient>(http =>
{
    http.BaseAddress = new Uri("https://api.anthropic.com/");
    http.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<IPolicyAssistant, PolicyAssistant>();

// Authentication: JWT bearer against an OIDC provider (Keycloak in docker-compose, or Entra ID).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Auth:Authority"];
        options.Audience = builder.Configuration["Auth:Audience"];
        options.RequireHttpsMetadata = builder.Configuration.GetValue("Auth:RequireHttpsMetadata", true);
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "preferred_username";
        options.TokenValidationParameters.RoleClaimType = "roles";
    });
builder.Services.AddTransient<IClaimsTransformation, KeycloakRolesClaimsTransformation>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(Policies.CanDecide, p => p.RequireRole(Roles.Manager, Roles.Admin))
    .AddPolicy(Policies.CanViewAudit, p => p.RequireRole(Roles.Admin));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"])
        .AllowAnyHeader()
        .AllowAnyMethod());
});

// Enums travel as readable text ("Pending") so the Angular client does not depend on numeric values.
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// SQL Server applies the EF Core migrations. SQLite (local development and tests) has no migration history of its own,
// so it is created straight from the model.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccessDeskDbContext>();
    if (db.Database.IsSqlite())
    {
        db.Database.EnsureCreated();
    }
    else
    {
        db.Database.Migrate();
    }
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
