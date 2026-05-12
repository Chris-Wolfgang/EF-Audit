namespace Wolfgang.Audit.Internal;

internal sealed class PendingAuditValue
{
    public required string ColumnName { get; init; }

    public required Type ClrType { get; init; }

    public object? Value { get; init; }
}
