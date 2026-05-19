using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Migrations.PostgreSql;



/// <summary>
/// Library-internal <see cref="DbContext"/> that knows only the audit entity
/// types. Migrations are discovered and applied against this context — the
/// consumer's own <c>DbContext</c> is never touched.
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
