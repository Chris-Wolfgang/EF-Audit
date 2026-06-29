using McMaster.Extensions.CommandLineUtils;
using Wolfgang.AuditTrail.Cli.Model;
using Wolfgang.AuditTrail.Cli.Service;

namespace Wolfgang.AuditTrail.Cli.Tests.Unit.TestSupport;



/// <summary>
/// Fake <see cref="IMigrateRunner"/> that records the <see cref="MigrateOptions"/>
/// it was handed instead of actually doing anything. Lets the
/// <see cref="Command.Migrate"/> option-parsing tests assert on the resolved
/// values without spinning up an EF Core context.
/// </summary>
internal sealed class CapturingMigrateRunner : IMigrateRunner
{
    public MigrateOptions? CapturedOptions { get; private set; }

    public int CallCount { get; private set; }

    public Exception? ThrowOnRun { get; set; }



    public Task RunAsync(MigrateOptions options, IConsole console)
    {
        CallCount++;
        CapturedOptions = options;

        if (ThrowOnRun is not null)
        {
            throw ThrowOnRun;
        }

        return Task.CompletedTask;
    }
}
