using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Wolfgang.Audit.Schema;

#if NET8_0_OR_GREATER



/// <summary>
/// Convenience extensions for consumers who derive from
/// <see cref="AuditingDbContext"/>. Wraps <see cref="AuditSchemaMigrator"/> so
/// the consumer's existing provider + connection are reused without the caller
/// having to construct an <see cref="AuditMigrationsDbContext"/> by hand.
/// </summary>
public static class DbContextAuditSchemaExtensions
{
    /// <summary>
    /// Creates or upgrades the audit-schema tables in the database backing
    /// <paramref name="context"/>. Idempotent — calling on an already-current
    /// schema is a no-op.
    /// </summary>
    /// <param name="context">
    /// The consumer's <see cref="AuditingDbContext"/>. Its
    /// <see cref="AuditOptions"/> and provider-configured connection are reused.
    /// </param>
    /// <param name="dryRun">
    /// When <c>true</c>, returns the SQL that would be executed without
    /// touching the database.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The generated migration script when <paramref name="dryRun"/> is
    /// <c>true</c>; <see cref="string.Empty"/> otherwise.
    /// </returns>
    /// <exception cref="ArgumentNullException">If <paramref name="context"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">
    /// If <paramref name="context"/> is configured with a non-relational EF Core
    /// provider (e.g. the in-memory provider). The schema migrator depends on
    /// the provider's <c>IMigrationsSqlGenerator</c>.
    /// </exception>
    public static async Task<string> MigrateAuditSchemaAsync
    (
        this AuditingDbContext context,
        bool dryRun = false,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        // IDbContextOptions is the interface EF Core's internal container
        // always registers; the concrete DbContextOptions is only resolvable
        // when the context was built through AddDbContext (it gets registered
        // as part of that pipeline). Going through the interface works for
        // both new-up'd contexts and DI-provided ones.
        var sourceOptions = context.GetService<IDbContextOptions>();
        var builder       = new DbContextOptionsBuilder<AuditMigrationsDbContext>();

        // Carry over only the provider-specific relational extension (the one
        // that knows how to UseSqlServer / UseNpgsql / UseSqlite / UseMySql).
        // The base CoreOptionsExtension is synthesised fresh by the new builder
        // so it binds to AuditMigrationsDbContext, not the consumer's context
        // type.
        var relationalExtension = sourceOptions.Extensions
            .OfType<Microsoft.EntityFrameworkCore.Infrastructure.RelationalOptionsExtension>()
            .FirstOrDefault();

        if (relationalExtension is null)
        {
            throw new InvalidOperationException(
                "Audit schema migration requires a relational provider " +
                "(UseSqlServer, UseNpgsql, UseSqlite, or UseMySql). The supplied " +
                $"{context.GetType().Name} is configured with a non-relational provider.");
        }

        ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(relationalExtension);

        // MA0004 false-positive on `await using var` — the disposal `await` cannot
        // be ConfigureAwait'd via that syntax. Use the explicit ConfiguredAsyncDisposable
        // form so both the construction and the implicit DisposeAsync run without
        // resuming on the captured SynchronizationContext.
        var migrationsContext = new AuditMigrationsDbContext(builder.Options, context.AuditOptions);
        await using (migrationsContext.ConfigureAwait(false))
        {
            return await AuditSchemaMigrator
                .RunAsync(migrationsContext, dryRun, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
#endif
