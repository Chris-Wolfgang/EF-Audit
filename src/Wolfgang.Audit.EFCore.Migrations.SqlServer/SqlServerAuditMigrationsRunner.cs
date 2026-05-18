using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Wolfgang.Audit.Migrations.SqlServer;



/// <summary>
/// Applies the SQL Server audit-schema migrations against the consumer's database.
/// The CLI's <c>migrate</c> subcommand calls this when <c>--provider sqlserver</c>
/// is selected (auto-detected or explicit).
/// </summary>
public static class SqlServerAuditMigrationsRunner
{
    /// <summary>
    /// Applies all pending migrations to <paramref name="connectionString"/>. Idempotent —
    /// running twice when the schema is already current is a no-op.
    /// </summary>
    /// <param name="connectionString">Target SQL Server connection string.</param>
    /// <param name="options">Audit options carrying schema + table-name overrides.</param>
    /// <param name="dryRun">If <c>true</c>, generate the pending-migration SQL and return it
    /// without touching the database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The SQL that was (or would be, on dry-run) executed.</returns>
    public static async Task<string> RunAsync
    (
        string connectionString,
        AuditOptions options,
        bool dryRun,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        ArgumentNullException.ThrowIfNull(options);

        var dbOptionsBuilder = new DbContextOptionsBuilder<AuditMigrationsDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(SqlServerAuditMigrationsRunner).Assembly.GetName().Name);
                // Use a dedicated history table so we don't clash with the consumer's
                // own __EFMigrationsHistory.
                sql.MigrationsHistoryTable("__AuditMigrationsHistory", options.Schema);
            })
            .ReplaceService<IMigrationsAssembly, DiAwareMigrationsAssembly>();

        await using var context = new AuditMigrationsDbContext(dbOptionsBuilder.Options, options);

        if (dryRun)
        {
            // Generate the script without applying it.
            var migrator = context.GetInfrastructure().GetRequiredService<IMigrator>();
            return migrator.GenerateScript(
                fromMigration: null,
                toMigration:   null,
                options:       MigrationsSqlGenerationOptions.Idempotent);
        }

        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        return string.Empty;
    }
}
