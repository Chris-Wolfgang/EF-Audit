using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Wolfgang.Audit.Migrations.Sqlite;



/// <summary>
/// Applies the SQLite audit-schema migrations against the consumer's database.
/// The CLI's <c>migrate</c> subcommand calls this when <c>--provider sqlite</c>
/// is selected.
/// </summary>
public static class SqliteAuditMigrationsRunner
{
    /// <summary>
    /// Applies all pending migrations to <paramref name="connectionString"/>.
    /// Idempotent — running twice when the schema is current is a no-op.
    /// </summary>
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
            .UseSqlite(connectionString, sqlite =>
            {
                sqlite.MigrationsAssembly(typeof(SqliteAuditMigrationsRunner).Assembly.GetName().Name);
                // Dedicated history table so we don't collide with the consumer's
                // own __EFMigrationsHistory.
                sqlite.MigrationsHistoryTable("__AuditMigrationsHistory");
            })
            .ReplaceService<IMigrationsAssembly, DiAwareMigrationsAssembly>();

        await using var context = new AuditMigrationsDbContext(dbOptionsBuilder.Options, options);

        if (dryRun)
        {
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
