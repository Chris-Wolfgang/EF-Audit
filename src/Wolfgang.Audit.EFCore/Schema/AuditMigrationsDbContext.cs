using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Schema;



/// <summary>
/// Library-internal <see cref="DbContext"/> whose model contains only the audit
/// entities — <see cref="Entities.AuditHeader"/>, <see cref="Entities.AuditDetail"/>,
/// and the version tracking row <see cref="AuditSchemaVersion"/>. Used by
/// <c>AuditSchemaMigrator</c> to install or upgrade the audit schema
/// without dragging the consumer's own entity model into the diff.
/// </summary>
/// <remarks>
/// Public so the CLI can construct it directly with a
/// <see cref="DbContextOptions{TContext}"/> configured for any EF Core provider
/// (SQL Server, PostgreSQL, MySQL, SQLite). Consumer applications normally never
/// touch this type — they call <c>MigrateAuditSchemaAsync</c> on their own
/// <see cref="AuditingDbContext"/> instead.
/// </remarks>
public sealed class AuditMigrationsDbContext : DbContext
{
    /// <summary>
    /// The schema-name / table-name overrides applied when the audit entities are
    /// configured on the model.
    /// </summary>
    public AuditOptions Options { get; }



    /// <summary>
    /// Constructs the migrations context with a provider-configured
    /// <see cref="DbContextOptions{TContext}"/> and the live
    /// <see cref="AuditOptions"/> that drive table/schema naming.
    /// </summary>
    public AuditMigrationsDbContext(DbContextOptions<AuditMigrationsDbContext> dbOptions, AuditOptions options)
        : base(dbOptions)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }



    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        // Audit entities exactly as the consumer's AuditingDbContext sees them.
        modelBuilder.ApplyAuditing(Options);

        // Version-tracking row. Single-row table; key is the constant 1.
        modelBuilder.Entity<AuditSchemaVersion>(b =>
        {
            if (!string.IsNullOrWhiteSpace(Options.Schema))
            {
                b.ToTable(AuditSchemaConstants.VersionTableName, Options.Schema);
            }
            else
            {
                b.ToTable(AuditSchemaConstants.VersionTableName);
            }
            b.HasKey(v => v.Id);
            b.Property(v => v.Id).ValueGeneratedNever();
            b.Property(v => v.Version).IsRequired();
        });
    }
}
