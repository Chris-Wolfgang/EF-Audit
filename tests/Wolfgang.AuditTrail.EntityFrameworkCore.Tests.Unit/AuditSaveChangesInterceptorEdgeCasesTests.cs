using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Wolfgang.AuditTrail.Internal;
using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Tests for paths not reached by the main interceptor suite: constructor
/// guards, the sync SavingChanges defensive-cleanup catch, the suppressed-
/// context early-out in SaveChangesFailed, and the
/// <c>acceptAllChangesOnSuccess: false</c> rejection.
/// </summary>
public class AuditSaveChangesInterceptorEdgeCasesTests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Roslynator", "RCS1163", Justification = "Test stub matching the messageGenerator signature.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Sonar", "S3257",     Justification = "Test stub matching the messageGenerator signature.")]
    private static string FormatNothing(Microsoft.EntityFrameworkCore.Diagnostics.EventDefinitionBase definition, EventData eventData)
        => string.Empty;


    private static AuditOptions ValidOptions() => new()
    {
        ValueSerializer     = new StringAuditValueSerializer(),
        EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
    };



    // ── Constructor guards ──────────────────────────────────────────────────

    [Fact]
    public void Constructor_throws_when_user_provider_is_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AuditSaveChangesInterceptor(userProvider: null!, ValidOptions()));
    }



    [Fact]
    public void Constructor_throws_when_options_is_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AuditSaveChangesInterceptor(new StaticAuditUserProvider("u"), options: null!));
    }



    [Fact]
    public void Constructor_throws_when_value_serializer_is_null()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new AuditSaveChangesInterceptor(
                new StaticAuditUserProvider("u"),
                new AuditOptions
                {
                    ValueSerializer     = null,
                    EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
                }));
        Assert.Contains("ValueSerializer", ex.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Constructor_throws_when_entity_key_serializer_is_null()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            new AuditSaveChangesInterceptor(
                new StaticAuditUserProvider("u"),
                new AuditOptions
                {
                    ValueSerializer     = new StringAuditValueSerializer(),
                    EntityKeySerializer = null,
                }));
        Assert.Contains("EntityKeySerializer", ex.Message, StringComparison.Ordinal);
    }



    // ── Sync SavingChanges defensive cleanup catch ──────────────────────────

    [Fact]
    public void SaveChanges_sync_when_retrying_strategy_throws_in_Begin_releases_interceptor_state()
    {
        // EnsureNonRetryingStrategy throws because the configured strategy
        // reports RetriesOnFailure=true. The interceptor's `try / catch` in
        // SavingChanges must call AbortAudit and rethrow. The throw is the
        // observable; the AbortAudit branch is the coverage target.
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var options = ValidOptions();
        var interceptor = new AuditSaveChangesInterceptor(new StaticAuditUserProvider("u"), options);

        // Seed via a context without the fake strategy — EnsureCreated would
        // otherwise route through the strategy's throwing Execute method.
        var seedBuilder = new DbContextOptionsBuilder<InterceptorTestDbContext>()
            .UseSqlite(connection);
        using (var seed = new InterceptorTestDbContext(seedBuilder.Options, options))
        {
            seed.Database.EnsureCreated();
        }

        var failBuilder = new DbContextOptionsBuilder<InterceptorTestDbContext>()
            .UseSqlite(connection)
            .ReplaceService<IExecutionStrategyFactory, FakeRetryingExecutionStrategyFactory>()
            .AddInterceptors(interceptor);

        using var ctx = new InterceptorTestDbContext(failBuilder.Options, options);
        ctx.Customers.Add(new Customer { Name = "Boom" });

        var ex = Assert.Throws<InvalidOperationException>(() => ctx.SaveChanges());

        // Pin each segment of the actionable message so a formatting change is caught.
        Assert.Contains("cannot open an audit transaction", ex.Message, StringComparison.Ordinal);
        Assert.Contains("enables retries on failure", ex.Message, StringComparison.Ordinal);
        Assert.Contains("EnableRetryOnFailure", ex.Message, StringComparison.Ordinal);
        Assert.Contains("inherit your DbContext from", ex.Message, StringComparison.Ordinal);
        Assert.Contains("AuditingDbContext", ex.Message, StringComparison.Ordinal);
        Assert.Contains("strategy.ExecuteAsync", ex.Message, StringComparison.Ordinal);
        Assert.Contains("ExecuteInTransactionAsync", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Database.CurrentTransaction", ex.Message, StringComparison.Ordinal);
    }



    // ── SaveChangesFailed suppressed-context early-out ──────────────────────

    [Fact]
    public void SaveChangesFailed_when_context_is_suppressed_returns_without_aborting()
    {
        // Suppression is the documented escape hatch for nested SaveChanges
        // calls inside the audit pass; the failed-hook must respect it and
        // exit immediately. Drives the `IsSuppressed(context)` early-return
        // (line 231 in the interceptor).
        const string SuppressKey = "Wolfgang.AuditTrail.Suppress";

        using var fixture = new InterceptorFixture();
        var interceptor = new AuditSaveChangesInterceptor(fixture.UserProvider, fixture.Options);

        using var ctx = fixture.CreateContext();
        ctx.SetItem(SuppressKey, value: true);

        var eventData = new DbContextErrorEventData(
            eventDefinition:    null!,
            messageGenerator:   FormatNothing,
            context:            ctx,
            exception:          new InvalidOperationException("ignored"));

        // No throw expected — early-out is the entire point.
        interceptor.SaveChangesFailed(eventData);
    }



    [Fact]
    public async Task SaveChangesFailedAsync_when_context_is_suppressed_returns_without_aborting()
    {
        const string SuppressKey = "Wolfgang.AuditTrail.Suppress";

        using var fixture = new InterceptorFixture();
        var interceptor = new AuditSaveChangesInterceptor(fixture.UserProvider, fixture.Options);

        await using var ctx = fixture.CreateContext();
        ctx.SetItem(SuppressKey, value: true);

        var eventData = new DbContextErrorEventData(
            eventDefinition:    null!,
            messageGenerator:   FormatNothing,
            context:            ctx,
            exception:          new InvalidOperationException("ignored"));

        await interceptor.SaveChangesFailedAsync(eventData);
    }



    // ── acceptAllChangesOnSuccess: false rejection ─────────────────────────

    [Fact]
    public async Task SaveChangesAsync_with_acceptAllChangesOnSuccess_false_throws_with_actionable_message()
    {
        using var fixture = new InterceptorFixture();

        await using var ctx = fixture.CreateContext();
        ctx.Customers.Add(new Customer { Name = "Refused", Email = "r@example.com" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ctx.SaveChangesAsync(acceptAllChangesOnSuccess: false));

        Assert.Contains("does not support", ex.Message, StringComparison.Ordinal);
        Assert.Contains("acceptAllChangesOnSuccess: false", ex.Message, StringComparison.Ordinal);
        Assert.Contains("re-emit the still-dirty user entries", ex.Message, StringComparison.Ordinal);
        Assert.Contains("without the false override", ex.Message, StringComparison.Ordinal);
        Assert.Contains("inherit your DbContext from AuditingDbContext", ex.Message, StringComparison.Ordinal);
        Assert.Contains("through both passes correctly", ex.Message, StringComparison.Ordinal);
    }
}
