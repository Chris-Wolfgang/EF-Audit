namespace Wolfgang.Audit;

/// <summary>
/// Supplies stored column values to an <see cref="IAuditValueSerializer"/> during decode.
/// </summary>
public interface IAuditValueReader
{
    /// <summary>
    /// Reads the named column as text. Returns <c>null</c> if the column is <c>NULL</c>
    /// or absent.
    /// </summary>
    string? ReadText(string columnName);
}
