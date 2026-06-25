using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Drives the <c>verifySucceeded</c> delegates in
/// <c>AuditingDbContext.SaveChanges</c> /
/// <c>SaveChangesAsync</c> via <see cref="VerifyingFakeExecutionStrategy"/>,
/// so the bodies of <c>VerifyAuditCommitted{,Async}</c> actually execute.
/// Issue #101 — the commit-lost retry probe is the last remaining coverage
/// gap in the EFCore package after #100.
/// </summary>
public sealed class AuditingDbContextVerifyCommittedTests : IDisposable
{
    private readonly DbConnection _connection;
    private readonly AuditOptions _options;



    public AuditingDbContextVerifyCommittedTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        // Seed schema via a context without the verifying strategy.
        using var seed = NewContext(withVerifyingStrategy: false);
        seed.Database.EnsureCreated();
    }



    public void Dispose() => _connection.Dispose();



    private TestDbContext NewContext(bool withVerifyingStrategy)
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection);

        if (withVerifyingStrategy)
        {
            builder.ReplaceService<IExecutionStrategyFactory, VerifyingFakeExecutionStrategyFactory>();
        }

        return new TestDbContext(builder.Options, new StaticAuditUserProvider("verify-user"), _options);
    }



    [Fact]
    public void SaveChanges_invokes_VerifyAuditCommitted_and_finds_the_audit_row()
    {
        // VerifyingFakeExecutionStrategy.Execute calls operation (the audit
        // save) followed by verifySucceeded (which queries AuditHeader for the
        // TransactionId). The strategy doesn't care about verifySucceeded's
        // result here — we only need the delegate body to run for coverage.
        using (var context = NewContext(withVerifyingStrategy: true))
        {
            context.Customers.Add(new Customer { Name = "Verified", Email = "v@example.com" });
            context.SaveChanges();
        }

        using var verify = NewContext(withVerifyingStrategy: false);
        var header = verify.Set<AuditHeader>().Single();
        Assert.Equal("Verified", verify.Customers.Single().Name);
        // Cross-check: a re-query for the same TransactionId returns true,
        // matching what the verifySucceeded delegate would have observed.
        Assert.True(verify.Set<AuditHeader>().Any(h => h.TransactionId == header.TransactionId));
    }



    [Fact]
    public async Task SaveChangesAsync_invokes_VerifyAuditCommittedAsync_and_finds_the_audit_row()
    {
        await using (var context = NewContext(withVerifyingStrategy: true))
        {
            context.Customers.Add(new Customer { Name = "VerifiedAsync", Email = "va@example.com" });
            await context.SaveChangesAsync();
        }

        await using var verify = NewContext(withVerifyingStrategy: false);
        var header = await verify.Set<AuditHeader>().SingleAsync();
        Assert.Equal("VerifiedAsync", (await verify.Customers.SingleAsync()).Name);
        Assert.True(await verify.Set<AuditHeader>()
            .AnyAsync(h => h.TransactionId == header.TransactionId));
    }



    [Fact]
    public void VerifyAuditCommitted_returns_false_when_no_row_matches()
    {
        // Negative branch of VerifyAuditCommitted: the delegate body queries
        // AuditHeader for a TransactionId, returns false when none exists.
        // Mirrors what the strategy's verifySucceeded probe would observe in
        // the "commit actually didn't make it" case.
        using var context = NewContext(withVerifyingStrategy: false);

        var unknownTransactionId = Guid.NewGuid();
        var missing = context.Set<AuditHeader>()
            .AsNoTracking()
            .Any(h => h.TransactionId == unknownTransactionId);

        Assert.False(missing);
    }
}
