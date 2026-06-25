using System.Diagnostics.CodeAnalysis;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit;
using Wolfgang.Audit.Cli.Model;
using Wolfgang.Audit.Schema;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit.Cli.Service;



/// <summary>
/// Real implementation of <see cref="IMigrateRunner"/> backed by
/// <see cref="AuditSchemaMigrator"/>. Picks the EF Core provider via a single
/// switch over <see cref="DatabaseProvider"/>; everything downstream of that
/// switch — model diffing, DDL generation, transactional apply, version
/// stamping — is provider-agnostic.
/// </summary>
/// <remarks>
/// Adding a new RDBMS = one new switch arm + one new <c>UseX()</c> PackageReference
/// in the csproj. No new service classes, no per-provider runners, no separate
/// migrations packages.
/// </remarks>
[ExcludeFromCodeCoverage] // Real EF Core + provider connection. Exercised by integration tests (Testcontainers).
internal sealed class SchemaMigrateRunner : IMigrateRunner
{
    public async Task RunAsync(MigrateOptions options, IConsole console)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

        var auditOptions = new AuditOptions
        {
            Schema              = options.Schema,
            HeaderTableName     = options.HeaderTableName,
            DetailTableName     = options.DetailTableName,
            // The migrator only touches schema; ApplyAuditing requires
            // serializers to be non-null, so seed defaults the consumer would
            // get from AddEfCoreAuditing<>(). They have no effect on DDL.
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        var dbContextOptions = BuildDbContextOptions(options);

#pragma warning disable CA1849, VSTHRD103, S6966 // McMaster IConsole has no async overloads
        console.WriteLine($"audit migrate — provider: {options.Provider}");
        console.WriteLine($"  Schema:        {options.Schema ?? "<provider default>"}");
        console.WriteLine($"  Header table:  {options.HeaderTableName}");
        console.WriteLine($"  Detail table:  {options.DetailTableName}");
        console.WriteLine($"  Dry run:       {options.DryRun}");
        console.WriteLine();
#pragma warning restore CA1849, VSTHRD103, S6966

        // MA0004: `await using var` cannot ConfigureAwait the implicit DisposeAsync.
        // Use the explicit ConfiguredAsyncDisposable form so dispose doesn't resume
        // on a captured SynchronizationContext.
        var context = new AuditMigrationsDbContext(dbContextOptions, auditOptions);
        await using var configured = context.ConfigureAwait(false);
        var script = await AuditSchemaMigrator
            .RunAsync(context, options.DryRun)
            .ConfigureAwait(false);

#pragma warning disable CA1849, VSTHRD103
        if (options.DryRun)
        {
            console.WriteLine("-- Dry run: pending migration script (not applied)");
            console.WriteLine(string.IsNullOrWhiteSpace(script)
                ? "-- (schema already current; nothing to apply)"
                : script);
        }
        else
        {
            console.WriteLine("Migrations applied (or already current).");
        }
#pragma warning restore CA1849, VSTHRD103, S6966
    }



    private static DbContextOptions<AuditMigrationsDbContext> BuildDbContextOptions(MigrateOptions options)
    {
        var builder = new DbContextOptionsBuilder<AuditMigrationsDbContext>();

        // The giant switch — the *only* code that knows about specific
        // provider packages. Add a new RDBMS by adding an arm here plus its
        // PackageReference in the csproj.
        return options.Provider switch
        {
            DatabaseProvider.SqlServer  => builder.UseSqlServer(options.ConnectionString).Options,
            DatabaseProvider.PostgreSql => builder.UseNpgsql(options.ConnectionString).Options,
            DatabaseProvider.Sqlite     => builder.UseSqlite(options.ConnectionString).Options,
            DatabaseProvider.MySql      => throw new NotSupportedException(
                "MySQL is not yet wired into the CLI: Pomelo.EntityFrameworkCore.MySql is " +
                "capped at EF Core 9.x while the CLI is on EF Core 10. Will re-enable once " +
                "Pomelo ships an EF Core 10 release."),
            _ => throw new ArgumentOutOfRangeException(
                     nameof(options),
                     options.Provider,
                     $"Unsupported provider '{options.Provider}'. Expected one of: SqlServer, PostgreSql, Sqlite."),
        };
    }
}
