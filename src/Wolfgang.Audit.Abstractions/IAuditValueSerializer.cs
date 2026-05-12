using System;
using System.Collections.Generic;

namespace Wolfgang.Audit;

/// <summary>
/// Encodes values captured by the audit interceptor into the column(s) it persists
/// on the <c>AuditDetail</c> row, and decodes them back on read.
/// </summary>
/// <remarks>
/// <para>
/// The serializer owns the column shape of the <c>AuditDetail</c> table's value
/// column(s) via <see cref="Columns"/>. The schema installer reads this to build
/// provider-appropriate DDL, so each implementation can choose between a single text
/// column, a single binary column, or a hybrid layout.
/// </para>
/// <para>
/// v1 ships <c>StringAuditValueSerializer</c> (text-only). Future implementations
/// (binary, hybrid) are additive — they slot in without changing this interface.
/// </para>
/// </remarks>
public interface IAuditValueSerializer
{
    /// <summary>
    /// The columns this serializer requires on the <c>AuditDetail</c> table, in the
    /// order they should appear. Used by the schema installer to build DDL.
    /// </summary>
    IReadOnlyList<AuditValueColumn> Columns { get; }

    /// <summary>
    /// Encodes <paramref name="value"/> (of declared CLR type <paramref name="clrType"/>)
    /// into <paramref name="writer"/>.
    /// </summary>
    /// <param name="value">The value to encode. May be <c>null</c>.</param>
    /// <param name="clrType">The declared CLR type of the audited property.</param>
    /// <param name="writer">Receives the encoded representation.</param>
    /// <returns>The <c>ValueType</c> discriminator to persist alongside the value.</returns>
    string Encode(object? value, Type clrType, IAuditValueWriter writer);

    /// <summary>
    /// Decodes a value previously produced by <see cref="Encode"/>.
    /// </summary>
    /// <param name="reader">Supplies the persisted column values.</param>
    /// <param name="valueType">The <c>ValueType</c> discriminator returned by <see cref="Encode"/>.</param>
    object? Decode(IAuditValueReader reader, string valueType);
}
