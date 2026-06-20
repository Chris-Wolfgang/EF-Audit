using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Smoke.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Smoke;

/// <summary>
/// Realistic end-to-end scenario: a single order is inserted, updated several times,
/// then deleted. Asserts that the audit headers + details accurately reconstruct the
/// order's lifecycle in the order events actually happened.
/// </summary>
public class EndToEndHistorySmokeTest
{
    [Fact]
    public async Task An_order_full_lifecycle_is_recoverable_from_the_audit_tables()
    {
        var options = new AuditOptions
        {
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        var userProvider = new StaticAuditUserProvider("smoke-user");

        DbConnection connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();
        try
        {
            SmokeDbContext NewContext() => new(
                new DbContextOptionsBuilder<SmokeDbContext>()
                    .UseSqlite(connection)
                    .Options,
                userProvider,
                options);

            await using (var seed = NewContext())
            {
                await seed.Database.EnsureCreatedAsync();
            }

            int orderId;

            // Each save below stamps AuditHeader.AuditedAtUtc with DateTime.UtcNow at
            // microsecond precision (HasPrecision(6)). Without the small Task.Delay
            // calls below, two consecutive saves on a fast machine can land in the
            // same microsecond — the order-by tie-breaker (HeaderId is a random Guid,
            // not chronological) then leaves the assertion sequence non-deterministic.
            // 10ms is comfortably above any provider's tick granularity.
            const int SaveSpacingMs = 10;

            // 1. Insert.
            await using (var ctx = NewContext())
            {
                var order = new Order { CustomerName = "Alice", Total = 100m };
                ctx.Orders.Add(order);
                await ctx.SaveChangesAsync();
                orderId = order.OrderId;
            }
            await Task.Delay(SaveSpacingMs);

            // 2. Update status: Pending -> Processing.
            await using (var ctx = NewContext())
            {
                var order = await ctx.Orders.SingleAsync();
                order.Status = "Processing";
                await ctx.SaveChangesAsync();
            }
            await Task.Delay(SaveSpacingMs);

            // 3. Update total (price adjustment).
            await using (var ctx = NewContext())
            {
                var order = await ctx.Orders.SingleAsync();
                order.Total = 95m;
                await ctx.SaveChangesAsync();
            }
            await Task.Delay(SaveSpacingMs);

            // 4. Update status: Processing -> Shipped.
            await using (var ctx = NewContext())
            {
                var order = await ctx.Orders.SingleAsync();
                order.Status = "Shipped";
                await ctx.SaveChangesAsync();
            }
            await Task.Delay(SaveSpacingMs);

            // 5. Delete.
            await using (var ctx = NewContext())
            {
                var order = await ctx.Orders.SingleAsync();
                ctx.Orders.Remove(order);
                await ctx.SaveChangesAsync();
            }

            await using var verify = NewContext();
            // With 10ms spacing between saves, AuditedAtUtc is guaranteed unique
            // per header for this test, so no tie-breaker is needed. The previous
            // ThenBy(HeaderId) was misleading: HeaderId is a random Guid, not a
            // chronological successor, so it would order ties incorrectly any time
            // two headers actually did tie. Better to make ties impossible.
            var history = await verify
                .Set<AuditHeader>()
                .Include(h => h.Details)
                .Where(h => h.EntityKey == orderId.ToString(System.Globalization.CultureInfo.InvariantCulture))
                .OrderBy(h => h.AuditedAtUtc)
                .ToListAsync();

            Assert.Equal(5, history.Count);

            Assert.Equal(AuditOperation.Insert, history[0].Operation);
            var insertColumns = history[0].Details.ToDictionary(d => d.ColumnName, d => d.ValueText, StringComparer.Ordinal);
            Assert.Equal("Alice", insertColumns["CustomerName"]);
            Assert.Equal("100", insertColumns["Total"]);
            Assert.Equal("Pending", insertColumns["Status"]);

            Assert.Equal(AuditOperation.Update, history[1].Operation);
            var statusToProcessing = Assert.Single(history[1].Details);
            Assert.Equal("Status", statusToProcessing.ColumnName);
            Assert.Equal("Processing", statusToProcessing.ValueText);

            Assert.Equal(AuditOperation.Update, history[2].Operation);
            var totalUpdate = Assert.Single(history[2].Details);
            Assert.Equal("Total", totalUpdate.ColumnName);
            Assert.Equal("95", totalUpdate.ValueText);

            Assert.Equal(AuditOperation.Update, history[3].Operation);
            var statusToShipped = Assert.Single(history[3].Details);
            Assert.Equal("Status", statusToShipped.ColumnName);
            Assert.Equal("Shipped", statusToShipped.ValueText);

            Assert.Equal(AuditOperation.Delete, history[4].Operation);
            Assert.Empty(history[4].Details);

            Assert.True(history.TrueForAll(h => string.Equals(h.UserId, "smoke-user", StringComparison.Ordinal)));
        }
        finally
        {
            await connection.DisposeAsync();
        }
    }
}
