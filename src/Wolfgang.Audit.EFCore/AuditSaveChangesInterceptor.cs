using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Internal;

namespace Wolfgang.Audit;



/// <summary>
/// Auto-transaction <see cref="ISaveChangesInterceptor"/> that captures Insert /
/// Update / Delete operations via <see cref="DbContext.ChangeTracker"/> and writes
/// audit header + detail rows in the same transaction as the user's save.
/// </summary>
/// <remarks>
/// <para>
/// Use this when your <see cref="DbContext"/> already inherits from a third-party
/// base (such as <c>IdentityDbContext&lt;TUser&gt;</c>, a multi-tenant base, or an
/// internal enterprise base) and cannot also inherit from
/// <see cref="AuditingDbContext"/>. Register via:
/// </para>
/// <code>
/// services.AddEfCoreAuditing&lt;MyUserProvider&gt;();
/// services.AddDbContext&lt;AppDbContext&gt;((sp, opts) =&gt; opts
///     .UseSqlServer(connStr)
///     .UseAuditing(sp));
/// </code>
/// <para>
/// <strong>Cancellation.</strong> If the <see cref="CancellationToken"/> is
/// signalled while <c>SavingChangesAsync</c> is opening the audit transaction,
/// the interceptor catches the resulting <c>OperationCanceledException</c>,
/// disposes the owned transaction, and re-raises — no leak. Cancellation that
/// fires later (during EF's own SQL execution between
/// <c>SavingChangesAsync</c> and <c>SavedChangesAsync</c>) is handled via
/// <c>SaveChangesFailedAsync</c> on EF Core 6 / pre-7-cancel-callback versions;
/// on EF Core 7+ that routes through <c>SaveChangesCanceledAsync</c> instead,
/// which this interceptor doesn't yet hook because that method isn't on the
/// EF Core 6 <c>ISaveChangesInterceptor</c> surface. In that narrow window the
/// owned transaction is disposed when the <see cref="DbContext"/> is.
/// Inherit from <see cref="AuditingDbContext"/> for full cancellation
/// robustness — it owns the <c>SaveChangesAsync</c> override and can clean up
/// on every exit path.
/// </para>
/// <para>
/// <strong>acceptAllChangesOnSuccess caveat.</strong> The interceptor does not
/// support <c>SaveChanges(acceptAllChangesOnSuccess: false)</c>: when the caller
/// passes <c>false</c>, EF leaves user entries in their dirty state and the
/// interceptor's audit-pass save would re-emit them. The interceptor detects
/// this case at runtime and throws <see cref="InvalidOperationException"/>
/// pointing at <see cref="AuditingDbContext"/>, which threads the value through
/// both passes correctly.
/// </para>
/// <para>
/// <strong>Retry-strategy caveat.</strong> EF Core's retrying execution strategies
/// (e.g. <c>SqlServerRetryingExecutionStrategy</c> from
/// <c>EnableRetryOnFailure</c>) refuse user-initiated transactions opened outside
/// a strategy delegate. <em>Important:</em> <c>strategy.ExecuteAsync(...)</c> by
/// itself does NOT open a transaction — <c>Database.CurrentTransaction</c>
/// remains <c>null</c> inside it, and this interceptor would still try to
/// open its own owned transaction (then throw via <c>EnsureNonRetryingStrategy</c>).
/// The supported integration is either:
/// <list type="bullet">
///   <item>Call <c>strategy.ExecuteInTransactionAsync(...)</c> so the strategy
///         opens a transaction the interceptor can enlist in, OR</item>
///   <item>Inside your <c>ExecuteAsync</c> delegate, call
///         <c>context.Database.BeginTransactionAsync(...)</c> yourself before
///         <c>SaveChangesAsync</c>, OR</item>
///   <item>Inherit from <see cref="AuditingDbContext"/> instead — it composes
///         with retrying strategies correctly out of the box.</item>
/// </list>
/// </para>
/// <para>
/// <strong>Rollback / ChangeTracker divergence.</strong> If the audit-pass
/// <c>SaveChanges</c> throws after the user-pass <c>SaveChanges</c> has
/// already accepted the user entries (entries are now <c>Unchanged</c>),
/// rolling back the owned transaction puts the database back to the
/// pre-save state but the in-memory <c>ChangeTracker</c> still reflects
/// the accepted state. The interceptor calls
/// <see cref="Microsoft.EntityFrameworkCore.ChangeTracking.ChangeTracker.Clear"/>
/// on the rollback path so a stale-state reuse fails loudly the next save —
/// but the safer pattern is to dispose the <see cref="DbContext"/> after a
/// save failure and create a new one.
/// </para>
/// </remarks>
public sealed class AuditSaveChangesInterceptor : ISaveChangesInterceptor
{
    private const string PendingItemsKey = "Wolfgang.Audit.Pending";
    private const string OwnedTxItemsKey = "Wolfgang.Audit.OwnedTransaction";
    private const string TxIdItemsKey    = "Wolfgang.Audit.TransactionId";
    private const string SuppressItemsKey = "Wolfgang.Audit.Suppress";

