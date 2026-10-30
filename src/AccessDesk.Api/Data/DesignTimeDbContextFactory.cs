using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccessDesk.Api.Data;

// Used only by `dotnet ef`. Migrations target SQL Server; the connection string is never opened when generating them.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AccessDeskDbContext>
{
    public AccessDeskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AccessDeskDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=AccessDesk;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        return new AccessDeskDbContext(options);
    }
}
