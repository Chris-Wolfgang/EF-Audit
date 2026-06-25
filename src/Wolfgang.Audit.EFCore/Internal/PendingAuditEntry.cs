using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Wolfgang.Audit.Internal;

/// <summary>
/// Snapshot of one entity's state captured *before* the user's <c>SaveChanges</c> runs.
/// The audit header / detail rows are built from this after the save completes, so
/// database-generated keys can be read back from <see cref="Entry"/> for Inserts and
/// Updates. For Deletes, the <see cref="EntityEntry"/> is detached after the save —
/// <see cref="KeyValuesBeforeSave"/> preserves the primary-key values captured ahead
/// of time.
/// </summary>
internal sealed class PendingAuditEntry
{
    public required EntityEntry Entry { get; init; }

    public required AuditOperation Operation { get; init; }

    public required string EntityType { get; init; }

    public required string EntityTable { get; init; }

    public required IReadOnlyList<PendingAuditValue> ChangedValues { get; init; }

    public required IReadOnlyList<object?> KeyValuesBeforeSave { get; init; }
}
