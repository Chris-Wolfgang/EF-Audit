using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Tests.Unit.TestSupport;

[ExcludeFromCodeCoverage]
public class TestDbContext : DbContext
{
    private readonly AuditOptions _auditOptions;

    public TestDbContext(DbContextOptions<TestDbContext> options, AuditOptions auditOptions)
        : base(options)
    {
        _auditOptions = auditOptions;
    }

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<CacheEntry> CacheEntries => Set<CacheEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyAuditing(_auditOptions);
    }
}
