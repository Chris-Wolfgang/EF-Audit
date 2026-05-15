using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Internal;

namespace Wolfgang.Audit;

/// <summary>
/// Public entry point for the audit library. Wraps the consumer's
/// <c>SaveChangesAsync</c> in a transaction managed by EF Core's
/// <see cref="IExecutionStrategy"/>, captures Insert / Update / Delete operations
/// via <c>ChangeTracker</c>, and persists header + detail rows into the audit
/// tables — all atomically.
/// </summary>
/// <remarks>
/// <para>
/// This is intentionally a method consumers must call rather than an interceptor
/// that hooks into the default <c>SaveChangesAsync</c>. The reason is atomicity:
/// EF Core's implicit transaction is committed <em>before</em> <c>SavedChangesAsync</c>
/// fires, so an interceptor cannot guarantee that audit rows commit in the same
/// transaction as the user's data (see efcore#37131). Owning the transaction at
/// the call site is the canonical pattern recommended by the EF Core team.
/// </para>
/// </remarks>
public static class DbContextAuditExtensions
{
    /// <summary>
    /// Saves changes to <paramref name="context"/> together with the audit rows
    /// produced by the configured <see cref="IAuditUserProvider"/>,
    /// <see cref="IAuditValueSerializer"/>, and <see cref="IAuditEntityKeySerializer"/>.
    /// The user save and the audit save run inside a single transaction; if either
    /// throws, the whole unit of work rolls back.
    /// </summary>
    /// <param name="context">The consumer's <see cref="DbContext"/>.</param>
    /// <param name="userProvider">Supplies the <see cref="AuditUser"/> stamped on every header.</param>
    /// <param name="options">Audit configuration including the value / entity-key serializers.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written by the user's save (does not include audit rows).</returns>
    /// <exception cref="ArgumentNullException">If any argument is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">If <see cref="AuditOptions.ValueSerializer"/> or <see cref="AuditOptions.EntityKeySerializer"/> is <c>null</c>.</exception>
    public static Task<int> SaveChangesWithAuditAsync(
        this DbContext context,
        IAuditUserProvider userProvider,
        AuditOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(userProvider);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ValueSerializer is null)
        {
            throw new ArgumentException("AuditOptions.ValueSerializer must be set.", nameof(options));
        }

        if (options.EntityKeySerializer is null)
        {
            throw new ArgumentException("AuditOptions.EntityKeySerializer must be set.", nameof(options));
        }

        // Generate the TransactionId once at the top of the call so we have a stable
        // identifier for verifySucceeded to look up if the execution strategy retries
        // after a transient commit failure.
        var auditTransactionId = Guid.NewGuid();

        // If the consumer is already inside a transaction, don't wrap in an execution
        // strategy (retrying strategies refuse user-initiated transactions). The save
        // and audit run in the existing transaction; commit/rollback remains the
        // consumer's responsibility.
        if (context.Database.CurrentTransaction is not null)
        {
            return ExecuteSaveAndAuditAsync(context, userProvider, options, auditTransactionId, cancellationToken);
        }

        var strategy = context.Database.CreateExecutionStrategy();
        var state = (context, userProvider, options, auditTransactionId);

        // ExecuteInTransactionAsync (the strategy-aware variant) opens a transaction
        // around the operation, commits on success, retries on transient failure, and
        // calls `verifySucceeded` between retries to detect the case where the commit
        // actually succeeded but the response was lost (so we don't double-write the
        // audit history). verifySucceeded queries for headers with our pre-generated
        // TransactionId — if any exist, the previous attempt already committed and
        // the strategy should not retry.
        return strategy.ExecuteInTransactionAsync(
            state: state,
            operation: static (s, ct) =>
                ExecuteSaveAndAuditAsync(s.context, s.userProvider, s.options, s.auditTransactionId, ct),
            verifySucceeded: static async (s, ct) =>
                await s.context.Set<AuditHeader>()
                    .AsNoTracking()
                    .AnyAsync(h => h.TransactionId == s.auditTransactionId, ct)
                    .ConfigureAwait(false),
            cancellationToken);
    }

    private static async Task<int> ExecuteSaveAndAuditAsync(
        DbContext context,
        IAuditUserProvider userProvider,
        AuditOptions options,
        Guid auditTransactionId,
        CancellationToken cancellationToken)
    {
        // Capture pending audit state BEFORE the user save runs. Updates and Deletes
        // need this because EF Core resets IsModified / detaches the entry once the
        // save completes; for Inserts we re-read the (now DB-generated) PK after the
        // save via the surviving EntityEntry reference (which is now Unchanged).
        var pending = AuditCapture.CapturePending(context, options);

        var result = await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        if (pending.Count > 0)
        {
            AuditCapture.AddAuditEntities(context, pending, userProvider, options, auditTransactionId);
            await context
                .SaveChangesAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }
}
