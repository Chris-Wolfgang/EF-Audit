namespace Wolfgang.Audit;

/// <summary>
/// Receives the serialized form of a value being captured into an
/// <c>AuditDetail</c> row.
/// </summary>
/// <remarks>
/// An <see cref="IAuditValueSerializer"/> writes one or more of the columns it declared
/// via <see cref="IAuditValueSerializer.Columns"/>. Columns not written are left as
/// <c>NULL</c>.
/// </remarks>
public interface IAuditValueWriter
{
    /// <summary>
    /// Writes a text value to the named column.
    /// </summary>
    void WriteText(string columnName, string? value);
}
