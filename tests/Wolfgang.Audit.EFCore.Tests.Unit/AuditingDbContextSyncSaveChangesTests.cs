using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Mirrors the async <see cref="AuditingDbContextSaveChangesTests"/> against
/// the synchronous <c>SaveChanges()</c> path so that the sync override on
/// <see cref="AuditingDbContext"/> is exercised. Issue #30 noted that 40% of
/// the AuditingDbContext lines were unreached because every other test uses
/// <c>SaveChangesAsync</c>.
/// </summary>
public class AuditingDbContextSyncSaveChangesTests
{
    [Fact]
    public void SaveChanges_when_inserting_a_customer_writes_header_and_detail_rows()
    {
        using var fixture = new AuditFixture();

        using (var context = fixture.CreateContext())
        {
            context.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            context.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        var header = verify.Set<AuditHeader>().Include(h => h.Details).Single();
        Assert.Equal(AuditOperation.Insert, header.Operation);
        Assert.Equal("test-user", header.UserId);

        var detailsByColumn = header.Details.ToDictionary(d => d.ColumnName, StringComparer.Ordinal);
        Assert.Equal("Alice", detailsByColumn["Name"].ValueText);
        Assert.Equal("alice@example.com", detailsByColumn["Email"].ValueText);
    }



    [Fact]
    public void SaveChanges_when_updating_a_customer_writes_one_detail_row_per_changed_column()
    {
        using var fixture = new AuditFixture();

        using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            seed.SaveChanges();
        }

        using (var update = fixture.CreateContext())
        {
            var customer = update.Customers.Single();
            customer.Email = "alice@new.example.com";
            update.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        var updateHeader = verify.Set<AuditHeader>()
            .Include(h => h.Details)
            .Single(h => h.Operation == AuditOperation.Update);

        var detail = Assert.Single(updateHeader.Details);
        Assert.Equal("Email", detail.ColumnName);
        Assert.Equal("alice@new.example.com", detail.ValueText);
    }



    [Fact]
    public void SaveChanges_when_deleting_a_customer_writes_header_only_by_default()
    {
        using var fixture = new AuditFixture(captureDeletedValues: false);

        using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            seed.SaveChanges();
        }

        using (var delete = fixture.CreateContext())
        {
            var customer = delete.Customers.Single();
            delete.Customers.Remove(customer);
            delete.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        var deleteHeader = verify.Set<AuditHeader>()
            .Include(h => h.Details)
            .Single(h => h.Operation == AuditOperation.Delete);

        Assert.Empty(deleteHeader.Details);
    }



    [Fact]
    public void SaveChanges_when_capturing_deleted_values_writes_pre_delete_detail_rows()
    {
        using var fixture = new AuditFixture(captureDeletedValues: true);

        using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice", Email = "alice@example.com" });
            seed.SaveChanges();
        }

        using (var delete = fixture.CreateContext())
        {
            var customer = delete.Customers.Single();
            delete.Customers.Remove(customer);
            delete.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        var deleteHeader = verify.Set<AuditHeader>()
            .Include(h => h.Details)
            .Single(h => h.Operation == AuditOperation.Delete);

        var detailsByColumn = deleteHeader.Details.ToDictionary(d => d.ColumnName, StringComparer.Ordinal);
        Assert.Equal("Alice",                detailsByColumn["Name"].ValueText);
        Assert.Equal("alice@example.com",    detailsByColumn["Email"].ValueText);
    }



    [Fact]
    public void SaveChanges_when_saving_multiple_entities_shares_one_TransactionId()
    {
        using var fixture = new AuditFixture();

        using (var context = fixture.CreateContext())
        {
            context.Customers.Add(new Customer { Name = "Alice", Email = "a@example.com" });
            context.Customers.Add(new Customer { Name = "Bob",   Email = "b@example.com" });
            context.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        var headers = verify.Set<AuditHeader>().ToList();
        Assert.Equal(2, headers.Count);
        Assert.Single(headers.Select(h => h.TransactionId).Distinct());
    }



    [Fact]
    public void SaveChanges_when_no_changes_is_a_safe_no_op()
    {
        using var fixture = new AuditFixture();

        using var context = fixture.CreateContext();
        var result = context.SaveChanges();

        Assert.Equal(0, result);
        Assert.Empty(context.Set<AuditHeader>().ToList());
    }



    [Fact]
    public void SaveChanges_when_user_transaction_already_open_runs_inline_and_respects_consumer_commit()
    {
        // Exercises the "Database.CurrentTransaction is not null" branch in
        // SaveChanges: the execution-strategy wrap is skipped, the audit save
        // runs inline, and the consumer owns commit/rollback.
        using var fixture = new AuditFixture();

        using (var context = fixture.CreateContext())
        {
            using var tx = context.Database.BeginTransaction();
            context.Customers.Add(new Customer { Name = "Alice", Email = "a@example.com" });
            context.SaveChanges();
            tx.Commit();
        }

        using var verify = fixture.CreateContext();
        Assert.Single(verify.Set<AuditHeader>().ToList());
    }



    [Fact]
    public void SaveChanges_when_user_transaction_rolled_back_writes_nothing()
    {
        using var fixture = new AuditFixture();

        using (var context = fixture.CreateContext())
        {
            using var tx = context.Database.BeginTransaction();
            context.Customers.Add(new Customer { Name = "Alice", Email = "a@example.com" });
            context.SaveChanges();
            tx.Rollback();
        }

        using var verify = fixture.CreateContext();
        Assert.Empty(verify.Set<AuditHeader>().ToList());
        Assert.Empty(verify.Customers.ToList());
    }
}
