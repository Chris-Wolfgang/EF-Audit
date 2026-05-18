using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Migrations.SqlServer;



/// <summary>
/// Library-internal <see cref="DbContext"/> that knows only the audit entity
/// types (<see cref="Entities.AuditHeader"/>, <see cref="Entities.AuditDetail"/>).
/// Migrations are discovered and applied against this context — the consumer's
/// own <c>DbContext</c> is never touched, so library updates can't interleave
/// with the consumer's own EF migrations.
/// </summary>
internal sealed class AuditMigrationsDbContext : DbContext
{
    private readonly AuditOptions _options;



    public AuditMigrationsDbContext(DbContextOptions<AuditMigrationsDbContext> dbOptions, AuditOptions options)
        : base(dbOptions)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyAuditing(_options);
    }
}
