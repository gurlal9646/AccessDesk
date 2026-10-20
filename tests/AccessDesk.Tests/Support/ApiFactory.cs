using AccessDesk.Api.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccessDesk.Tests.Support;

public class ApiFactory : WebApplicationFactory<Program>
{
    // One open connection keeps the in-memory SQLite database alive for the lifetime of the factory.
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.ConfigureServices(services =>
        {
            // EF Core 9 keeps the provider setup in IDbContextOptionsConfiguration<T> as well as DbContextOptions<T>;
            // both must go or the SQL Server and SQLite providers end up registered together.
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AccessDeskDbContext>)
                            || (d.ServiceType.IsGenericType
                                && d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration")
                                && d.ServiceType.GenericTypeArguments[0] == typeof(AccessDeskDbContext)))
                .ToList();
            foreach (var descriptor in toRemove)
            {
                services.Remove(descriptor);
            }
            services.AddDbContext<AccessDeskDbContext>(o => o.UseSqlite(_connection));

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
            services.PostConfigure<AuthenticationOptions>(o =>
            {
                o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            });
        });
    }

    public HttpClient CreateClientFor(string userId, string name, params string[] roles)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserNameHeader, name);
        if (roles.Length > 0)
        {
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, string.Join(',', roles));
        }
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
