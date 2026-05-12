using System;

namespace Wolfgang.Audit.Entities;

/// <summary>
/// One row per changed column on the associated <see cref="AuditHeader"/>. The value
/// column(s) populated depend on the configured <see cref="IAuditValueSerializer"/>;
/// the discriminator in <see cref="ValueType"/> identifies the CLR type the value
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
    /// Text representation of the value (populated by serializers that declare a text
    /// column — including v1's <c>StringAuditValueSerializer</c>). <c>null</c> when
    /// the audited value was <c>null</c> or when the serializer routes the value to
    /// <see cref="ValueBinary"/>.
    /// </summary>
    public string? ValueText { get; set; }

    /// <summary>
    /// Binary representation of the value (populated by serializers that declare a
    /// binary column). <c>null</c> for the v1 text-only serializer.
    /// </summary>
    public byte[]? ValueBinary { get; set; }

    /// <summary>
    /// Discriminator identifying the CLR type of the value: <c>String</c>, <c>Int32</c>,
    /// <c>DateTime</c>, <c>Guid</c>, <c>Enum:&lt;FullName&gt;</c>, etc.
    /// </summary>
    public string ValueType { get; set; } = string.Empty;
}
