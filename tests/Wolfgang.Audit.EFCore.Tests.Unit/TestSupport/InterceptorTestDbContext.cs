using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Tests.Unit.TestSupport;



/// <summary>
/// Test context that derives directly from <see cref="DbContext"/> (no
/// <see cref="AuditingDbContext"/> base) so the interceptor path can be exercised
/// in isolation. Models the real-world case of an application whose context
/// already inherits from <c>IdentityDbContext&lt;TUser&gt;</c> or another
/// third-party base.
/// </summary>
[ExcludeFromCodeCoverage]
public class InterceptorTestDbContext : DbContext
{
    private readonly AuditOptions _auditOptions;



    public InterceptorTestDbContext
    (
        DbContextOptions<InterceptorTestDbContext> options,
        AuditOptions auditOptions
    )
        : base(options)
    {
        _auditOptions = auditOptions;
    }



    public DbSet<Customer> Customers => Set<Customer>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyAuditing(_auditOptions);
    }
}
