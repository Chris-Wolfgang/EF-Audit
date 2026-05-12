using Wolfgang.Audit.Entities;

namespace Wolfgang.Audit.Internal;

/// <summary>
/// Bridges an <see cref="IAuditValueSerializer"/> back onto the strongly-typed
/// <see cref="AuditDetail"/> columns. Routes <c>ValueText</c> / <c>ValueBinary</c>
/// writes onto the matching properties.
/// </summary>
internal sealed class ColumnValueWriter : IAuditValueWriter
{
    private readonly AuditDetail _detail;

    public ColumnValueWriter(AuditDetail detail)
    {
        _detail = detail;
    }

    public void WriteText(string columnName, string? value)
    {
        _detail.ValueText = value;
    }

    public void WriteBinary(string columnName, byte[]? value)
    {
        _detail.ValueBinary = value;
    }
}
