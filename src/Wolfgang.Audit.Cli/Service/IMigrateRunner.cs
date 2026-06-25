using McMaster.Extensions.CommandLineUtils;
using Wolfgang.Audit.Cli.Model;

namespace Wolfgang.Audit.Cli.Service;



/// <summary>
/// Executes the <c>migrate</c> subcommand. v1 implementation is a stub that prints
/// the pending work and exits; the real migration runner will land alongside the
/// <c>Wolfgang.Audit.EFCore.Migrations.*</c> packages in a follow-up PR.
/// </summary>
internal interface IMigrateRunner
{
    Task RunAsync(MigrateOptions options, IConsole console);
}
