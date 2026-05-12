namespace Wolfgang.Audit;

/// <summary>
/// Describes a column that an <see cref="IAuditValueSerializer"/> requires on the
/// <c>AuditDetail</c> table.
/// </summary>
/// <remarks>
/// The schema installer queries every registered serializer's <see cref="IAuditValueSerializer.Columns"/>
/// to build provider-appropriate DDL.
/// </remarks>
/// <param name="Name">The column name, e.g. <c>Value</c>, <c>ValueText</c>, <c>ValueBinary</c>.</param>
/// <param name="StorageKind">Whether the column stores text or binary data.</param>
/// <param name="IsNullable">Whether the column is nullable. Defaults to <c>true</c>.</param>
public sealed record AuditValueColumn(string Name, AuditValueStorageKind StorageKind, bool IsNullable = true);
