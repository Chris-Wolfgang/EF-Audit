using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;

public class AuditSaveChangesWithAuditTests
{
    [Fact]
    public async Task SaveChangesWithAuditAsync_when_inserting_a_customer_writes_header_and_detail_rows()
    {
        using var fixture = new AuditFixture();

        await using (var context = fixture.CreateContext())
        {
            context.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            await fixture.SaveAsync(context);
        }

        await using var verify = fixture.CreateContext();
        var header = await verify.Set<AuditHeader>().Include(h => h.Details).SingleAsync();
        Assert.Equal(AuditOperation.Insert, header.Operation);
        Assert.Equal("test-user", header.UserId);
        Assert.Null(header.OnBehalfOfUserId);
        Assert.Contains("Customer", header.EntityType, StringComparison.Ordinal);
        Assert.Equal("1", header.EntityKey);

        var detailsByColumn = header.Details.ToDictionary(d => d.ColumnName, StringComparer.Ordinal);
        Assert.Equal("Alice", detailsByColumn["Name"].ValueText);
        Assert.Equal("alice@example.com", detailsByColumn["Email"].ValueText);
        Assert.DoesNotContain("Notes", detailsByColumn.Keys);
    }

    [Fact]
    public async Task SaveChangesWithAuditAsync_when_updating_a_customer_writes_one_detail_row_per_changed_column()
    {
        using var fixture = new AuditFixture();

        await using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            await fixture.SaveAsync(seed);
        }

        await using (var update = fixture.CreateContext())
        {
            var customer = await update.Customers.SingleAsync();
            customer.Email = "alice@new.example.com";
            await fixture.SaveAsync(update);
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
    public async Task SaveChangesWithAuditAsync_when_deleting_writes_header_with_no_details_by_default()
    {
        using var fixture = new AuditFixture();

        await using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice" });
            await fixture.SaveAsync(seed);
        }

        await using (var delete = fixture.CreateContext())
        {
            var customer = await delete.Customers.SingleAsync();
            delete.Customers.Remove(customer);
            await fixture.SaveAsync(delete);
        }

        await using var verify = fixture.CreateContext();
        var deleteHeader = await verify
            .Set<AuditHeader>()
            .Include(h => h.Details)
            .Where(h => h.Operation == AuditOperation.Delete)
            .SingleAsync();

        Assert.Empty(deleteHeader.Details);
    }

    [Fact]
    public async Task SaveChangesWithAuditAsync_when_CaptureDeletedValues_is_true_writes_pre_delete_detail_rows()
    {
        using var fixture = new AuditFixture(captureDeletedValues: true);

        await using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            await fixture.SaveAsync(seed);
        }

        await using (var delete = fixture.CreateContext())
        {
            var customer = await delete.Customers.SingleAsync();
            delete.Customers.Remove(customer);
            await fixture.SaveAsync(delete);
        }

        await using var verify = fixture.CreateContext();
        var deleteHeader = await verify
            .Set<AuditHeader>()
            .Include(h => h.Details)
            .Where(h => h.Operation == AuditOperation.Delete)
            .SingleAsync();

        var detailsByColumn = deleteHeader.Details.ToDictionary(d => d.ColumnName, StringComparer.Ordinal);
        Assert.Equal("Alice", detailsByColumn["Name"].ValueText);
        Assert.Equal("alice@example.com", detailsByColumn["Email"].ValueText);
    }

    [Fact]
    public async Task SaveChangesWithAuditAsync_when_entity_has_NotAudited_attribute_writes_no_header_for_it()
    {
        using var fixture = new AuditFixture();

        await using (var context = fixture.CreateContext())
        {
            context.CacheEntries.Add(new CacheEntry { Payload = "x" });
            await fixture.SaveAsync(context);
        }

        await using var verify = fixture.CreateContext();
        Assert.Empty(await verify.Set<AuditHeader>().ToListAsync());
    }

    [Fact]
    public async Task SaveChangesWithAuditAsync_when_saving_multiple_entities_groups_them_under_one_TransactionId()
    {
        using var fixture = new AuditFixture();

        await using (var context = fixture.CreateContext())
        {
            context.Customers.Add(new Customer { Name = "Alice" });
            context.Customers.Add(new Customer { Name = "Bob" });
            await fixture.SaveAsync(context);
        }

        await using var verify = fixture.CreateContext();
        var headers = await verify.Set<AuditHeader>().ToListAsync();
        Assert.Equal(2, headers.Count);
        Assert.Single(headers.Select(h => h.TransactionId).Distinct());
    }
}
