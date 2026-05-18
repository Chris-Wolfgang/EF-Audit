using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Wolfgang.Audit;
using Wolfgang.Audit.Migrations.SqlServer;
using Xunit;

namespace Wolfgang.Audit.EFCore.Migrations.SqlServer.Tests.Integration;



/// <summary>
/// End-to-end tests that spin up SQL Server in a Testcontainer, run
/// <see cref="SqlServerAuditMigrationsRunner"/>, and assert the resulting
/// schema. Validates Approach B: <see cref="AuditOptions.Schema"/> /
/// <see cref="AuditOptions.HeaderTableName"/> / <see cref="AuditOptions.DetailTableName"/>
/// flow into the generated DDL at apply time.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SqlServerAuditMigrationsRunnerTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .Build();



    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();



    private string ConnectionStringFor(string database)
    {
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = database,
        };
        return builder.ConnectionString;
    }



    private async Task EnsureDatabaseAsync(string database)
    {
        var master = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "master",
        }.ConnectionString;

        await using var conn = new SqlConnection(master);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"IF DB_ID(N'{database}') IS NULL CREATE DATABASE [{database}];";
        await cmd.ExecuteNonQueryAsync();
    }



    private async Task<bool> TableExistsAsync(string connectionString, string? schema, string table)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES " +
            "WHERE TABLE_NAME = @t AND (@s IS NULL OR TABLE_SCHEMA = @s);";
        cmd.Parameters.Add(new SqlParameter("@t", table));
        cmd.Parameters.Add(new SqlParameter("@s", (object?)schema ?? DBNull.Value));
        var count = (int)(await cmd.ExecuteScalarAsync() ?? 0);
        return count > 0;
    }



    [Fact]
    public async Task RunAsync_creates_audit_tables_under_default_schema()
    {
        const string db = "default_schema_test";
        await EnsureDatabaseAsync(db);
        var conn = ConnectionStringFor(db);

        var options = new AuditOptions();

        await SqlServerAuditMigrationsRunner.RunAsync(conn, options, dryRun: false);

        Assert.True(await TableExistsAsync(conn, schema: "dbo", "AuditHeader"));
        Assert.True(await TableExistsAsync(conn, schema: "dbo", "AuditDetail"));
        Assert.True(await TableExistsAsync(conn, schema: "dbo", "__AuditMigrationsHistory"));
    }



    [Fact]
    public async Task RunAsync_creates_audit_tables_under_custom_schema()
    {
        const string db = "custom_schema_test";
        await EnsureDatabaseAsync(db);
        var conn = ConnectionStringFor(db);

        var options = new AuditOptions
        {
            Schema           = "audit",
            HeaderTableName  = "MyHeader",
            DetailTableName  = "MyDetail",
        };

        await SqlServerAuditMigrationsRunner.RunAsync(conn, options, dryRun: false);

        Assert.True(await TableExistsAsync(conn, schema: "audit", "MyHeader"));
        Assert.True(await TableExistsAsync(conn, schema: "audit", "MyDetail"));
    }



    [Fact]
    public async Task RunAsync_is_idempotent_when_called_twice()
    {
        const string db = "idempotent_test";
        await EnsureDatabaseAsync(db);
        var conn = ConnectionStringFor(db);

        var options = new AuditOptions();

        await SqlServerAuditMigrationsRunner.RunAsync(conn, options, dryRun: false);
        // Second invocation must be a no-op — running the runner against an
        // already-current schema is part of the documented contract.
        await SqlServerAuditMigrationsRunner.RunAsync(conn, options, dryRun: false);

        Assert.True(await TableExistsAsync(conn, schema: "dbo", "AuditHeader"));
        Assert.True(await TableExistsAsync(conn, schema: "dbo", "AuditDetail"));
    }



    [Fact]
    public async Task RunAsync_with_dryRun_returns_script_without_creating_tables()
    {
        const string db = "dryrun_test";
        await EnsureDatabaseAsync(db);
        var conn = ConnectionStringFor(db);

        var options = new AuditOptions();

        var script = await SqlServerAuditMigrationsRunner.RunAsync(conn, options, dryRun: true);

        Assert.False(string.IsNullOrWhiteSpace(script));
        Assert.Contains("AuditHeader", script, StringComparison.Ordinal);
        Assert.Contains("AuditDetail", script, StringComparison.Ordinal);
        Assert.False(await TableExistsAsync(conn, schema: "dbo", "AuditHeader"));
        Assert.False(await TableExistsAsync(conn, schema: "dbo", "AuditDetail"));
    }
}
