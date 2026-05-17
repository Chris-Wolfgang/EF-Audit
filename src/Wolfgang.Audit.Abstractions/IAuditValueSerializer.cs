using System;
using System.Collections.Generic;

namespace Wolfgang.Audit;

/// <summary>
/// Encodes values captured by the audit interceptor into the column(s) it persists
/// on the <c>AuditDetail</c> row, and decodes them back on read.
/// </summary>
/// <remarks>
/// <para>
/// The serializer declares the value column(s) it expects on the <c>AuditDetail</c>
/// table via <see cref="Columns"/>. v1's DDL path is just
/// <c>Database.EnsureCreatedAsync</c> against the EF Core model — which contains the
/// fixed <c>ValueText</c> / <c>ValueBinary</c> property set on <c>AuditDetail</c>
/// regardless of which serializer is configured. <see cref="Columns"/> is therefore
/// advisory in v1 (used by the serializer to keep its writes consistent with the
/// entity properties); a future schema installer that emits per-serializer DDL is
/// planned but not shipped.
/// </para>
/// <para>
/// v1 ships <c>StringAuditValueSerializer</c> (text-only). Future implementations
/// (binary, hybrid) are additive — they slot in without changing this interface.
/// </para>
/// </remarks>
public interface IAuditValueSerializer
{
    /// <summary>
    /// The columns this serializer expects on the <c>AuditDetail</c> table, in the
    /// order they should appear. Advisory in v1 — see remarks on the interface.
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
