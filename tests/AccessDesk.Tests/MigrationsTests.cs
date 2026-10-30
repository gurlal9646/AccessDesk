using AccessDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AccessDesk.Tests;

public class MigrationsTests
{
    // Builds the SQL Server model without opening a connection, so no database is needed.
    private static AccessDeskDbContext SqlServerContext() => new(
        new DbContextOptionsBuilder<AccessDeskDbContext>()
            .UseSqlServer("Server=localhost;Database=AccessDesk;Trusted_Connection=True;TrustServerCertificate=True")
            .Options);

    [Fact]
    public void Migrations_MatchTheModel_SoNoModelChangeIsMissingAMigration()
    {
        using var db = SqlServerContext();

        Assert.False(db.Database.HasPendingModelChanges(),
            "The model changed without a migration. Run: dotnet ef migrations add <Name> --project src/AccessDesk.Api --output-dir Data/Migrations");
    }

    [Fact]
    public void Migrations_ExistForSqlServer()
    {
        using var db = SqlServerContext();

        Assert.NotEmpty(db.Database.GetMigrations());
    }
}
