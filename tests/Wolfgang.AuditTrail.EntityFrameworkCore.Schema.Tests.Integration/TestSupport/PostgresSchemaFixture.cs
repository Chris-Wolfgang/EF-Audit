using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Wolfgang.AuditTrail;
using Wolfgang.AuditTrail.Schema;
using Xunit;

namespace Wolfgang.AuditTrail.EntityFrameworkCore.Schema.Tests.Integration.TestSupport;



[ExcludeFromCodeCoverage]
public sealed class PostgresSchemaFixture : IAsyncLifetime, ISchemaProviderFixture
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16.4-alpine3.20")
        .WithDatabase("audit_root")
        .Build();

    private string _currentDatabase = "audit_root";

    public string ProviderName => "PostgreSQL";

    public string CustomSchema => "audit";



    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();



    private string ConnectionStringFor(string database)
    {
        var b = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = database,
        };
        return b.ConnectionString;
    }



    public async Task<AuditMigrationsDbContext> CreateContextAsync(AuditOptions options)
    {
        // PostgreSQL identifiers must be lowercase (or quoted) — keep ticks-based names
        // lowercase so unquoted DB references match.
        _currentDatabase = $"audit_{DateTime.UtcNow.Ticks}".ToLowerInvariant();
        await EnsureDatabaseAsync(_currentDatabase);

        var dbOpts = new DbContextOptionsBuilder<AuditMigrationsDbContext>()
            .UseNpgsql(ConnectionStringFor(_currentDatabase))
            .Options;

        return new AuditMigrationsDbContext(dbOpts, options);
    }



    public async Task<IReadOnlyList<TableInfo>> ListTablesAsync(string? schema)
    {
        var rows = new List<TableInfo>();
        await using var conn = new NpgsqlConnection(ConnectionStringFor(_currentDatabase));
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        // information_schema.tables has both public and any custom schema entries;
        // the test filters as needed.
        cmd.CommandText =
            "SELECT table_schema, table_name FROM information_schema.tables " +
            "WHERE table_type = 'BASE TABLE' " +
            "AND table_schema NOT IN ('pg_catalog', 'information_schema')";
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new TableInfo(reader.GetString(0), reader.GetString(1)));
        }
        return rows;
    }



    private async Task EnsureDatabaseAsync(string database)
    {
        // PostgreSQL doesn't support "CREATE DATABASE IF NOT EXISTS"; query and skip.
        var root = ConnectionStringFor("audit_root");
        await using var conn = new NpgsqlConnection(root);
        await conn.OpenAsync();

        await using (var probe = conn.CreateCommand())
        {
            probe.CommandText = $"SELECT 1 FROM pg_database WHERE datname = '{database}'";
            var exists = await probe.ExecuteScalarAsync();
            if (exists is not null)
            {
                return;
            }
        }

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE {database}";
        await cmd.ExecuteNonQueryAsync();
    }
}
