using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit.Internal;

/// <summary>
/// Bridges an <see cref="IAuditValueSerializer"/> back onto the strongly-typed
/// <see cref="AuditDetail"/> columns. Routes <c>WriteText</c> calls to the
/// named column declared by the active serializer.
/// </summary>
/// <remarks>
/// v1 ships a single <c>ValueText</c> text column (declared by
/// <see cref="StringAuditValueSerializer"/>), so any other column name the
/// writer receives is an interface contract violation — we throw so the
/// custom serializer's bug is loud, not silent. Future binary/hybrid
/// serializers will introduce additional column names; the switch is the
/// extension point.
/// </remarks>
internal sealed class ColumnValueWriter : IAuditValueWriter
{
    private readonly AuditDetail _detail;

    public ColumnValueWriter(AuditDetail detail)
    {
        _detail = detail;
    }

    public void WriteText(string columnName, string? value)
    {
        ArgumentNullException.ThrowIfNull(columnName);

        switch (columnName)
        {
            case StringAuditValueSerializer.ValueColumnName:
                _detail.ValueText = value;
                break;

            default:
                throw new InvalidOperationException(
                    $"IAuditValueWriter.WriteText was called with column name '{columnName}', " +
                    $"but the only column the v1 AuditDetail entity exposes for text values is " +
                    $"'{StringAuditValueSerializer.ValueColumnName}'. Custom IAuditValueSerializer " +
                    $"implementations must either restrict themselves to the v1 columns or wait for " +
                    $"binary/hybrid serializer support that introduces the corresponding columns.");
        }
    }
}
