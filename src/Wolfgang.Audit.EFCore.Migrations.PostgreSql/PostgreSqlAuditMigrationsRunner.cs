using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Wolfgang.Audit.Migrations.PostgreSql;



/// <summary>
/// Applies the PostgreSQL audit-schema migrations against the consumer's database.
/// The CLI's <c>migrate</c> subcommand calls this when <c>--provider postgresql</c>
/// is selected.
/// </summary>
public static class PostgreSqlAuditMigrationsRunner
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
            .UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(PostgreSqlAuditMigrationsRunner).Assembly.GetName().Name);
                // Dedicated history table so we don't clash with the consumer's
                // own __EFMigrationsHistory.
                npgsql.MigrationsHistoryTable("__AuditMigrationsHistory", options.Schema);
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
