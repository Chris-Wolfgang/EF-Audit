using McMaster.Extensions.CommandLineUtils;
using Wolfgang.Audit;
using Wolfgang.Audit.Cli.Model;
using Wolfgang.Audit.Migrations.SqlServer;

namespace Wolfgang.Audit.Cli.Service;



/// <summary>
/// Real <see cref="IMigrateRunner"/> implementation for SQL Server. Delegates to
/// <see cref="SqlServerAuditMigrationsRunner.RunAsync"/>. Idempotent — running
/// twice when the schema is current is a no-op.
/// </summary>
internal sealed class SqlServerMigrateRunner : IMigrateRunner
{
    public async Task RunAsync(MigrateOptions options, IConsole console)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

        var auditOptions = new AuditOptions
        {
            Schema           = options.Schema,
            HeaderTableName  = options.HeaderTableName,
            DetailTableName  = options.DetailTableName,
        };

#pragma warning disable CA1849, VSTHRD103 // McMaster IConsole has no async overloads
        console.WriteLine($"audit migrate — provider: SqlServer");
        console.WriteLine($"  Schema:        {options.Schema ?? "<dbo>"}");
        console.WriteLine($"  Header table:  {options.HeaderTableName}");
        console.WriteLine($"  Detail table:  {options.DetailTableName}");
        console.WriteLine($"  Dry run:       {options.DryRun}");
        console.WriteLine();
#pragma warning restore CA1849, VSTHRD103

        var script = await SqlServerAuditMigrationsRunner
            .RunAsync(options.ConnectionString, auditOptions, options.DryRun)
            .ConfigureAwait(false);

#pragma warning disable CA1849, VSTHRD103
        if (options.DryRun)
        {
            console.WriteLine("-- Dry run: pending migration script (not applied)");
            console.WriteLine(script);
        }
        else
        {
            console.WriteLine("Migrations applied (or already current).");
        }
#pragma warning restore CA1849, VSTHRD103
    }
}
