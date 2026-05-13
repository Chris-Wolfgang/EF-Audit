using Wolfgang.Audit.Entities;

namespace Wolfgang.Audit.Internal;

/// <summary>
/// Bridges an <see cref="IAuditValueSerializer"/> back onto the strongly-typed
/// <see cref="AuditDetail"/> columns. Routes writes by column name to the matching
/// property; rejects unknown column names so a misconfigured serializer fails fast
/// instead of silently dropping its output.
/// </summary>
internal sealed class ColumnValueWriter : IAuditValueWriter
{
    private const string ValueTextColumn = "ValueText";
    private const string ValueBinaryColumn = "ValueBinary";

    private readonly AuditDetail _detail;

    public ColumnValueWriter(AuditDetail detail)
    {
        _detail = detail;
    }

    public void WriteText(string columnName, string? value)
    {
        if (!string.Equals(columnName, ValueTextColumn, System.StringComparison.Ordinal))
        {
            throw new System.ArgumentException(
                $"Unknown text column '{columnName}'. The AuditDetail entity exposes '{ValueTextColumn}'.",
                nameof(columnName));
        }

        _detail.ValueText = value;
    }

    public void WriteBinary(string columnName, byte[]? value)
    {
        if (!string.Equals(columnName, ValueBinaryColumn, System.StringComparison.Ordinal))
        {
            throw new System.ArgumentException(
                $"Unknown binary column '{columnName}'. The AuditDetail entity exposes '{ValueBinaryColumn}'.",
                nameof(columnName));
        }

        _detail.ValueBinary = value;
    }
}
