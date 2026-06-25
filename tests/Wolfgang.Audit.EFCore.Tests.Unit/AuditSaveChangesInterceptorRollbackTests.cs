using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Regression coverage for PR #2 cluster #5 — when the audit pass throws
/// after the user-pass save has already accepted the user entries, the
/// interceptor must roll back the owned transaction AND clear the
/// ChangeTracker so a downstream reuse of the context can't silently
/// no-op a re-save of stale entries (r3255402689).
/// </summary>
public class AuditSaveChangesInterceptorRollbackTests
{
    [Fact]
    public async Task When_audit_pass_throws_async_ChangeTracker_is_cleared()
    {
        using var fixture = new FailingFixture();
        await using var context = fixture.CreateContext();

        context.Customers.Add(new Customer { Name = "fail-me", Email = "f@example.com" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        // Without the Clear, the Customer would still be tracked as Unchanged
        // (EF accepted the user pass) and a re-SaveChanges would no-op while
        // the DB has nothing.
        Assert.Empty(context.ChangeTracker.Entries());
    }



    [Fact]
    public void When_audit_pass_throws_sync_ChangeTracker_is_cleared()
    {
        using var fixture = new FailingFixture();
        using var context = fixture.CreateContext();

        context.Customers.Add(new Customer { Name = "fail-me", Email = "f@example.com" });

        Assert.Throws<InvalidOperationException>(() => context.SaveChanges());
        Assert.Empty(context.ChangeTracker.Entries());
    }



    [Fact]
    public async Task When_audit_pass_throws_user_rows_are_rolled_back_in_database()
    {
        using var fixture = new FailingFixture();
        await using var context = fixture.CreateContext();

        context.Customers.Add(new Customer { Name = "fail-me", Email = "f@example.com" });

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());

        await using var verify = fixture.CreateContext();
        Assert.Empty(await verify.Customers.ToListAsync());
    }



    /// <summary>
    /// Variant of <see cref="InterceptorFixture"/> that wires the failing serializer
    /// so the audit-pass save throws after the user-pass has accepted entries.
    /// </summary>
    private sealed class FailingFixture : IDisposable
    {
        private readonly DbConnection _connection;
        private readonly AuditSaveChangesInterceptor _interceptor;

        public FailingFixture()
        {
            Options = new AuditOptions
            {
                ValueSerializer     = new FailingAuditValueSerializer(),
                EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
            };

            _interceptor = new AuditSaveChangesInterceptor(new StaticAuditUserProvider("u"), Options);

            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            using var seed = CreateContext();
            seed.Database.EnsureCreated();
        }

        public AuditOptions Options { get; }

        public InterceptorTestDbContext CreateContext()
        {
            var builder = new DbContextOptionsBuilder<InterceptorTestDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(_interceptor);
            return new InterceptorTestDbContext(builder.Options, Options);
        }

        public void Dispose() => _connection.Dispose();
    }
}
