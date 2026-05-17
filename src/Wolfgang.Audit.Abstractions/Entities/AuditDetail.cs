using System;

namespace Wolfgang.Audit.Entities;

/// <summary>
/// One row per changed column on the associated <see cref="AuditHeader"/>. The
/// discriminator in <see cref="ValueType"/> identifies the CLR type the value
/// represents.
/// </summary>
public class AuditDetail
{
    /// <summary>Primary key (identity).</summary>
    public long DetailId { get; set; }

    /// <summary>Foreign key to the owning <see cref="AuditHeader"/>.</summary>
    public Guid HeaderId { get; set; }

    /// <summary>Navigation to the owning header.</summary>
    public AuditHeader? Header { get; set; }

    /// <summary>Name of the changed column on the audited entity.</summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Culture-invariant text representation of the captured column value, encoded
    /// by the active <see cref="IAuditValueSerializer"/>. For Insert and Update this
    /// is the new (post-change) value; for Delete with
    /// <c>AuditOptions.CaptureDeletedValues = true</c> this is the pre-delete
    /// (original) value. <c>null</c> when the audited value was itself <c>null</c>.
    /// </summary>
    public string? ValueText { get; set; }

    /// <summary>
    /// Discriminator identifying the CLR type of the value: <c>String</c>, <c>Int32</c>,
    /// <c>DateTime</c>, <c>Guid</c>, <c>Enum:&lt;FullName&gt;</c>, etc.
    /// </summary>
    public string ValueType { get; set; } = string.Empty;
}
