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
// is built first wins. Splitting into two concrete types (rather than a shared
// abstract base) gives each variant its own model-cache entry and its own
// consistent schema. The audited variant derives from AuditingDbContext to
// mirror what real consumers do; the unaudited variant derives from DbContext
// directly as the comparison baseline.

[ExcludeFromCodeCoverage]
public sealed class UnauditedBenchmarkDbContext : DbContext
{
    public UnauditedBenchmarkDbContext(DbContextOptions<UnauditedBenchmarkDbContext> options)
        : base(options)
    {
    }



    public DbSet<Customer> Customers => Set<Customer>();
}

[ExcludeFromCodeCoverage]
public sealed class AuditedBenchmarkDbContext : AuditingDbContext
{
    public AuditedBenchmarkDbContext
    (
        DbContextOptions<AuditedBenchmarkDbContext> options,
        IAuditUserProvider userProvider,
        AuditOptions auditOptions
    )
        : base(options, userProvider, auditOptions)
    {
    }



    public DbSet<Customer> Customers => Set<Customer>();
}

[ExcludeFromCodeCoverage]
public sealed class StaticAuditUserProvider : IAuditUserProvider
{
    public AuditUser GetCurrentUser() => new("benchmark-user");
}
