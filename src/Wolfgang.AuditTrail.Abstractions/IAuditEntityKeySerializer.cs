using System.Collections.Generic;

namespace Wolfgang.AuditTrail;

/// <summary>
/// Serializes an entity's primary key value(s) into the string stored in
/// <c>AuditHeader.EntityKey</c>.
/// </summary>
/// <remarks>
/// Composite keys are passed as a multi-element sequence; single-column keys arrive as
/// a sequence of one element. The default implementation joins parts with <c>'|'</c>;
/// consumers can swap to JSON or any other format.
/// </remarks>
public interface IAuditEntityKeySerializer
{
    /// <summary>
    /// Renders the given primary-key values as the string persisted in
    /// <c>AuditHeader.EntityKey</c>.
    /// </summary>
    string Serialize(IReadOnlyList<object?> keyValues);
}
