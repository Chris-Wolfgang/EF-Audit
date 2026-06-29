using Microsoft.EntityFrameworkCore;
using Wolfgang.AuditTrail.Entities;
using Wolfgang.AuditTrail.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Regression tests for PR #2 review cluster: save-path correctness fixes.
/// </summary>
public class SavePathCorrectnessFixesTests
{
    [Fact]
    public async Task AuditingDbContext_SaveChangesAsync_with_acceptAllChangesOnSuccess_false_throws_when_audit_work_is_pending()
    {
        using var fixture = new AuditFixture();

        await using var context = fixture.CreateContext();
        context.Customers.Add(new Customer { Name = "Refused", Email = "r@example.com" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            context.SaveChangesAsync(acceptAllChangesOnSuccess: false));

        Assert.Contains("acceptAllChangesOnSuccess: false", ex.Message, StringComparison.Ordinal);
        Assert.Contains("AuditingDbContext", ex.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void AuditingDbContext_SaveChanges_with_acceptAllChangesOnSuccess_false_throws_when_audit_work_is_pending()
    {
        using var fixture = new AuditFixture();

        using var context = fixture.CreateContext();
        context.Customers.Add(new Customer { Name = "Refused", Email = "r@example.com" });

        var ex = Assert.Throws<InvalidOperationException>(() =>
            context.SaveChanges(acceptAllChangesOnSuccess: false));

        Assert.Contains("acceptAllChangesOnSuccess: false", ex.Message, StringComparison.Ordinal);
    }



    [Fact]
    public async Task AuditingDbContext_SaveChangesAsync_with_acceptAllChangesOnSuccess_false_is_a_safe_passthrough_when_no_audit_work()
    {
        // No pending audit work means no second-save problem; threading false
        // through to the underlying DbContext.SaveChangesAsync(false) is
        // legitimate and the guard must NOT fire.
        using var fixture = new AuditFixture();

        await using var context = fixture.CreateContext();
        var result = await context.SaveChangesAsync(acceptAllChangesOnSuccess: false);

        Assert.Equal(0, result);
    }



    [Fact]
    public async Task AuditingDbContext_SaveChangesAsync_when_only_NotAudited_property_modified_writes_no_audit_header()
    {
        // Customer.Notes is [NotAudited]. Modifying only Notes (and nothing
        // else) should not produce an Update header — that header would have
        // zero detail rows.
        using var fixture = new AuditFixture();

        await using (var seed = fixture.CreateContext())
        {
            seed.Customers.Add(new Customer { Name = "Alice", Email = "a@example.com" });
            await seed.SaveChangesAsync();
        }

        await using (var update = fixture.CreateContext())
        {
            var customer = await update.Customers.SingleAsync();
            customer.Notes = "private side-channel note";   // [NotAudited]
            await update.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();
        var headers = await verify.Set<AuditHeader>().ToListAsync();
        // Only the seed Insert header. No empty Update.
        Assert.Single(headers);
        Assert.Equal(AuditOperation.Insert, headers[0].Operation);
    }
}
