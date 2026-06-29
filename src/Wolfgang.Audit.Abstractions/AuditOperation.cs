namespace Wolfgang.Audit;

/// <summary>
/// Identifies the type of change captured on an <see cref="Entities.AuditHeader"/> row.
/// The underlying byte values are the ASCII codes for <c>'I'</c>, <c>'U'</c>, and <c>'D'</c>;
/// the EF model converts the enum through <c>char</c> on the way out so the database
/// column literally contains <c>'I'</c> / <c>'U'</c> / <c>'D'</c> when queried directly.
/// </summary>
public enum AuditOperation : byte
{
    /// <summary>An entity was inserted.</summary>
    Insert = (byte)'I',

    /// <summary>An entity was updated.</summary>
    Update = (byte)'U',

    /// <summary>An entity was deleted.</summary>
    Delete = (byte)'D',
}
