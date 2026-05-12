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

[ExcludeFromCodeCoverage]
public class BenchmarkDbContext : DbContext
{
    private readonly AuditOptions? _auditOptions;

    public BenchmarkDbContext(DbContextOptions<BenchmarkDbContext> options, AuditOptions? auditOptions = null)
        : base(options)
    {
        _auditOptions = auditOptions;
    }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        if (_auditOptions is not null)
        {
            modelBuilder.ApplyAuditing(_auditOptions);
        }
    }
}

[ExcludeFromCodeCoverage]
public sealed class StaticAuditUserProvider : IAuditUserProvider
{
    public AuditUser GetCurrentUser() => new("benchmark-user");
}
