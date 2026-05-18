using McMaster.Extensions.CommandLineUtils;
using Wolfgang.Audit.Cli.Model;

namespace Wolfgang.Audit.Cli.Service;



/// <summary>
/// v1 stub implementation: prints the work the runner would do, then exits. The
/// real implementation will arrive in a follow-up PR once the
/// <c>Wolfgang.Audit.EFCore.Migrations.{Provider}</c> packages exist. Keeping the
/// CLI's option parsing + flag wiring stable now lets consumers script against the
/// final UX even before the engine lands.
/// </summary>
internal sealed class StubMigrateRunner : IMigrateRunner
{
    public Task RunAsync(MigrateOptions options, IConsole console)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(console);

#pragma warning disable CA1849, VSTHRD103 // McMaster IConsole has no async overloads
        console.WriteLine($"audit migrate (stub — v1.0; real runner ships with Wolfgang.Audit.EFCore.Migrations.*)");
        console.WriteLine($"  Provider:      {options.Provider}");
        console.WriteLine($"  Schema:        {options.Schema ?? "<provider default>"}");
        console.WriteLine($"  Header table:  {options.HeaderTableName}");
        console.WriteLine($"  Detail table:  {options.DetailTableName}");
        console.WriteLine($"  Dry run:       {options.DryRun}");
        console.WriteLine();
        console.WriteLine("Would apply all pending migrations from the Wolfgang.Audit.EFCore.Migrations.{Provider} assembly.");
        console.WriteLine("Stub exits successfully without touching the database.");
#pragma warning restore CA1849, VSTHRD103

        return Task.CompletedTask;
    }
}
