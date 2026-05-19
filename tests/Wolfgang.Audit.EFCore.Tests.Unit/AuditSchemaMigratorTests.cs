using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Schema;
using Wolfgang.Audit.Serializers;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



#if NET8_0_OR_GREATER
/// <summary>
/// Covers <see cref="AuditSchemaMigrator"/> end-to-end against an in-memory
/// SQLite database. SQLite is the right provider for unit tests because it
/// ships an <see cref="Microsoft.EntityFrameworkCore.Migrations.IMigrationsSqlGenerator"/>,
/// runs in-process, and exercises the same code paths the differ-based migrator
/// will use against SQL Server / PostgreSQL / MySQL in integration tests.
/// </summary>
public sealed class AuditSchemaMigratorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AuditOptions _options;



    public AuditSchemaMigratorTests()
    {
        // Shared in-memory connection so the schema survives between context
        // disposals within a single test.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
    }



    public void Dispose()
    {
        _connection.Dispose();
    }



    private AuditMigrationsDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<AuditMigrationsDbContext>()
            .UseSqlite(_connection);
        return new AuditMigrationsDbContext(builder.Options, _options);
    }



    [Fact]
    public async Task RunAsync_on_fresh_database_creates_audit_tables_and_stamps_version()
    {
        await using (var context = CreateContext())
        {
            await AuditSchemaMigrator.RunAsync(context);
        }

        await using var verify = CreateContext();
        Assert.Empty(await verify.Set<Entities.AuditHeader>().ToListAsync());
        Assert.Empty(await verify.Set<Entities.AuditDetail>().ToListAsync());
        var version = await verify.Set<AuditSchemaVersion>().SingleAsync();
        Assert.Equal(AuditSchemaConstants.CurrentSchemaVersion, version.Version);
    }



    [Fact]
    public async Task RunAsync_is_idempotent_when_called_twice()
    {
        await using (var first = CreateContext())
        {
            await AuditSchemaMigrator.RunAsync(first);
        }
        await using (var second = CreateContext())
        {
            // Second call must early-out on the version check and execute no SQL.
            // If it tried to CREATE TABLE again the second call would throw.
            await AuditSchemaMigrator.RunAsync(second);
        }

        await using var verify = CreateContext();
        var version = await verify.Set<AuditSchemaVersion>().SingleAsync();
        Assert.Equal(AuditSchemaConstants.CurrentSchemaVersion, version.Version);
    }



    [Fact]
    public async Task RunAsync_dryRun_returns_script_without_creating_tables()
    {
        await using var context = CreateContext();

        var script = await AuditSchemaMigrator.RunAsync(context, dryRun: true);

        Assert.False(string.IsNullOrWhiteSpace(script));
        Assert.Contains("AuditHeader", script, StringComparison.Ordinal);
        Assert.Contains("AuditDetail", script, StringComparison.Ordinal);
        Assert.Contains(AuditSchemaConstants.VersionTableName, script, StringComparison.Ordinal);

        // Dry-run must not have executed anything; querying any audit table
        // should throw "no such table".
        var ex = await Record.ExceptionAsync(async () =>
            await context.Set<Entities.AuditHeader>().ToListAsync());
        Assert.NotNull(ex);
    }



    [Fact]
    public async Task RunAsync_honors_custom_header_and_detail_table_names()
    {
        _options.HeaderTableName = "MyHeader";
        _options.DetailTableName = "MyDetail";

        await using (var context = CreateContext())
        {
            await AuditSchemaMigrator.RunAsync(context);
        }

        await using var verify = CreateContext();
        // If table-name overrides flowed into the diff, the entity sets will
        // resolve. If they didn't, EF would still talk to MyHeader/MyDetail and
        // get "no such table" because the migrator created AuditHeader/AuditDetail.
        Assert.Empty(await verify.Set<Entities.AuditHeader>().ToListAsync());
        Assert.Empty(await verify.Set<Entities.AuditDetail>().ToListAsync());
    }
}
#endif
