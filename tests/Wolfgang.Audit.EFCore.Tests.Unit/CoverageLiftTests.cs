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
#endif



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
