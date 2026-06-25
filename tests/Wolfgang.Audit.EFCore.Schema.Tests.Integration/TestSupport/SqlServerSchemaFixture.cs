using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Wolfgang.Audit;
using Wolfgang.Audit.Schema;
using Xunit;

namespace Wolfgang.Audit.EFCore.Schema.Tests.Integration.TestSupport;



[ExcludeFromCodeCoverage]
public sealed class SqlServerSchemaFixture : IAsyncLifetime, ISchemaProviderFixture
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .Build();

    private string _currentDatabase = "master";

    public string ProviderName => "SqlServer";

    public string CustomSchema => "audit";



    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();



    private string ConnectionStringFor(string database)
    {
        var b = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = database,
            // Disable pooling: tests create a fresh database per method, so each
            // connection string is distinct and pool entries accumulate. The
            // SqlClient pool has shown OOM under that pattern. Test runs are
            // short-lived; the pool overhead isn't relevant here.
            Pooling = false,
        };
        return b.ConnectionString;
    }



    public async Task<AuditMigrationsDbContext> CreateContextAsync(AuditOptions options)
    {
        // Unique database per test method so concurrent runs don't cross-contaminate.
        // SQL Server database names are bounded but we don't need that much entropy
        // — Ticks gives enough.
        _currentDatabase = $"audit_{DateTime.UtcNow.Ticks}";
        await EnsureDatabaseAsync(_currentDatabase);

        var dbOpts = new DbContextOptionsBuilder<AuditMigrationsDbContext>()
            .UseSqlServer(ConnectionStringFor(_currentDatabase))
            .Options;

        return new AuditMigrationsDbContext(dbOpts, options);
    }



    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(string? schema)
    {
        var rows = new List<TableInfo>();
        await using var conn = new SqlConnection(ConnectionStringFor(_currentDatabase));
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText =
            "SELECT TABLE_SCHEMA, TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new TableInfo(reader.GetString(0), reader.GetString(1)));
        }
        return rows;
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
}
