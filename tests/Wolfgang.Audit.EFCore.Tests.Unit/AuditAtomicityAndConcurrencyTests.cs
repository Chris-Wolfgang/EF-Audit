using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;

public class AuditAtomicityAndConcurrencyTests
{
    [Fact]
    public async Task SaveChangesWithAuditAsync_when_value_serializer_throws_rolls_back_the_user_save()
    {
        // SaveChangesWithAuditAsync owns the transaction via
        // IExecutionStrategy.ExecuteInTransactionAsync, so the user save and the
        // audit save share one transaction. If the serializer throws while building
        // audit rows, the entire transaction rolls back — even without the consumer
        // opening their own.
        var options = new AuditOptions
        {
            ValueSerializer = new FailingAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        var userProvider = new StaticAuditUserProvider("test-user");

        DbConnection connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        try
        {
            TestDbContext NewContext() => new(
                new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(connection)
                    .Options,
                options);

            await using (var seed = NewContext())
            {
                await seed.Database.EnsureCreatedAsync();
            }

            await using (var ctx = NewContext())
            {
                ctx.Customers.Add(new Customer { Name = "Alice" });
                await Assert.ThrowsAsync<InvalidOperationException>(
                    () => ctx.SaveChangesWithAuditAsync(userProvider, options));
            }

            await using var verify = NewContext();
            Assert.Empty(await verify.Customers.ToListAsync());
            Assert.Empty(await verify.Set<AuditHeader>().ToListAsync());
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }

    [Fact]
    public async Task SaveChangesWithAuditAsync_when_called_repeatedly_each_save_gets_a_distinct_TransactionId()
    {
        using var fixture = new AuditFixture();

        for (var i = 0; i < 3; i++)
        {
            await using var ctx = fixture.CreateContext();
            ctx.Customers.Add(new Customer { Name = $"User{i}" });
            await fixture.SaveAsync(ctx);
        }

        await using var verify = fixture.CreateContext();
        var headers = await verify.Set<AuditHeader>().ToListAsync();

        Assert.Equal(3, headers.Count);
        Assert.Equal(3, headers.Select(h => h.TransactionId).Distinct().Count());
    }

    [Fact]
    public async Task SaveChangesWithAuditAsync_uses_the_consumer_supplied_IAuditEntityKeySerializer()
    {
        var options = new AuditOptions
        {
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new JsonEntityKeySerializer(),
        };
        var userProvider = new StaticAuditUserProvider("test-user");

        DbConnection connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        try
        {
            TestDbContext NewContext() => new(
                new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(connection)
                    .Options,
                options);

            await using (var seed = NewContext())
            {
                await seed.Database.EnsureCreatedAsync();
            }

            await using (var ctx = NewContext())
            {
                ctx.OrderLines.Add(new OrderLine { OrderId = 7, LineNumber = 3, Description = "Widget" });
                await ctx.SaveChangesWithAuditAsync(userProvider, options);
            }

            await using var verify = NewContext();
            var header = await verify.Set<AuditHeader>().SingleAsync();
            Assert.Equal("[7,3]", header.EntityKey);
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
