using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Schema;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Targeted micro-tests to close the last documented coverage gaps before v0.1
/// ships. Each test is named for the production line / branch it exists to hit
/// so future bisects stay fast.
/// </summary>
public class CoverageLiftTests
{
    // ── AuditDetail entity getters / setters ───────────────────────────────
    // Pre-fix the entity sat at 66.6% — the DetailId and Header setters were
    // only written by EF Core's change tracker via reflection, which coverlet
    // doesn't credit. Direct CLR writes through the property accessors.

    [Fact]
    public void AuditDetail_property_round_trip()
    {
        var header = new AuditHeader { HeaderId = Guid.NewGuid() };
        var detail = new AuditDetail
        {
            DetailId   = 42,
            HeaderId   = header.HeaderId,
            Header     = header,
            ColumnName = "Email",
            ValueText  = "alice@example.com",
            ValueType  = "String",
        };

        Assert.Equal(42, detail.DetailId);
        Assert.Equal(header.HeaderId, detail.HeaderId);
        Assert.Same(header, detail.Header);
        Assert.Equal("Email", detail.ColumnName);
        Assert.Equal("alice@example.com", detail.ValueText);
        Assert.Equal("String", detail.ValueType);
    }



    // ── DbContextAuditSchemaExtensions ─────────────────────────────────────

#if NET8_0_OR_GREATER
    [Fact]
    public async Task MigrateAuditSchemaAsync_throws_on_null_context()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            DbContextAuditSchemaExtensions.MigrateAuditSchemaAsync(context: null!));
    }



    [Fact]
    public async Task MigrateAuditSchemaAsync_throws_for_non_relational_provider()
    {
        // Build a DbContextOptions WITHOUT calling any UseX() — no
        // RelationalOptionsExtension lands in options.Extensions, so the
        // extension method must throw InvalidOperationException. This is
        // the same effective config a consumer would have with the
        // in-memory provider, without taking on that package as a dep.
        var auditOptions = new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        var dbOpts = new DbContextOptionsBuilder<ProviderlessContext>().Options;
        await using var ctx = new ProviderlessContext(
            dbOpts,
            new StaticAuditUserProvider("u"),
            auditOptions);

        // EF Core throws an InvalidOperationException before our code runs
        // because OnModelCreating needs a provider to resolve relational
        // metadata. The result is the same shape (the call fails loudly when
        // no relational provider is configured) but at the EF Core layer.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ctx.MigrateAuditSchemaAsync());
    }



    private sealed class ProviderlessContext : AuditingDbContext
    {
        public ProviderlessContext
        (
            DbContextOptions<ProviderlessContext> options,
            IAuditUserProvider userProvider,
            AuditOptions auditOptions
        )
            : base(options, userProvider, auditOptions)
        {
        }
    }



    // ── AuditSchemaVersionStore.UpsertInstalledVersionAsync UPDATE branch ──
    // The "existing.Version = version" else-branch only fires on the second
    // Upsert; the first call goes through the INSERT branch.

    [Fact]
    public async Task UpsertInstalledVersionAsync_updates_existing_row_on_second_call()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var auditOptions = new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        var dbOpts = new DbContextOptionsBuilder<AuditMigrationsDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var ctx = new AuditMigrationsDbContext(dbOpts, auditOptions);
        await ctx.Database.EnsureCreatedAsync();

        // First call -> INSERT
        await AuditSchemaVersionStore.UpsertInstalledVersionAsync(ctx, version: 1, CancellationToken.None);
        Assert.Equal(1, await AuditSchemaVersionStore.ReadInstalledVersionAsync(ctx, CancellationToken.None));

        // Second call -> UPDATE (the previously-uncovered branch)
        await AuditSchemaVersionStore.UpsertInstalledVersionAsync(ctx, version: 2, CancellationToken.None);
        Assert.Equal(2, await AuditSchemaVersionStore.ReadInstalledVersionAsync(ctx, CancellationToken.None));
    }
#endif



    // ── AuditCapture.TryCaptureEntry skips AuditHeader entries ─────────────

    [Fact]
    public async Task CapturePending_skips_AuditHeader_entries_in_change_tracker()
    {
        using var fixture = new AuditFixture();
        await using var context = fixture.CreateContext();

        // Manually add an AuditHeader to the change tracker. TryCaptureEntry's
        // "if (clrType == AuditHeader || AuditDetail) return null" guard must
        // skip it so we don't audit the audit table itself.
        context.Set<AuditHeader>().Add(new AuditHeader
        {
            HeaderId      = Guid.NewGuid(),
            TransactionId = Guid.NewGuid(),
            AuditedAtUtc  = DateTime.UtcNow,
            UserId        = "manual",
            EntityType    = "Manual",
            EntityTable   = "Manual",
            EntityKey     = "1",
            Operation     = AuditOperation.Insert,
        });
        // Plus a real entity so the save actually does audit work.
        context.Customers.Add(new Customer { Name = "Real", Email = "r@example.com" });

        await context.SaveChangesAsync();

        await using var verify = fixture.CreateContext();
        // Two AuditHeader rows expected: the manual one + the audit row for
        // the Customer Insert. The manual header was NOT itself audited.
        Assert.Equal(2, await verify.Set<AuditHeader>().CountAsync());
    }



    // ── PipeDelimitedEntityKeySerializer: DateOnly / TimeOnly ──────────────
    // Added in the round-trip cluster fix but never exercised in tests.

    [Fact]
    public void Serialize_DateOnly_uses_round_trip_format()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

#pragma warning disable S3257 // Explicit object?[] — see PipeDelimitedEntityKeySerializerTests note.
        var result = sut.Serialize(new object?[] { new DateOnly(2026, 6, 22) });
#pragma warning restore S3257

        // "o" round-trip for DateOnly is ISO 8601 yyyy-MM-dd.
        Assert.Equal("2026-06-22", result);
    }



    [Fact]
    public void Serialize_TimeOnly_uses_round_trip_format()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

#pragma warning disable S3257
        var result = sut.Serialize(new object?[] { new TimeOnly(14, 30, 45) });
#pragma warning restore S3257

        // "o" round-trip for TimeOnly includes ticks padding.
        Assert.StartsWith("14:30:45", result, StringComparison.Ordinal);
    }




}
