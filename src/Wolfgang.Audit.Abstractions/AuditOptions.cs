namespace Wolfgang.Audit;

/// <summary>
/// Configures how the audit interceptor captures changes and how the schema installer
/// creates the audit tables.
/// </summary>
public sealed class AuditOptions
{
    /// <summary>
    /// Database schema for the <c>AuditHeader</c> and <c>AuditDetail</c> tables.
    /// <c>null</c> (the default) uses the provider's default schema: <c>dbo</c> on
    /// SQL Server, <c>public</c> on PostgreSQL, none on SQLite/MySQL.
    /// </summary>
    public string? Schema { get; set; }

    /// <summary>
    /// Name of the header table. Defaults to <c>AuditHeader</c>.
    /// </summary>
    public string HeaderTableName { get; set; } = "AuditHeader";

    /// <summary>
    /// Name of the detail table. Defaults to <c>AuditDetail</c>.
    /// </summary>
    public string DetailTableName { get; set; } = "AuditDetail";

    /// <summary>
    /// When <c>true</c>, <see cref="AuditOperation.Delete"/> operations write detail
    /// rows containing the pre-delete column values. When <c>false</c> (the default),
    /// Delete operations write only a header row.
    /// </summary>
    public bool CaptureDeletedValues { get; set; }

    /// <summary>
    /// The value serializer used by the interceptor to encode column values into
    /// detail rows, and (via the schema installer) to drive the detail table's column
    /// shape. Defaults to <c>StringAuditValueSerializer</c> (set by the registration
    /// extension; may be <c>null</c> if accessed before registration).
    /// </summary>
    public IAuditValueSerializer? ValueSerializer { get; set; }

    /// <summary>
    /// The entity-key serializer used to render primary-key values into the
    /// <c>EntityKey</c> column on <c>AuditHeader</c>. Defaults to
    /// <c>PipeDelimitedEntityKeySerializer</c> (set by the registration extension; may
    /// be <c>null</c> if accessed before registration).
    /// </summary>
    public IAuditEntityKeySerializer? EntityKeySerializer { get; set; }
}
