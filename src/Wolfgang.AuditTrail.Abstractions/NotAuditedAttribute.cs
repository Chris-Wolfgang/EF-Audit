using System;

namespace Wolfgang.AuditTrail;

/// <summary>
/// Excludes an entity type or a single property from audit capture.
/// </summary>
/// <remarks>
/// Apply at the class level to skip the entire entity, or at the property level to keep
/// the entity audited but exclude one column. Mirrors the parallel with EF Core's
/// <c>[NotMapped]</c> attribute.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class NotAuditedAttribute : Attribute
{
}
