using AccessDesk.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace AccessDesk.Api.Data;

public class AccessDeskDbContext(DbContextOptions<AccessDeskDbContext> options) : DbContext(options)
{
    public DbSet<AccessRequest> AccessRequests => Set<AccessRequest>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.RequesterId).HasMaxLength(200).IsRequired();
            e.Property(x => x.RequesterName).HasMaxLength(200).IsRequired();
            e.Property(x => x.System).HasMaxLength(200).IsRequired();
            e.Property(x => x.Justification).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.DecidedBy).HasMaxLength(200);
            e.Property(x => x.DecisionComment).HasMaxLength(2000);
            e.HasIndex(x => new { x.RequesterId, x.Status });
        });

        modelBuilder.Entity<AuditEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Actor).HasMaxLength(200).IsRequired();
            e.Property(x => x.Action).HasMaxLength(100).IsRequired();
            e.Property(x => x.Details).HasMaxLength(2000);
            e.HasIndex(x => x.TimestampUtc);
        });
    }
}