    private readonly IAuditUserProvider _userProvider;
    private readonly AuditOptions _options;



    /// <summary>
    /// Initializes a new <see cref="AuditSaveChangesInterceptor"/>.
    /// </summary>
    public AuditSaveChangesInterceptor
    (
        IAuditUserProvider userProvider,
        AuditOptions options
    )
    {
        _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
        _options = options ?? throw new ArgumentNullException(nameof(options));

        if (_options.ValueSerializer is null)
        {
            throw new ArgumentException("AuditOptions.ValueSerializer must be set.", nameof(options));
        }

        if (_options.EntityKeySerializer is null)
        {
            throw new ArgumentException("AuditOptions.EntityKeySerializer must be set.", nameof(options));
        }
    }



    /// <inheritdoc/>
    public InterceptionResult<int> SavingChanges
    (
        DbContextEventData eventData,
        InterceptionResult<int> result
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context is null || IsSuppressed(context))
        {
            return result;
        }

        try
        {
            BeginAudit(context);
        }
        catch
        {
            // Defensive cleanup mirroring SavingChangesAsync — see comment there.
            AbortAudit(context);
            throw;
        }

        return result;
    }



    /// <inheritdoc/>
    public async ValueTask<InterceptionResult<int>> SavingChangesAsync
    (
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context is null || IsSuppressed(context))
        {
            return result;
        }

        try
        {
            await BeginAuditAsyncCore(context, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Defensive cleanup: if BeginAuditAsync throws (most commonly
            // OperationCanceledException via cancellationToken between opening the
            // owned transaction and the rest of EF's save pipeline), we own the
            // transaction at this point and EF won't necessarily call
            // SaveChangesFailedAsync (EF Core 7+ routes cancellation through the
            // separate SaveChangesCanceledAsync hook). AbortAudit disposes the
            // transaction and clears the per-context state so nothing leaks.
            await AbortAuditAsync(context, CancellationToken.None).ConfigureAwait(false);
            throw;
        }

        return result;
    }



    private async Task BeginAuditAsyncCore(DbContext context, CancellationToken cancellationToken)
    {
        var pending = AuditCapture.CapturePending(context, _options);
        context.SetItem(PendingItemsKey, pending);
        context.SetItem(TxIdItemsKey, Guid.NewGuid());

        // Symmetric short-circuit with BeginAudit (sync): no pending audit
        // entries means no audit transaction.
        if (pending.Count > 0 && context.Database.CurrentTransaction is null)
        {
            EnsureNonRetryingStrategy(context);
            var tx = await context.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
            context.SetItem(OwnedTxItemsKey, tx);
        }
    }



    /// <inheritdoc/>
    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context is null || IsSuppressed(context))
        {
            return result;
        }

        FinishAudit(context);
        return result;
    }



    /// <inheritdoc/>
    public async ValueTask<int> SavedChangesAsync
    (
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context is null || IsSuppressed(context))
        {
            return result;
        }

        await FinishAuditAsync(context, cancellationToken).ConfigureAwait(false);
        return result;
    }



    /// <inheritdoc/>
    public void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context is null || IsSuppressed(context))
        {
            return;
        }

        AbortAudit(context);
    }



    /// <inheritdoc/>
    public async Task SaveChangesFailedAsync
    (
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context is null || IsSuppressed(context))
        {
            return;
        }

        await AbortAuditAsync(context, cancellationToken).ConfigureAwait(false);
    }



    private void BeginAudit(DbContext context)
    {
        // Snapshot ChangeTracker BEFORE the user save runs so we can read pre-save
        // PK values for Deletes and IsModified flags for Updates.
        var pending = AuditCapture.CapturePending(context, _options);
        context.SetItem(PendingItemsKey, pending);
        context.SetItem(TxIdItemsKey, Guid.NewGuid());

        // No audit work means no transaction. Opening an owned BEGIN/COMMIT
        // when CapturePending returns empty just burns a database round-trip
        // on every no-audit save. FinishAudit already short-circuits on
        // pending.Count == 0, so the symmetric guard here keeps the two paths
        // aligned.
        if (pending.Count > 0 && context.Database.CurrentTransaction is null)
        {
            EnsureNonRetryingStrategy(context);
            var tx = context.Database.BeginTransaction();
            context.SetItem(OwnedTxItemsKey, tx);
        }
    }



    private void FinishAudit(DbContext context)
    {
        var pending = context.GetItem<List<PendingAuditEntry>>(PendingItemsKey);
        var txId    = context.GetItem<Guid>(TxIdItemsKey);
        var ownedTx = context.GetItem<IDbContextTransaction>(OwnedTxItemsKey);

        try
        {
            if (pending is { Count: > 0 })
            {
                EnsureUserEntriesAreSettled(context);
                AuditCapture.AddAuditEntities(context, pending, _userProvider, _options, txId);

                context.SetItem(SuppressItemsKey, value: true);
                try     { context.SaveChanges(); }
                finally { context.RemoveItem(SuppressItemsKey); }
            }

            ownedTx?.Commit();
        }
        catch
        {
            ownedTx?.Rollback();
            // ChangeTracker still reflects the user-pass-accepted state but the
            // DB just got rolled back. Clear so a downstream reuse can't
            // silently no-op a re-save of stale entries. See class XML doc.
            context.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            ownedTx?.Dispose();
            ClearItems(context);
        }
    }



    private async Task FinishAuditAsync(DbContext context, CancellationToken cancellationToken)
    {
        var pending = context.GetItem<List<PendingAuditEntry>>(PendingItemsKey);
        var txId    = context.GetItem<Guid>(TxIdItemsKey);
        var ownedTx = context.GetItem<IDbContextTransaction>(OwnedTxItemsKey);

        try
        {
            if (pending is { Count: > 0 })
            {
                EnsureUserEntriesAreSettled(context);
                AuditCapture.AddAuditEntities(context, pending, _userProvider, _options, txId);

                context.SetItem(SuppressItemsKey, value: true);
                try
                {
                    await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    context.RemoveItem(SuppressItemsKey);
                }
            }

            if (ownedTx is not null)
            {
                await ownedTx.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            if (ownedTx is not null)
            {
                await ownedTx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
            // ChangeTracker still reflects the user-pass-accepted state but
            // the DB just got rolled back. Clear so a downstream reuse can't
            // silently no-op a re-save of stale entries. See class XML doc.
            context.ChangeTracker.Clear();
            throw;
        }
        finally
        {
            if (ownedTx is not null)
            {
                await ownedTx.DisposeAsync().ConfigureAwait(false);
            }
            ClearItems(context);
        }
    }



#if NET8_0_OR_GREATER
    /// <summary>
    /// EF Core 7+ raises this when a save is cancelled mid-flight (after
    /// SavingChangesAsync but before SavedChangesAsync). On net6 / EF Core 6
    /// the same cleanup happens through SaveChangesFailedAsync; this method
    /// only exists on EF Core 7+ targets so we gate it on net8+.
    /// </summary>
    /// <inheritdoc/>
    public async Task SaveChangesCanceledAsync
    (
        DbContextEventData eventData,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(eventData);
        var context = eventData.Context;
        if (context is null || IsSuppressed(context))
        {
            return;
        }

        // CancellationToken.None — the supplied token is already cancelled
        // and we still need to dispose the owned transaction synchronously.
        await AbortAuditAsync(context, CancellationToken.None).ConfigureAwait(false);
    }
#endif



    private static void AbortAudit(DbContext context)
    {
        var ownedTx = context.GetItem<IDbContextTransaction>(OwnedTxItemsKey);
        try     { ownedTx?.Rollback(); }
        catch   { /* swallow — original SaveChanges exception is propagating */ }
        finally
        {
            ownedTx?.Dispose();
            ClearItems(context);
        }
    }



    private static async Task AbortAuditAsync(DbContext context, CancellationToken cancellationToken)
    {
        var ownedTx = context.GetItem<IDbContextTransaction>(OwnedTxItemsKey);
        try
        {
            if (ownedTx is not null)
            {
                await ownedTx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch
        {
            // swallow — original SaveChanges exception is propagating
        }
        finally
        {
            if (ownedTx is not null)
            {
                await ownedTx.DisposeAsync().ConfigureAwait(false);
            }
            ClearItems(context);
        }
    }



    private static void EnsureNonRetryingStrategy(DbContext context)
    {
        var strategy = context.Database.CreateExecutionStrategy();
        if (strategy.RetriesOnFailure)
        {
            throw new InvalidOperationException
            (
                "AuditSaveChangesInterceptor cannot open an audit transaction because " +
                "the configured execution strategy enables retries on failure (e.g. " +
                "EnableRetryOnFailure). Either inherit your DbContext from " +
                "AuditingDbContext (which composes correctly with retrying strategies), " +
                "or open the transaction yourself inside strategy.ExecuteAsync(...) " +
                "(for example via strategy.ExecuteInTransactionAsync(...)) so the " +
                "interceptor sees Database.CurrentTransaction and enlists in it."
            );
        }
    }



    private static void EnsureUserEntriesAreSettled(DbContext context)
    {
        // After the user's SaveChanges completes successfully, EF leaves the
        // user-data entries in Unchanged state when acceptAllChangesOnSuccess is
        // true (the default). If the caller passed false, the entries remain in
        // Added/Modified/Deleted and our internal audit-pass SaveChanges would
        // re-emit them. The interceptor cannot safely roundtrip that semantic, so
        // detect and fail loudly pointing at AuditingDbContext (which composes
        // correctly because it owns the override and can thread the value through).
        var stillDirty = context.ChangeTracker
            .Entries()
            .Any(e => e.Entity is not AuditHeader
                   && e.Entity is not AuditDetail
                   && e.State is EntityState.Added
                                or EntityState.Modified
                                or EntityState.Deleted);

        if (stillDirty)
        {
            throw new InvalidOperationException
            (
                "AuditSaveChangesInterceptor does not support " +
                "SaveChanges(acceptAllChangesOnSuccess: false) because the inner " +
                "audit-pass save would re-emit the still-dirty user entries. Either " +
                "call SaveChanges() / SaveChangesAsync() without the false override, " +
                "or inherit your DbContext from AuditingDbContext, which routes " +
                "acceptAllChangesOnSuccess through both passes correctly."
            );
        }
    }



    private static bool IsSuppressed(DbContext context)
    {
        return context.GetItem<bool?>(SuppressItemsKey) == true;
    }



    private static void ClearItems(DbContext context)
    {
        context.RemoveItem(PendingItemsKey);
        context.RemoveItem(OwnedTxItemsKey);
        context.RemoveItem(TxIdItemsKey);
    }
}



