using Microsoft.Extensions.Logging.Abstractions;
using Wolfgang.AuditTrail.Cli.Command;
using Wolfgang.AuditTrail.Cli.Model;
using Wolfgang.AuditTrail.Cli.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Cli.Tests.Unit;



/// <summary>
/// Drives <see cref="Migrate"/> directly (it's internal; the CLI csproj has
/// InternalsVisibleTo wired). Captures the resolved <see cref="MigrateOptions"/>
/// through <see cref="CapturingMigrateRunner"/> so the assertions exercise the
/// parsing pipeline without needing a real EF Core context.
/// </summary>
public class MigrateTests
{
    private static async Task<(int exit, CapturingMigrateRunner runner, StringConsole console)> RunAsync(
        Migrate migrate,
        CapturingMigrateRunner? runner = null)
    {
        runner ??= new CapturingMigrateRunner();
        var console = new StringConsole();

        var exit = await migrate.OnExecuteAsync(console, NullLogger<Migrate>.Instance, runner);

        return (exit, runner, console);
    }



    // ── Connection string resolution ────────────────────────────────────────

    [Fact]
    public async Task OnExecuteAsync_when_neither_connection_string_set_returns_error()
    {
        var migrate = new Migrate { Provider = "sqlserver" };

        var (exit, runner, console) = await RunAsync(migrate);

        Assert.Equal(ExitCode.ApplicationError, exit);
        Assert.Equal(0, runner.CallCount);
        Assert.Contains("--connection-string or --connection-string-env is required", console.StdErr, StringComparison.Ordinal);
    }



    [Fact]
    public async Task OnExecuteAsync_when_connection_string_and_env_both_set_returns_error()
    {
        var migrate = new Migrate
        {
            ConnectionString    = "Server=.;Database=x",
            ConnectionStringEnv = "AUDIT_CONN_TEST",
        };

        var (exit, runner, console) = await RunAsync(migrate);

        Assert.Equal(ExitCode.ApplicationError, exit);
        Assert.Equal(0, runner.CallCount);
        Assert.Contains("mutually exclusive", console.StdErr, StringComparison.Ordinal);
    }



    [Fact]
    public async Task OnExecuteAsync_when_env_var_unset_returns_error()
    {
        var migrate = new Migrate { ConnectionStringEnv = "AUDIT_CONN_NOT_SET_" + Guid.NewGuid().ToString("N") };

        var (exit, runner, console) = await RunAsync(migrate);

        Assert.Equal(ExitCode.ApplicationError, exit);
        Assert.Equal(0, runner.CallCount);
        Assert.Contains("is not set or is empty", console.StdErr, StringComparison.Ordinal);
    }



    [Fact]
    public async Task OnExecuteAsync_when_env_var_set_uses_resolved_value()
    {
        var varName = "AUDIT_CONN_OK_" + Guid.NewGuid().ToString("N");
        Environment.SetEnvironmentVariable(varName, "Server=.;Database=fromenv");
        try
        {
            var migrate = new Migrate { ConnectionStringEnv = varName, Provider = "sqlserver" };

            var (exit, runner, _) = await RunAsync(migrate);

            Assert.Equal(ExitCode.Success, exit);
            Assert.NotNull(runner.CapturedOptions);
            Assert.Equal("Server=.;Database=fromenv", runner.CapturedOptions!.ConnectionString);
        }
        finally
        {
            Environment.SetEnvironmentVariable(varName, value: null);
        }
    }



    // ── Provider normalization (explicit) ────────────────────────────────────

    [Theory]
    [InlineData("sqlserver",  "SqlServer")]
    [InlineData("SqlServer",  "SqlServer")]
    [InlineData("mssql",      "SqlServer")]
    [InlineData("postgresql", "PostgreSql")]
    [InlineData("postgres",   "PostgreSql")]
    [InlineData("pg",         "PostgreSql")]
    [InlineData("mysql",      "MySql")]
    [InlineData("sqlite",     "Sqlite")]
    public async Task OnExecuteAsync_normalizes_explicit_provider_aliases(string input, string expectedName)
    {
        var migrate = new Migrate
        {
            ConnectionString = "Server=.;Database=x",
            Provider         = input,
        };

        var (exit, runner, _) = await RunAsync(migrate);

        Assert.Equal(ExitCode.Success, exit);
        Assert.Equal(expectedName, runner.CapturedOptions!.Provider.ToString());
    }



    [Fact]
    public async Task OnExecuteAsync_when_explicit_provider_unrecognized_returns_error()
    {
        var migrate = new Migrate
        {
            ConnectionString = "Server=.;Database=x",
            Provider         = "oracle",
        };

        var (exit, runner, console) = await RunAsync(migrate);

        Assert.Equal(ExitCode.ApplicationError, exit);
        Assert.Equal(0, runner.CallCount);
        Assert.Contains("unrecognized provider 'oracle'", console.StdErr, StringComparison.Ordinal);
    }



