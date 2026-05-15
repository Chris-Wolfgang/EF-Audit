using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Tests.Integration.TestSupport;

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
}
