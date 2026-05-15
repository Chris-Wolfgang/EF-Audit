using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Tests.Smoke.TestSupport;

[ExcludeFromCodeCoverage]
public class SmokeDbContext : AuditingDbContext
{
    public SmokeDbContext
    (
        DbContextOptions<SmokeDbContext> options,
        IAuditUserProvider userProvider,
        AuditOptions auditOptions
    )
        : base(options, userProvider, auditOptions)
    {
    }



    public DbSet<Order> Orders => Set<Order>();
}
