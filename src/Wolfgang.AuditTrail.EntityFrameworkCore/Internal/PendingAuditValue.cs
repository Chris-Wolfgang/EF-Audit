namespace Wolfgang.AuditTrail.Internal;

internal sealed class PendingAuditValue
{
    public required string ColumnName { get; init; }

    public required Type ClrType { get; init; }

    /// <summary>
    /// CLR property name on the tracked entity. Used to re-read
    /// <c>CurrentValue</c> at materialize time for Inserts/Updates so
    /// database-generated columns (defaults, computed, identity) reflect
    /// the post-save value instead of the pre-save CLR default.
    /// </summary>
    public required string PropertyName { get; init; }

    /// <summary>
    /// Pre-captured value. For Deletes this is the only authoritative source
    /// (the <c>EntityEntry</c> detaches after save). For Inserts/Updates this
    /// is the pre-save value; <c>AddAuditEntities</c> re-reads
    /// <c>CurrentValue</c> after save and prefers that.
    /// </summary>
    public object? Value { get; init; }
}
