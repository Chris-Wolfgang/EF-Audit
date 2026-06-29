using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.AuditTrail.Tests.Unit.TestSupport;

[ExcludeFromCodeCoverage]
public class TestDbContext : AuditingDbContext
{
    public TestDbContext
    (
        DbContextOptions<TestDbContext> options,
        IAuditUserProvider userProvider,
        AuditOptions auditOptions
    )
        : base(options, userProvider, auditOptions)
    {
    }



    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CacheEntry> CacheEntries => Set<CacheEntry>();

    public DbSet<OrderLine> OrderLines => Set<OrderLine>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.Entity<OrderLine>().HasKey(o => new { o.OrderId, o.LineNumber });
        base.OnModelCreating(modelBuilder); // applies audit entity mappings
    }
}
