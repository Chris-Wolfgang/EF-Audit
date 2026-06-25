namespace Wolfgang.Audit;

/// <summary>
/// Describes a column that an <see cref="IAuditValueSerializer"/> requires on the
/// <c>AuditDetail</c> table.
/// </summary>
/// <remarks>
/// v1 declares only text columns. Future serializers (binary, hybrid) will extend
/// this type with a storage-kind discriminator when they ship.
/// </remarks>
/// <param name="Name">The column name, e.g. <c>ValueText</c>.</param>
/// <param name="IsNullable">Whether the column is nullable. Defaults to <c>true</c>.</param>
public sealed record AuditValueColumn(string Name, bool IsNullable = true);
