using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace Wolfgang.Audit.Migrations.MySql;



/// <summary>
/// Applies the MySQL audit-schema migrations against the consumer's database.
/// The CLI's <c>migrate</c> subcommand calls this when <c>--provider mysql</c>
/// is selected.
/// </summary>
public static class MySqlAuditMigrationsRunner
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

        var serverVersion = ServerVersion.AutoDetect(connectionString);

        var dbOptionsBuilder = new DbContextOptionsBuilder<AuditMigrationsDbContext>()
            .UseMySql(connectionString, serverVersion, mysql =>
            {
                mysql.MigrationsAssembly(typeof(MySqlAuditMigrationsRunner).Assembly.GetName().Name);
                // Dedicated history table so we don't collide with the consumer's
                // own __EFMigrationsHistory.
                mysql.MigrationsHistoryTable("__AuditMigrationsHistory");
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
