using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Wolfgang.Audit.Internal;

/// <summary>
/// Snapshot of one entity's state captured in <c>SavingChangesAsync</c>; resolved into
/// header + detail rows in <c>SavedChangesAsync</c> once generated keys are populated.
/// </summary>
internal sealed class PendingAuditEntry
{
    public required EntityEntry Entry { get; init; }

    public required AuditOperation Operation { get; init; }

    public required string EntityType { get; init; }

    public required string EntityTable { get; init; }

    public required IReadOnlyList<PendingAuditValue> ChangedValues { get; init; }
}
