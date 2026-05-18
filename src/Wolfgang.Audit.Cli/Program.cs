using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Wolfgang.Audit.Cli.Command;
using Wolfgang.Audit.Cli.Framework;
using Wolfgang.Audit.Cli.Service;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Wolfgang.Audit.Cli;



[Command
(
    Name = "audit",
    Description = "Command-line companion for Wolfgang.Audit.EFCore. Applies and maintains audit-table schemas via EF Core migrations.",
    UnrecognizedArgumentHandling = UnrecognizedArgumentHandling.Throw,
    ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated
)]
[Subcommand(typeof(Migrate))]
[ExcludeFromCodeCoverage]
internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        try
        {
            return await new HostBuilder()
                .AddConfigurationFile(ConfigurationFileMethod.SingleFile, optional: true, reloadOnChange: false)
                .UseSerilog((context, configuration) =>
                {
                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .Enrich.WithProperty("Version", Assembly.GetEntryAssembly()?.GetName().Version);
                })
                .ConfigureServices((context, services) =>
                {
                    services
                        .AddSingleton<IReporter, ConsoleReporter>()
                        .AddSingleton<IMigrateRunner, StubMigrateRunner>();
                })
                .RunCommandLineApplicationAsync<Program>(args).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.Message).ConfigureAwait(false);
            Log.Logger.Fatal(e, "Unhandled exception: {Message}", e.Message);
            return ExitCode.UnhandledException;
        }
        finally
        {
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
        }
    }



    /// <summary>
    /// Invoked when the user runs <c>audit</c> with no subcommand. Prints help.
    /// </summary>
    internal int OnExecute
    (
        CommandLineApplication<Program> application,
        ILogger logger
    )
    {
        logger.LogDebug("Starting {Command}", GetType().Name);
        application.ShowHelp();
        return ExitCode.Success;
    }
}
