namespace Wolfgang.Audit.Internal;

/// <summary>
/// Per-SaveChanges state carried between the <c>Saving</c> and <c>Saved</c> hooks. The
/// <see cref="IsAuditSave"/> flag suppresses recursion when the interceptor performs
/// its own second-phase save inside <c>SavedChangesAsync</c>.
/// </summary>
internal sealed class AuditSaveState
{
    public bool IsAuditSave { get; set; }

    public Guid TransactionId { get; init; }

    public IReadOnlyList<PendingAuditEntry> Pending { get; init; } = Array.Empty<PendingAuditEntry>();
}