    // ── Provider auto-detection ──────────────────────────────────────────────

    [Theory]
    [InlineData("DataSource=/tmp/audit.db",                                              "Sqlite")]
    [InlineData("Filename=audit.db",                                                     "Sqlite")]
    [InlineData("/path/to/audit.sqlite",                                                 "Sqlite")]
    [InlineData("Host=localhost;Username=postgres;Password=p;Database=mydb",             "PostgreSql")]
    [InlineData("Host=db;User Id=app;Password=p;Database=mydb",                          "PostgreSql")]
    [InlineData("Server=localhost;Port=3306;Database=audit;Uid=app;Pwd=p",               "MySql")]
    [InlineData("Server=localhost;Database=audit;User Id=sa;Password=p;TrustServerCertificate=true", "SqlServer")]
    [InlineData("Data Source=localhost;Initial Catalog=audit;Integrated Security=true",  "SqlServer")]
    public async Task OnExecuteAsync_auto_detects_provider_from_connection_string(string conn, string expectedName)
    {
        var migrate = new Migrate { ConnectionString = conn };

        var (exit, runner, _) = await RunAsync(migrate);

        Assert.Equal(ExitCode.Success, exit);
        Assert.Equal(expectedName, runner.CapturedOptions!.Provider.ToString());
    }



    [Fact]
    public async Task OnExecuteAsync_when_provider_undetectable_returns_error()
    {
        var migrate = new Migrate { ConnectionString = "something=weird" };

        var (exit, runner, console) = await RunAsync(migrate);

        Assert.Equal(ExitCode.ApplicationError, exit);
        Assert.Equal(0, runner.CallCount);
        Assert.Contains("could not auto-detect provider", console.StdErr, StringComparison.Ordinal);
    }



    // ── Option pass-through ──────────────────────────────────────────────────

    [Fact]
    public async Task OnExecuteAsync_passes_dry_run_through_to_runner()
    {
        var migrate = new Migrate
        {
            ConnectionString = "Server=.;Database=x",
            Provider         = "sqlserver",
            DryRun           = true,
        };

        var (exit, runner, _) = await RunAsync(migrate);

        Assert.Equal(ExitCode.Success, exit);
        Assert.True(runner.CapturedOptions!.DryRun);
    }



    [Fact]
    public async Task OnExecuteAsync_passes_schema_and_table_names_through_to_runner()
    {
        var migrate = new Migrate
        {
            ConnectionString = "Server=.;Database=x",
            Provider         = "sqlserver",
            Schema           = "myaudit",
            HeaderTable      = "MyHeader",
            DetailTable      = "MyDetail",
        };

        var (exit, runner, _) = await RunAsync(migrate);

        Assert.Equal(ExitCode.Success, exit);
        Assert.Equal("myaudit",  runner.CapturedOptions!.Schema);
        Assert.Equal("MyHeader", runner.CapturedOptions.HeaderTableName);
        Assert.Equal("MyDetail", runner.CapturedOptions.DetailTableName);
    }



    [Fact]
    public async Task OnExecuteAsync_when_schema_is_whitespace_normalises_to_null()
    {
        // "" / whitespace on --schema should mean "use provider default", which
        // the runner sees as Schema == null.
        var migrate = new Migrate
        {
            ConnectionString = "Server=.;Database=x",
            Provider         = "sqlserver",
            Schema           = "   ",
        };

        var (exit, runner, _) = await RunAsync(migrate);

        Assert.Equal(ExitCode.Success, exit);
        Assert.Null(runner.CapturedOptions!.Schema);
    }



    [Fact]
    public async Task OnExecuteAsync_uses_default_table_names_when_not_overridden()
    {
        var migrate = new Migrate
        {
            ConnectionString = "Server=.;Database=x",
            Provider         = "sqlserver",
        };

        var (exit, runner, _) = await RunAsync(migrate);

        Assert.Equal(ExitCode.Success, exit);
        Assert.Equal("AuditHeader", runner.CapturedOptions!.HeaderTableName);
        Assert.Equal("AuditDetail", runner.CapturedOptions.DetailTableName);
    }



    // ── Runner failures ──────────────────────────────────────────────────────

    [Fact]
    public async Task OnExecuteAsync_when_runner_throws_returns_error_and_writes_message_to_stderr()
    {
        var migrate = new Migrate
        {
            ConnectionString = "Server=.;Database=x",
            Provider         = "sqlserver",
        };
        var runner = new CapturingMigrateRunner { ThrowOnRun = new InvalidOperationException("kaboom") };

        var (exit, _, console) = await RunAsync(migrate, runner);

        Assert.Equal(ExitCode.ApplicationError, exit);
        Assert.Contains("kaboom", console.StdErr, StringComparison.Ordinal);
    }
}
