using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Integration.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Integration;

/// <summary>
/// Shared integration-test body. One concrete subclass per provider supplies the
/// fixture (SQL Server / PostgreSQL / MySQL) via <see cref="IClassFixture{TFixture}"/>.
/// </summary>
public abstract class ProviderAuditTestsBase<TFixture> : IClassFixture<TFixture>
    where TFixture : class, IProviderFixture
{
    private readonly TFixture _fixture;

    protected ProviderAuditTestsBase(TFixture fixture)
    {
        _fixture = fixture;
    }

    private (TestDbContext Context, AuditOptions Options) NewContext()
    {
        var options = new AuditOptions
        {
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        var interceptor = new AuditSaveChangesInterceptor(new StaticAuditUserProvider("test-user"), options);
        var context = new TestDbContext(_fixture.CreateContextOptions(interceptor), options);
        return (context, options);
    }

    [Fact]
    public async Task SaveChangesAsync_when_inserting_writes_a_header_with_the_generated_primary_key()
    {
        int customerId;
        var (context, _) = NewContext();
        await using (context)
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            var alice = new Customer { Name = "Alice", Email = "alice@example.com" };
            context.Customers.Add(alice);
            await context.SaveChangesAsync();
            customerId = alice.CustomerId;
        }

        var (verify, _) = NewContext();
        await using (verify)
        {
            var header = await verify.Set<AuditHeader>().Include(h => h.Details).SingleAsync();
            Assert.Equal(AuditOperation.Insert, header.Operation);
            Assert.Equal(
                customerId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                header.EntityKey);

            var details = header.Details.ToDictionary(d => d.ColumnName, StringComparer.Ordinal);
            Assert.Equal("Alice", details["Name"].ValueText);
            Assert.Equal("alice@example.com", details["Email"].ValueText);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_when_user_transaction_rolls_back_writes_no_audit_rows()
    {
        var (context, _) = NewContext();
        await using (context)
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            await using var tx = await context.Database.BeginTransactionAsync();
            context.Customers.Add(new Customer { Name = "Alice" });
            await context.SaveChangesAsync();
            await tx.RollbackAsync();
        }

        var (verify, _) = NewContext();
        await using (verify)
        {
            Assert.Empty(await verify.Set<AuditHeader>().ToListAsync());
        }
    }

    [Fact]
    public async Task SaveChangesAsync_when_inserting_multiple_entities_shares_one_TransactionId()
    {
        var (context, _) = NewContext();
        await using (context)
        {
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            context.Customers.Add(new Customer { Name = "Alice" });
            context.Customers.Add(new Customer { Name = "Bob" });
            await context.SaveChangesAsync();
        }

        var (verify, _) = NewContext();
        await using (verify)
        {
            var headers = await verify.Set<AuditHeader>().ToListAsync();
            Assert.Equal(2, headers.Count);
            Assert.Single(headers.Select(h => h.TransactionId).Distinct());
        }
    }
}
