using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Internal;
using Wolfgang.Audit.Schema;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Targeted tests for individual lines/branches uncovered after the main
/// expansion in #100. Each test is named for the production line it exists to
/// hit so future bisects stay quick.
/// </summary>
public class SmallCoverageGapsTests
{
    // ── DbContextItemBag ────────────────────────────────────────────────────

    [Fact]
    public void DbContextItemBag_SetItem_with_null_value_removes_key()
    {
        using var fixture = new AuditFixture();
        using var context = fixture.CreateContext();

        context.SetItem("k", "v");
        Assert.Equal("v", context.GetItem<string>("k"));

        context.SetItem("k", value: null);
        Assert.Null(context.GetItem<string>("k"));
    }



    // ── TableNotFoundIndicators ─────────────────────────────────────────────

    [Fact]
    public void TableNotFoundIndicators_returns_false_for_unrelated_DbException_messages()
    {
        // A permission / connectivity failure must NOT be misclassified as
        // "table not found" — otherwise AuditSchemaVersionStore would silently
        // treat a misconfigured environment as a fresh install.
        var ex = new TestDbException("ERROR 18456: Login failed for user 'svc'.", sqlState: "28000");

        var actual = TableNotFoundIndicators.IsTableNotFound(ex);

        Assert.False(actual);
    }



    [Fact]
    public void TableNotFoundIndicators_returns_true_on_sqlite_message()
    {
        var ex = new TestDbException("SQLite Error 1: 'no such table: AuditHeader'.");

        Assert.True(TableNotFoundIndicators.IsTableNotFound(ex));
    }



    // ── AuditCapture: state switch fall-through ─────────────────────────────

    [Fact]
    public void AuditCapture_skips_entities_in_Unchanged_state()
    {
        // The switch in CapturePending maps only Added/Modified/Deleted to an
        // audit operation. Unchanged entries (line 50 catch-all) produce no
        // pending audit entry — exercised by attaching an entity in the
        // Unchanged state and saving a different one.
        using var fixture = new AuditFixture();

        using (var context = fixture.CreateContext())
        {
            context.Customers.Add(new Customer { Name = "Seed", Email = "s@example.com" });
            context.SaveChanges();
        }

        using (var context = fixture.CreateContext())
        {
            // Load and re-attach as Unchanged.
            var existing = context.Customers.AsNoTracking().Single();
            context.Customers.Attach(existing); // Unchanged

            // Plus an Added entity so the save isn't a no-op.
            context.Customers.Add(new Customer { Name = "New", Email = "n@example.com" });
            context.SaveChanges();
        }

        using var verify = fixture.CreateContext();
        // One header for the initial seed, one for the second Add. The Attached
        // (Unchanged) entry produced none.
        Assert.Equal(2, verify.Set<AuditHeader>().Count());
    }



    // ── AuditMigrationsDbContext: custom schema path ────────────────────────
    // The model-cache-key factory that distinguishes these contexts by Options
    // is itself net8+ (Schema/* is #if NET8_OR_GREATER). On net6 the per-type
    // cache would serve the first test's model to the second, so these two
    // tests run only where the cache key disambiguates them.

#if NET8_0_OR_GREATER
    [Fact]
    public void AuditMigrationsDbContext_when_schema_is_set_routes_version_table_under_it()
    {
        // Drives the `if (!string.IsNullOrWhiteSpace(Options.Schema))` branch
        // in OnModelCreating for the version table (line 80).
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var auditOptions = new AuditOptions
        {
            Schema              = "myaudit",
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        var dbOptions = new DbContextOptionsBuilder<AuditMigrationsDbContext>()
            .UseSqlite(connection)
            .Options;

        using var ctx = new AuditMigrationsDbContext(dbOptions, auditOptions);
        var entity = ctx.Model.FindEntityType(typeof(AuditSchemaVersion));
        Assert.NotNull(entity);
        Assert.Equal("myaudit", entity!.GetSchema());
    }



    // ── ModelBuilderExtensions: schema-set branch on Header/Detail ──────────

    [Fact]
    public void ApplyAuditing_when_schema_is_set_routes_header_and_detail_under_it()
    {
        // Drives ModelBuilderExtensions lines 35 + 79 (the "schema present"
        // arm in both ConfigureAuditHeader and ConfigureAuditDetail).
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var auditOptions = new AuditOptions
        {
            Schema              = "schemaonly",
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        var dbOptions = new DbContextOptionsBuilder<AuditMigrationsDbContext>()
            .UseSqlite(connection)
            .Options;

        using var ctx = new AuditMigrationsDbContext(dbOptions, auditOptions);
        var header = ctx.Model.FindEntityType(typeof(AuditHeader));
        var detail = ctx.Model.FindEntityType(typeof(AuditDetail));

        Assert.Equal("schemaonly", header!.GetSchema());
        Assert.Equal("schemaonly", detail!.GetSchema());
    }
#endif



    // ── Test helper for TableNotFoundIndicators ─────────────────────────────

    private sealed class TestDbException : DbException
    {
        public TestDbException(string message, string? sqlState = null) : base(message)
        {
            SqlState = sqlState;
        }

        public override string? SqlState { get; }
    }
}
