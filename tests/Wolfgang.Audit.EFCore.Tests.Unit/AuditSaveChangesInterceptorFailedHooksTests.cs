using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Exercises the failure hooks (<see cref="ISaveChangesInterceptor.SaveChangesFailed"/> /
/// <see cref="ISaveChangesInterceptor.SaveChangesFailedAsync"/>) on
/// <see cref="AuditSaveChangesInterceptor"/>, which clear interceptor state so
/// the next save on the same context starts clean. Issue #30 noted these
/// methods were 0% covered.
/// </summary>
public class AuditSaveChangesInterceptorFailedHooksTests
{
    [Fact]
    public async Task SaveChangesFailedAsync_after_a_throwing_save_lets_the_next_save_succeed()
    {
        // The interceptor uses a per-context state bag. If the failure path
        // doesn't clear it, a subsequent SaveChanges on the same context would
        // surface "audit already in progress" / a stale pending list. Drives
        // the failure path by using a serializer that throws during Encode,
        // then runs a clean save and asserts it wrote audit rows normally.
        var failing = new FailingAuditValueSerializer();
        using var fixture = new InterceptorFixture();
        fixture.Options.ValueSerializer = failing;

        await using var ctx = fixture.CreateContext();

        ctx.Customers.Add(new Customer { Name = "Trigger", Email = "t@example.com" });
        await Assert.ThrowsAnyAsync<Exception>(() => ctx.SaveChangesAsync());

        // Detach any tracked entities so they don't re-throw on the next save.
        foreach (var entry in ctx.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        // Switch back to a working serializer and retry on the SAME context.
        fixture.Options.ValueSerializer = new Wolfgang.Audit.Serializers.StringAuditValueSerializer();
        ctx.Customers.Add(new Customer { Name = "Recovered", Email = "r@example.com" });
        await ctx.SaveChangesAsync();

        await using var verify = fixture.CreateContext();
        Assert.Single(await verify.Customers.ToListAsync());
        Assert.Single(await verify.Set<AuditHeader>().ToListAsync());
    }



    [Fact]
    public void SaveChangesFailed_after_a_throwing_save_lets_the_next_save_succeed()
    {
        // Sync counterpart — exercises the sync ISaveChangesInterceptor.SaveChangesFailed.
        var failing = new FailingAuditValueSerializer();
        using var fixture = new InterceptorFixture();
        fixture.Options.ValueSerializer = failing;

        using var ctx = fixture.CreateContext();

        ctx.Customers.Add(new Customer { Name = "Trigger", Email = "t@example.com" });
        Assert.ThrowsAny<Exception>(() => ctx.SaveChanges());

        foreach (var entry in ctx.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        fixture.Options.ValueSerializer = new Wolfgang.Audit.Serializers.StringAuditValueSerializer();
        ctx.Customers.Add(new Customer { Name = "Recovered", Email = "r@example.com" });
        ctx.SaveChanges();

        using var verify = fixture.CreateContext();
        Assert.Single(verify.Customers.ToList());
        Assert.Single(verify.Set<AuditHeader>().ToList());
    }



    [Fact]
    public async Task SaveChangesFailedAsync_throws_on_null_event_data()
    {
        using var fixture = new InterceptorFixture();
        // Round-trip an interceptor instance the way Build wires it.
        var interceptor = new AuditSaveChangesInterceptor(fixture.UserProvider, fixture.Options);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            interceptor.SaveChangesFailedAsync(eventData: null!));
    }



    [Fact]
    public void SaveChangesFailed_throws_on_null_event_data()
    {
        using var fixture = new InterceptorFixture();
        var interceptor = new AuditSaveChangesInterceptor(fixture.UserProvider, fixture.Options);

        Assert.Throws<ArgumentNullException>(() =>
            interceptor.SaveChangesFailed(eventData: null!));
    }
}
