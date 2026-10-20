using System.Text.Json.Serialization;
using AccessDesk.Api.Data;
using AccessDesk.Api.Security;
using AccessDesk.Api.Services;
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

// Code-first schema. EF Core migrations come next; EnsureCreated keeps the first run simple.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AccessDeskDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program;
