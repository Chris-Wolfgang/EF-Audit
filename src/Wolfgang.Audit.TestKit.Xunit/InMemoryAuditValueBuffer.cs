using System.Collections.Generic;

namespace Wolfgang.Audit.TestKit.Xunit;

/// <summary>
/// In-memory <see cref="IAuditValueWriter"/> / <see cref="IAuditValueReader"/> used by
/// the contract tests. Stores the most recent value written to each column and serves
/// it back on read.
/// </summary>
public sealed class InMemoryAuditValueBuffer : IAuditValueWriter, IAuditValueReader
{
    private readonly Dictionary<string, string?> _textColumns = new(System.StringComparer.Ordinal);
    private readonly Dictionary<string, byte[]?> _binaryColumns = new(System.StringComparer.Ordinal);

    /// <inheritdoc />
    public void WriteText(string columnName, string? value)
    {
        _textColumns[columnName] = value;
    }

    /// <inheritdoc />
    public void WriteBinary(string columnName, byte[]? value)
    {
        _binaryColumns[columnName] = value;
    }

    /// <inheritdoc />
    public string? ReadText(string columnName)
    {
        return _textColumns.TryGetValue(columnName, out var value) ? value : null;
    }

    /// <inheritdoc />
    public byte[]? ReadBinary(string columnName)
    {
        return _binaryColumns.TryGetValue(columnName, out var value) ? value : null;
    }
}
