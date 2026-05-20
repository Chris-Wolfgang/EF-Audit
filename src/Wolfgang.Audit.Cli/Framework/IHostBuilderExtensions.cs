using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Wolfgang.Audit.Cli.Framework;



internal enum ConfigurationFileMethod
{
    SingleFile,
}



// ReSharper disable once InconsistentNaming
internal static class IHostBuilderExtensions
{
    /// <summary>
    /// Adds <c>AppSettings.json</c> to the host's configuration, layered with
    /// environment variables on top.
    /// </summary>
    /// <param name="builder">Host builder to extend.</param>
    /// <param name="method">
    /// Which file-loading strategy to use. Only <see cref="ConfigurationFileMethod.SingleFile"/>
    /// is supported today; the enum is kept so a future per-environment overlay
    /// can be wired in without changing call sites.
    /// </param>
    /// <param name="optional">Whether the JSON file is required to exist.</param>
    /// <param name="reloadOnChange">Whether to re-read the file when it changes on disk.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="builder"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If <paramref name="method"/> is not a recognised value.</exception>
    public static IHostBuilder AddConfigurationFile
    (
        this IHostBuilder builder,
        ConfigurationFileMethod method,
        bool optional = false,
        bool reloadOnChange = false
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        return method switch
        {
            ConfigurationFileMethod.SingleFile => AddSingleConfigFile(builder, optional, reloadOnChange),
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, null),
        };
    }



    private static IHostBuilder AddSingleConfigFile
    (
        IHostBuilder builder,
        bool optional,
        bool reloadOnChange
    )
    {
        builder.ConfigureAppConfiguration((context, configurationBuilder) =>
        {
            configurationBuilder
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("AppSettings.json", optional, reloadOnChange)
                .AddEnvironmentVariables();
        });

        return builder;
    }
}
