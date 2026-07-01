using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.AuditTrail.Schema;
using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.Entities;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Behavioural tests for <see cref="AuditSchemaVersionStore"/> — pins the
/// read-latest ordering, the insert-vs-update upsert branch, and the
/// table-not-found → 0 fallback. Targets surviving mutants in the version store.
/// </summary>
public sealed class AuditSchemaVersionStoreBehaviorTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AuditOptions _options;

    public AuditSchemaVersionStoreBehaviorTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new AuditOptions
        {
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
    }

    public void Dispose() => _connection.Dispose();

    private AuditMigrationsDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<AuditMigrationsDbContext>().UseSqlite(_connection);
        return new AuditMigrationsDbContext(builder.Options, _options);
    }



    [Fact]
    public async Task ReadInstalledVersion_returns_the_highest_version_when_multiple_rows_exist()
    {
        await using (var ctx = CreateContext())
        {
            await ctx.Database.EnsureCreatedAsync();
            // Two rows with distinct Ids (key is ValueGeneratedNever). The store
            // must return the MAX version — OrderByDescending, not OrderBy.
            ctx.Set<AuditSchemaVersion>().Add(new AuditSchemaVersion { Id = 1, Version = 2 });
            ctx.Set<AuditSchemaVersion>().Add(new AuditSchemaVersion { Id = 2, Version = 9 });
            ctx.Set<AuditSchemaVersion>().Add(new AuditSchemaVersion { Id = 3, Version = 5 });
            await ctx.SaveChangesAsync();
        }

        await using var read = CreateContext();
        var version = await AuditSchemaVersionStore.ReadInstalledVersionAsync(read, default);
        Assert.Equal(9, version);
    }



    [Fact]
    public async Task ReadInstalledVersion_returns_zero_when_no_rows()
    {
        await using var ctx = CreateContext();
        await ctx.Database.EnsureCreatedAsync();

        Assert.Equal(0, await AuditSchemaVersionStore.ReadInstalledVersionAsync(ctx, default));
    }



    [Fact]
    public async Task ReadInstalledVersion_returns_zero_when_version_table_missing()
    {
        // No EnsureCreated — the version table does not exist, so the query throws
        // a "table not found" DbException that the store must swallow to 0.
        await using var ctx = CreateContext();
        Assert.Equal(0, await AuditSchemaVersionStore.ReadInstalledVersionAsync(ctx, default));
    }



    [Fact]
    public async Task Upsert_inserts_a_new_row_when_none_exists()
    {
        await using (var ctx = CreateContext())
        {
            await ctx.Database.EnsureCreatedAsync();
            await AuditSchemaVersionStore.UpsertInstalledVersionAsync(ctx, 3, default);
        }

        await using var read = CreateContext();
        Assert.Equal(3, await AuditSchemaVersionStore.ReadInstalledVersionAsync(read, default));
        Assert.Equal(1, await read.Set<AuditSchemaVersion>().CountAsync());
    }



    [Fact]
    public async Task Upsert_updates_the_existing_row_rather_than_inserting_a_second()
    {
        await using (var ctx = CreateContext())
        {
            await ctx.Database.EnsureCreatedAsync();
            await AuditSchemaVersionStore.UpsertInstalledVersionAsync(ctx, 3, default);
        }

        await using (var ctx = CreateContext())
        {
            await AuditSchemaVersionStore.UpsertInstalledVersionAsync(ctx, 7, default);
        }

        await using var read = CreateContext();
        Assert.Equal(7, await AuditSchemaVersionStore.ReadInstalledVersionAsync(read, default));
        Assert.Equal(1, await read.Set<AuditSchemaVersion>().CountAsync());
    }
}
