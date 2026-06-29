using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Logging;
using Wolfgang.AuditTrail.Cli.Framework;
using Wolfgang.AuditTrail.Cli.Model;
using Wolfgang.AuditTrail.Cli.Service;

namespace Wolfgang.AuditTrail.Cli.Command;



[Command
(
    Name = "migrate",
    Description = "Apply pending audit-schema migrations to the target database. Idempotent: running twice is a no-op once the schema is current.",
    ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated
)]
internal class Migrate
{
    [Option("-c|--connection-string <CONN>", Description = "ADO.NET connection string for the target database. Mutually exclusive with --connection-string-env.")]
    public string? ConnectionString { get; set; }

    [Option("--connection-string-env <ENV_VAR>", Description = "Name of an environment variable holding the connection string. Use this instead of --connection-string to keep secrets out of shell history.")]
    public string? ConnectionStringEnv { get; set; }

    [Option("-p|--provider <PROVIDER>", Description = "Database provider: sqlserver | postgresql | mysql | sqlite. Auto-detected from the connection string when possible.")]
    public string? Provider { get; set; }

    [Option("--schema <SCHEMA>", Description = "Schema to install the audit tables under. Defaults to the provider's default (dbo for SQL Server, public for PostgreSQL, none for SQLite/MySQL).")]
    public string? Schema { get; set; }

    [Option("--header-table <NAME>", Description = "Override the audit-header table name (default: AuditHeader).")]
    public string HeaderTable { get; set; } = "AuditHeader";

    [Option("--detail-table <NAME>", Description = "Override the audit-detail table name (default: AuditDetail).")]
    public string DetailTable { get; set; } = "AuditDetail";

    [Option("--dry-run", Description = "Print the SQL that would be executed but do not apply it.")]
    public bool DryRun { get; set; }



    internal async Task<int> OnExecuteAsync
    (
        IConsole console,
        ILogger<Migrate> logger,
        IMigrateRunner runner
    )
    {
        logger.LogDebug("Starting {Command}", GetType().Name);

        try
        {
            var options = ResolveOptions(console);
            if (options is null)
            {
                return ExitCode.ApplicationError;
            }

            await runner.RunAsync(options, console).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogCritical(e, "Unhandled error: {Message}", e.Message);
#pragma warning disable CA1849, VSTHRD103, S6966
            console.Error.WriteLine($"Error: {e.Message}");
#pragma warning restore CA1849, VSTHRD103, S6966
            return ExitCode.ApplicationError;
        }

        logger.LogDebug("Completed {Command}", GetType().Name);
        return ExitCode.Success;
    }



    private MigrateOptions? ResolveOptions(IConsole console)
    {
        var connectionString = ResolveConnectionString(console);
        if (connectionString is null)
        {
            return null;
        }

        var provider = ResolveProvider(console, connectionString);
        if (provider is null)
        {
            return null;
        }

        return new MigrateOptions
        (
            ConnectionString:  connectionString,
            Provider:          provider.Value,
            Schema:            string.IsNullOrWhiteSpace(Schema) ? null : Schema,
            HeaderTableName:   HeaderTable,
            DetailTableName:   DetailTable,
            DryRun:            DryRun
        );
    }



    private string? ResolveConnectionString(IConsole console)
    {
        var connectionString = ConnectionString;
        if (!string.IsNullOrWhiteSpace(ConnectionStringEnv))
        {
            if (!string.IsNullOrWhiteSpace(ConnectionString))
            {
#pragma warning disable CA1849, VSTHRD103, S6966
                console.Error.WriteLine("Error: --connection-string and --connection-string-env are mutually exclusive.");
#pragma warning restore CA1849, VSTHRD103, S6966
                return null;
            }
            connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnv);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
#pragma warning disable CA1849, VSTHRD103, S6966
                console.Error.WriteLine($"Error: environment variable '{ConnectionStringEnv}' is not set or is empty.");
#pragma warning restore CA1849, VSTHRD103, S6966
                return null;
            }
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
#pragma warning disable CA1849, VSTHRD103, S6966
            console.Error.WriteLine("Error: one of --connection-string or --connection-string-env is required.");
#pragma warning restore CA1849, VSTHRD103, S6966
            return null;
        }

        return connectionString;
    }



    private DatabaseProvider? ResolveProvider(IConsole console, string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(Provider))
        {
            var explicitProvider = Provider.ToUpperInvariant() switch
            {
                "SQLSERVER" or "MSSQL" => DatabaseProvider.SqlServer,
                "POSTGRESQL" or "POSTGRES" or "PG" => DatabaseProvider.PostgreSql,
                "MYSQL" => DatabaseProvider.MySql,
                "SQLITE" => DatabaseProvider.Sqlite,
                _ => DatabaseProvider.Unknown,
            };

            if (explicitProvider == DatabaseProvider.Unknown)
            {
#pragma warning disable CA1849, VSTHRD103, S6966
                console.Error.WriteLine(
                    $"Error: unrecognized provider '{Provider}'. " +
                    "Valid values: sqlserver, postgresql, mysql, sqlite.");
#pragma warning restore CA1849, VSTHRD103, S6966
                return null;
            }

            return explicitProvider;
        }

        var detected = DetectProvider(connectionString);
        if (detected == DatabaseProvider.Unknown)
        {
#pragma warning disable CA1849, VSTHRD103, S6966
            console.Error.WriteLine(
                "Error: could not auto-detect provider from the connection string. " +
                "Specify --provider explicitly (sqlserver|postgresql|mysql|sqlite).");
#pragma warning restore CA1849, VSTHRD103, S6966
            return null;
        }

        return detected;
    }



    private static DatabaseProvider DetectProvider(string connectionString)
    {
        var lower = connectionString.ToLowerInvariant();

        if (lower.Contains("filename=", StringComparison.Ordinal)
            || lower.Contains(".sqlite", StringComparison.Ordinal)
            || lower.Contains(".db;", StringComparison.Ordinal)
            || lower.EndsWith(".db", StringComparison.Ordinal))
        {
            return DatabaseProvider.Sqlite;
        }

        if (lower.Contains("host=", StringComparison.Ordinal)
            && (lower.Contains("username=", StringComparison.Ordinal) || lower.Contains("user id=", StringComparison.Ordinal)))
        {
            return DatabaseProvider.PostgreSql;
        }

        if (lower.Contains("port=3306", StringComparison.Ordinal)
            || (lower.Contains("server=", StringComparison.Ordinal) && lower.Contains("uid=", StringComparison.Ordinal)))
        {
            return DatabaseProvider.MySql;
        }

        if (lower.Contains("server=", StringComparison.Ordinal) || lower.Contains("data source=", StringComparison.Ordinal))
        {
            return DatabaseProvider.SqlServer;
        }

        return DatabaseProvider.Unknown;
    }
}
