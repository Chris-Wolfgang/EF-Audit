using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Tests.Smoke.TestSupport;

[ExcludeFromCodeCoverage]
public class SmokeDbContext : DbContext
{
    private readonly AuditOptions _auditOptions;

    public SmokeDbContext(DbContextOptions<SmokeDbContext> options, AuditOptions auditOptions)
        : base(options)
    {
        _auditOptions = auditOptions;
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyAuditing(_auditOptions);
    }
}

[ExcludeFromCodeCoverage]
public sealed class StaticAuditUserProvider : IAuditUserProvider
{
    private readonly AuditUser _user;
    public StaticAuditUserProvider(string id) => _user = new AuditUser(id);
    public AuditUser GetCurrentUser() => _user;
}
