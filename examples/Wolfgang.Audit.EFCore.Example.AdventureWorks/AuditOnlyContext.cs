using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Example.AdventureWorks;



/// <summary>
/// A dedicated DbContext whose model contains only the audit entities
/// (<c>AuditHeader</c>, <c>AuditDetail</c>). Used by <c>CreateAuditTablesAsync</c>
/// in <c>Program.cs</c> so <c>EnsureCreatedAsync</c> creates just the audit
/// tables, not also the Person / EmailAddress tables that AdventureWorks has
/// already restored.
/// </summary>
internal sealed class AuditOnlyContext : DbContext
{
    private readonly AuditOptions _options;



    public AuditOnlyContext(DbContextOptions<AuditOnlyContext> options, AuditOptions auditOptions)
        : base(options)
    {
        _options = auditOptions ?? throw new ArgumentNullException(nameof(auditOptions));
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyAuditing(_options);
    }
}
