using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Benchmarks;

[ExcludeFromCodeCoverage]
public class Customer
{
    public int CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }

    public int LoyaltyPoints { get; set; }
}

// EF Core caches IModel per DbContext type, so an audited and unaudited variant
// sharing the same type would contaminate each other's cached model — whichever
// is built first wins. Splitting into two concrete subclasses gives each
// variant its own type, its own model-cache entry, and its own consistent
// schema. The shared base owns the Customer DbSet so both variants see the
// same user-data shape.

[ExcludeFromCodeCoverage]
public abstract class BenchmarkDbContextBase : DbContext
{
    protected BenchmarkDbContextBase(DbContextOptions options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();
}

[ExcludeFromCodeCoverage]
public sealed class UnauditedBenchmarkDbContext : BenchmarkDbContextBase
{
    public UnauditedBenchmarkDbContext(DbContextOptions<UnauditedBenchmarkDbContext> options)
        : base(options)
    {
    }
}

[ExcludeFromCodeCoverage]
public sealed class AuditedBenchmarkDbContext : BenchmarkDbContextBase
{
    private readonly AuditOptions _auditOptions;

    public AuditedBenchmarkDbContext(DbContextOptions<AuditedBenchmarkDbContext> options, AuditOptions auditOptions)
        : base(options)
    {
        _auditOptions = auditOptions;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyAuditing(_auditOptions);
    }
}

[ExcludeFromCodeCoverage]
public sealed class StaticAuditUserProvider : IAuditUserProvider
{
    public AuditUser GetCurrentUser() => new("benchmark-user");
}
