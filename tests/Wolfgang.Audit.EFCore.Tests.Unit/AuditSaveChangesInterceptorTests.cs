using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Tests for <see cref="AuditSaveChangesInterceptor"/> — the integration model used
/// by applications whose <see cref="DbContext"/> already inherits from a third-party
/// base and therefore cannot derive from <see cref="AuditingDbContext"/>.
/// </summary>
public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public async Task SaveChangesAsync_when_interceptor_is_registered_writes_audit_rows_for_an_insert()
    {
        using var fixture = new InterceptorFixture();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            await ctx.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var header = await verify.Set<AuditHeader>().Include(h => h.Details).SingleAsync();

        Assert.Equal(AuditOperation.Insert, header.Operation);
        Assert.Equal("test-user", header.UserId);
        Assert.Contains("Customer", header.EntityType, StringComparison.Ordinal);
        Assert.Equal("1", header.EntityKey);

        var detailsByColumn = header.Details.ToDictionary(d => d.ColumnName, StringComparer.Ordinal);
        Assert.Equal("Alice", detailsByColumn["Name"].ValueText);
        Assert.Equal("alice@example.com", detailsByColumn["Email"].ValueText);
    }



    [Fact]
    public async Task SaveChangesAsync_when_interceptor_is_registered_recursion_guard_prevents_auditing_the_audit_rows()
    {
        // If the recursion guard misfires, the second SaveChanges (audit-rows pass)
        // would re-enter the interceptor and try to audit AuditHeader / AuditDetail.
        // A single Customer insert should produce exactly one header row.
        using var fixture = new InterceptorFixture();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Customers.Add(new Customer { Name = "Solo" });
            await ctx.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var headers = await verify.Set<AuditHeader>().ToListAsync();
        Assert.Single(headers);
    }



    [Fact]
    public async Task SaveChangesAsync_when_interceptor_is_registered_writes_audit_rows_for_an_update()
    {
        using var fixture = new InterceptorFixture();

        await using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            await seed.SaveChangesAsync();
        }

        await using (var update = fixture.CreateContext())
        {
            var customer = await update.Customers.SingleAsync();
            customer.Email = "alice@new.example.com";
            await update.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var updateHeader = await verify
            .Set<AuditHeader>()
            .Include(h => h.Details)
            .Where(h => h.Operation == AuditOperation.Update)
            .SingleAsync();

        var detail = Assert.Single(updateHeader.Details);
        Assert.Equal("Email", detail.ColumnName);
        Assert.Equal("alice@new.example.com", detail.ValueText);
    }



    [Fact]
    public async Task SaveChangesAsync_when_interceptor_is_registered_writes_audit_rows_for_a_delete()
    {
        using var fixture = new InterceptorFixture();

        await using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice" });
            await seed.SaveChangesAsync();
        }

        await using (var delete = fixture.CreateContext())
        {
            var customer = await delete.Customers.SingleAsync();
            delete.Customers.Remove(customer);
            await delete.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var deleteHeader = await verify
            .Set<AuditHeader>()
            .Include(h => h.Details)
            .Where(h => h.Operation == AuditOperation.Delete)
            .SingleAsync();

        Assert.Equal("test-user", deleteHeader.UserId);
        Assert.Equal("1", deleteHeader.EntityKey);
        // CaptureDeletedValues defaults to false → no detail rows on delete.
        Assert.Empty(deleteHeader.Details);
    }



    [Fact]
    public async Task SaveChangesAsync_when_interceptor_is_registered_and_user_owns_the_transaction_does_not_open_its_own()
    {
        // When the user has already opened a transaction, the interceptor should
        // enlist in it rather than opening a second one. We prove this by rolling
        // back the user transaction and asserting that nothing — neither user data
        // nor audit rows — was persisted.
        using var fixture = new InterceptorFixture();

        await using (var ctx = fixture.CreateContext())
        {
            await using var tx = await ctx.Database.BeginTransactionAsync();
            ctx.Customers.Add(new Customer { Name = "Alice" });
            await ctx.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        await using var verify = fixture.CreateContext();
        Assert.Empty(await verify.Customers.ToListAsync());
        Assert.Empty(await verify.Set<AuditHeader>().ToListAsync());
    }



    [Fact]
    public async Task SaveChangesAsync_when_interceptor_is_registered_and_strategy_retries_on_failure_throws_InvalidOperationException()
    {
        // The interceptor cannot open its own transaction safely under a retrying
        // execution strategy (the strategy would refuse a user-initiated transaction
        // opened outside its ExecuteAsync wrap). The interceptor detects this at
        // first save and throws a clear error pointing at AuditingDbContext.
        using var fixture = new InterceptorFixture();

        await using var ctx = new InterceptorTestDbContext
        (
            new DbContextOptionsBuilder<InterceptorTestDbContext>()
                .UseSqlite(fixture.GetConnection())
                .AddInterceptors(new AuditSaveChangesInterceptor(fixture.UserProvider, fixture.Options))
                .ReplaceService<IExecutionStrategyFactory, FakeRetryingExecutionStrategyFactory>()
                .Options,
            fixture.Options
        );

        ctx.Customers.Add(new Customer { Name = "Alice" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => ctx.SaveChangesAsync());
        Assert.Contains("AuditingDbContext", ex.Message, StringComparison.Ordinal);
        Assert.Contains("retries on failure", ex.Message, StringComparison.Ordinal);
    }



    [Fact]
    public async Task SaveChangesAsync_when_interceptor_is_registered_multi_entity_save_shares_one_TransactionId()
    {
        using var fixture = new InterceptorFixture();

        await using (var ctx = fixture.CreateContext())
        {
            ctx.Customers.Add(new Customer { Name = "Alice" });
            ctx.Customers.Add(new Customer { Name = "Bob" });
            ctx.Customers.Add(new Customer { Name = "Carol" });
            await ctx.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var headers = await verify.Set<AuditHeader>().ToListAsync();

        Assert.Equal(3, headers.Count);
        Assert.Single(headers.Select(h => h.TransactionId).Distinct());
    }



    [Fact]
    public void SaveChanges_sync_when_interceptor_is_registered_writes_audit_rows_for_an_insert()
    {
        // The interceptor exposes both sync (SavingChanges / SavedChanges /
        // SaveChangesFailed) and async hooks; the async path is covered elsewhere
        // in this class. This test exercises the sync path directly to catch
        // regressions in the synchronous transaction / capture / audit-save flow.
        using var fixture = new InterceptorFixture();

        using (var ctx = fixture.CreateContext())
        {
            ctx.Customers.Add(new Customer { Name = "SyncAlice", Email = "sa@example.com" });
            ctx.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        var header = verify.Set<AuditHeader>().Include(h => h.Details).Single();
        Assert.Equal(AuditOperation.Insert, header.Operation);
        Assert.Equal("test-user", header.UserId);
        var details = header.Details.ToDictionary(d => d.ColumnName, StringComparer.Ordinal);
        Assert.Equal("SyncAlice", details["Name"].ValueText);
        Assert.Equal("sa@example.com", details["Email"].ValueText);
    }



    [Fact]
    public void SaveChanges_sync_when_interceptor_is_registered_and_user_owns_the_transaction_rolls_back_cleanly()
    {
        // Sync counterpart to the async transaction-rollback test. Proves the sync
        // hook path (SavingChanges / SavedChanges) honors an existing user transaction
        // and rolls back user data + audit rows together.
        using var fixture = new InterceptorFixture();

        using (var ctx = fixture.CreateContext())
        {
            using var tx = ctx.Database.BeginTransaction();
            ctx.Customers.Add(new Customer { Name = "SyncRollback" });
            ctx.SaveChanges();
            tx.Rollback();
        }

        using var verify = fixture.CreateContext();
        Assert.Empty(verify.Customers.ToList());
        Assert.Empty(verify.Set<AuditHeader>().ToList());
    }
}
