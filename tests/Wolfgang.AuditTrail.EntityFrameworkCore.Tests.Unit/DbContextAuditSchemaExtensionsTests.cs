#if NET8_0_OR_GREATER
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.AuditTrail.Entities;
using Wolfgang.AuditTrail.Schema;
using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Covers <see cref="DbContextAuditSchemaExtensions.MigrateAuditSchemaAsync"/>
/// — the consumer-facing convenience that runs <see cref="AuditSchemaMigrator"/>
/// against an <see cref="AuditingDbContext"/>'s own connection without forcing
/// the caller to construct an <see cref="AuditMigrationsDbContext"/> by hand.
/// </summary>
public sealed class DbContextAuditSchemaExtensionsTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");



    public DbContextAuditSchemaExtensionsTests()
    {
        _connection.Open();
    }



    public void Dispose() => _connection.Dispose();



    private TestDbContext CreateContext(AuditOptions? options = null)
    {
        var auditOptions = options ?? new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new TestDbContext(dbOptions, new StaticAuditUserProvider("test-user"), auditOptions);
    }



    [Fact]
    public async Task MigrateAuditSchemaAsync_creates_audit_schema_on_consumer_connection()
    {
        await using var context = CreateContext();

        await context.MigrateAuditSchemaAsync();

        // Re-open with a verification context that knows only the audit model.
        await using var verify = CreateContext();
        Assert.Empty(await verify.Set<AuditHeader>().ToListAsync());
    }



    [Fact]
    public async Task MigrateAuditSchemaAsync_dryRun_returns_script_without_creating_tables()
    {
        await using var context = CreateContext();

        var script = await context.MigrateAuditSchemaAsync(dryRun: true);

        Assert.False(string.IsNullOrWhiteSpace(script));
        Assert.Contains("AuditHeader", script, StringComparison.Ordinal);

        // No tables were created — the audit set lookup should fail.
        await using var verify = CreateContext();
        var ex = await Record.ExceptionAsync(async () => await verify.Set<AuditHeader>().ToListAsync());
        Assert.NotNull(ex);
    }



    [Fact]
    public async Task MigrateAuditSchemaAsync_throws_on_null_context()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DbContextAuditSchemaExtensions.MigrateAuditSchemaAsync(context: null!));
    }
}
#endif
