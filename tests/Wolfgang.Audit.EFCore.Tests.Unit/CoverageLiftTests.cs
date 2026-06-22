using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Internal;
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
        // UseInMemoryDatabase configures a real provider that has no
        // RelationalOptionsExtension — so we get past EF Core's own
        // "must configure a provider" guard and reach OUR throw at line 69.
        var auditOptions = new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        var dbOpts = new DbContextOptionsBuilder<ProviderlessContext>()
            .UseInMemoryDatabase("non-relational-coverage-lift")
            .Options;
        await using var ctx = new ProviderlessContext(
            dbOpts,
            new StaticAuditUserProvider("u"),
            auditOptions);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ctx.MigrateAuditSchemaAsync());

        Assert.Contains("non-relational provider", ex.Message, StringComparison.Ordinal);
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



    // ── SaveChangesCanceledAsync (net8+) ───────────────────────────────────
    // EF Core 7+ added this hook to ISaveChangesInterceptor; the production
    // override routes to AbortAuditAsync. Invoke the interceptor directly
    // with a DbContextEventData so the async state machine actually runs.

#if NET8_0_OR_GREATER
    [Fact]
    public async Task SaveChangesCanceledAsync_with_null_context_returns_without_throwing()
    {
        var sut = new AuditSaveChangesInterceptor(
            new StaticAuditUserProvider("u"),
            new AuditOptions
            {
                ValueSerializer     = new StringAuditValueSerializer(),
                EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
            });

        var eventData = new DbContextEventData(
            eventDefinition:  null!,
            messageGenerator: static (_, _) => string.Empty,
            context:          null);

        await sut.SaveChangesCanceledAsync(eventData);
        // Reaching here means the "context is null" early-return ran cleanly.
    }



    [Fact]
    public async Task SaveChangesCanceledAsync_with_live_context_runs_AbortAuditAsync()
    {
        // No owned transaction in the bag → AbortAuditAsync's null-tx branch
        // runs to completion. This covers the full success path of the cancel
        // hook (the catch-swallow branch is covered by a separate test below).
        using var fixture = new InterceptorFixture();
        await using var ctx = fixture.CreateContext();

        var sut = new AuditSaveChangesInterceptor(fixture.UserProvider, fixture.Options);

        var eventData = new DbContextEventData(
            eventDefinition:  null!,
            messageGenerator: static (_, _) => string.Empty,
            context:          ctx);

        await sut.SaveChangesCanceledAsync(eventData);
    }
#endif



    // ── AbortAuditAsync exception-during-rollback ──────────────────────────
    // The catch block (lines 445-456) is hit when ownedTx.RollbackAsync()
    // itself throws. Plant a fake IDbContextTransaction that throws on
    // RollbackAsync, then trigger the abort via SaveChangesFailedAsync.

    [Fact]
    public async Task AbortAuditAsync_swallows_rollback_exception()
    {
        using var fixture = new InterceptorFixture();
        await using var ctx = fixture.CreateContext();

        var throwingTx = new ThrowingTransaction();
        ctx.SetItem("Wolfgang.Audit.OwnedTransaction", throwingTx);

        var sut = new AuditSaveChangesInterceptor(fixture.UserProvider, fixture.Options);

        var errorData = new DbContextErrorEventData(
            eventDefinition:  null!,
            messageGenerator: static (_, _) => string.Empty,
            context:          ctx,
            exception:        new InvalidOperationException("simulated user-pass failure"));

        // SaveChangesFailedAsync → AbortAuditAsync → throwingTx.RollbackAsync
        // throws → catch swallows → finally disposes. The interceptor must
        // not propagate the rollback exception.
        await sut.SaveChangesFailedAsync(errorData);

        Assert.True(throwingTx.RollbackAttempted);
        Assert.True(throwingTx.Disposed);
        // OwnedTransaction key cleared on the way out.
        Assert.Null(ctx.GetItem<IDbContextTransaction>("Wolfgang.Audit.OwnedTransaction"));
    }



    /// <summary>Minimal IDbContextTransaction that throws on rollback.</summary>
    private sealed class ThrowingTransaction : IDbContextTransaction
    {
        public bool RollbackAttempted { get; private set; }
        public bool Disposed { get; private set; }

        public Guid TransactionId { get; } = Guid.NewGuid();

        public void Commit()  => throw new NotSupportedException();
        public void Rollback()
        {
            RollbackAttempted = true;
            throw new InvalidOperationException("simulated rollback failure");
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RollbackAttempted = true;
            throw new InvalidOperationException("simulated rollback failure");
        }

        public void Dispose() => Disposed = true;
        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }



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
