using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Internal;

namespace Wolfgang.Audit;



/// <summary>
/// Base <see cref="DbContext"/> that writes audit rows atomically with every
/// <see cref="DbContext.SaveChanges()"/> / <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
/// call. Derive your application's context from this instead of <see cref="DbContext"/>
/// and continue calling <c>SaveChangesAsync</c> as usual — audit capture happens
/// transparently in the same transaction.
/// </summary>
/// <remarks>
/// <para>
/// This is the recommended integration model for new applications and any context
/// that doesn't already derive from a non-<see cref="DbContext"/> base. The override
/// wraps the save in <see cref="IExecutionStrategy"/>'s
/// <c>ExecuteInTransactionAsync</c>, so it composes correctly with
/// <c>EnableRetryOnFailure</c> on cloud providers.
/// </para>
/// <para>
/// For applications that already inherit from <c>IdentityDbContext&lt;TUser&gt;</c>
/// or another third-party base, use the auto-transaction interceptor instead —
/// register it via <c>services.AddEfCoreAuditing&lt;TUserProvider&gt;()</c> and
/// <c>options.UseAuditing(serviceProvider)</c>.
/// </para>
/// </remarks>
public abstract class AuditingDbContext : DbContext
{
    private readonly IAuditUserProvider _userProvider;
    private readonly AuditOptions _auditOptions;

    private bool _isAuditingSave;   // recursion guard for the audit-rows pass



    /// <summary>
    /// Audit configuration applied to this context. Exposed so the schema
    /// migrator can read schema and table-name overrides without going through
    /// the model.
    /// </summary>
    public AuditOptions AuditOptions => _auditOptions;



    /// <summary>
    /// Initializes a new <see cref="AuditingDbContext"/>.
    /// </summary>
    /// <param name="options">EF Core <see cref="DbContextOptions"/>.</param>
    /// <param name="userProvider">Supplies the <see cref="AuditUser"/> stamped on every header.</param>
    /// <param name="auditOptions">Audit configuration including the value / entity-key serializers.</param>
    /// <exception cref="ArgumentNullException">If any argument is <c>null</c>.</exception>
    protected AuditingDbContext
    (
        DbContextOptions options,
        IAuditUserProvider userProvider,
        AuditOptions auditOptions
    )
        : base(options)
    {
        _userProvider = userProvider ?? throw new ArgumentNullException(nameof(userProvider));
        _auditOptions = auditOptions ?? throw new ArgumentNullException(nameof(auditOptions));

        if (_auditOptions.ValueSerializer is null)
        {
            throw new ArgumentException("AuditOptions.ValueSerializer must be set.", nameof(auditOptions));
        }

        if (_auditOptions.EntityKeySerializer is null)
        {
            throw new ArgumentException("AuditOptions.EntityKeySerializer must be set.", nameof(auditOptions));
        }
    }



    /// <summary>
    /// Applies the audit entity mappings. Derived classes that override
    /// <c>OnModelCreating</c> must call <c>base.OnModelCreating(modelBuilder)</c> so
    /// the audit tables are configured.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyAuditing(_auditOptions);
    }



    /// <inheritdoc/>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        if (_isAuditingSave)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        // If nothing in the change tracker produces an audit row, skip the
        // execution-strategy + transaction wrap entirely. Wrapping a no-op save in
        // ExecuteInTransaction's commit-lost detection would be unsafe because
        // verifySucceeded looks for AuditHeader rows tagged with our TransactionId —
        // and none would exist, so a transient commit-lost failure could cause the
        // strategy to retry the user save.
        var pending = AuditCapture.CapturePending(this, _auditOptions);
        if (pending.Count == 0)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        // TransactionId is generated once and threaded through state so a retrying
        // execution strategy can detect "the commit actually succeeded but the response
        // was lost" via VerifyAuditCommitted.
        var auditTransactionId = Guid.NewGuid();

        // Honor an existing user transaction: skip the execution-strategy wrap and
        // just run inline; commit/rollback remains the consumer's responsibility.
        if (Database.CurrentTransaction is not null)
        {
            return SaveWithAuditInline(acceptAllChangesOnSuccess, auditTransactionId, pending);
        }

        var strategy = Database.CreateExecutionStrategy();

        return strategy.ExecuteInTransaction
        (
            state:           (Context: this, AcceptAll: acceptAllChangesOnSuccess, TxId: auditTransactionId, Pending: pending),
            operation:       static s => s.Context.SaveWithAuditInline(s.AcceptAll, s.TxId, s.Pending),
            verifySucceeded: static s => VerifyAuditCommitted(s.Context, s.TxId)
        );
    }



    /// <inheritdoc/>
    public override Task<int> SaveChangesAsync
    (
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        if (_isAuditingSave)
        {
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // See SaveChanges for the no-op short-circuit rationale.
        var pending = AuditCapture.CapturePending(this, _auditOptions);
        if (pending.Count == 0)
        {
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var auditTransactionId = Guid.NewGuid();

        if (Database.CurrentTransaction is not null)
        {
            return SaveWithAuditInlineAsync(acceptAllChangesOnSuccess, auditTransactionId, pending, cancellationToken);
        }

        var strategy = Database.CreateExecutionStrategy();

        return strategy.ExecuteInTransactionAsync
        (
            state:             (Context: this, AcceptAll: acceptAllChangesOnSuccess, TxId: auditTransactionId, Pending: pending),
            operation:         static (s, ct) => s.Context.SaveWithAuditInlineAsync(s.AcceptAll, s.TxId, s.Pending, ct),
            verifySucceeded:   static (s, ct) => VerifyAuditCommittedAsync(s.Context, s.TxId, ct),
            cancellationToken: cancellationToken
        );
    }



    private int SaveWithAuditInline
    (
        bool acceptAllChangesOnSuccess,
        Guid transactionId,
        List<PendingAuditEntry> pending
    )
    {
        var result = base.SaveChanges(acceptAllChangesOnSuccess);

        AuditCapture.AddAuditEntities(this, pending, _userProvider, _auditOptions, transactionId);

        _isAuditingSave = true;
        try     { base.SaveChanges(acceptAllChangesOnSuccess); }
        finally { _isAuditingSave = false; }

        return result;
    }



    private async Task<int> SaveWithAuditInlineAsync
    (
        bool acceptAllChangesOnSuccess,
        Guid transactionId,
        List<PendingAuditEntry> pending,
        CancellationToken cancellationToken
    )
    {
        var result = await base
            .SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
            .ConfigureAwait(false);

        AuditCapture.AddAuditEntities(this, pending, _userProvider, _auditOptions, transactionId);

        _isAuditingSave = true;
        try
        {
            await base
                .SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _isAuditingSave = false;
        }

        return result;
    }



    private static bool VerifyAuditCommitted(DbContext context, Guid transactionId)
    {
        return context.Set<AuditHeader>()
            .AsNoTracking()
            .Any(h => h.TransactionId == transactionId);
    }



    private static Task<bool> VerifyAuditCommittedAsync
    (
        DbContext context,
        Guid transactionId,
        CancellationToken cancellationToken
    )
    {
        return context.Set<AuditHeader>()
            .AsNoTracking()
            .AnyAsync(h => h.TransactionId == transactionId, cancellationToken);
    }
}
