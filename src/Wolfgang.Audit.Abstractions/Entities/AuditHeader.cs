using System;
using System.Collections.Generic;

namespace Wolfgang.Audit.Entities;

/// <summary>
/// One row per audited entity change. Grouped with sibling header rows from the same
/// <c>SaveChanges</c> call via <see cref="TransactionId"/>.
/// </summary>
public class AuditHeader
{
    /// <summary>Primary key.</summary>
    public Guid HeaderId { get; set; }

    /// <summary>
    /// Shared by every header row produced by a single <c>SaveChanges</c> call. Lets
    /// consumers reconstruct "what changed together" without joining on timestamps.
    /// </summary>
    public Guid TransactionId { get; set; }

    /// <summary>UTC timestamp captured when the interceptor wrote the row.</summary>
    public DateTime AuditedAtUtc { get; set; }

    /// <summary>
    /// Authenticated principal — typically the service account running the app, or
    /// the directly logged-in user.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// End user the authenticated principal was acting on behalf of (web scenarios
    /// where a service account handles a request from a human user). <c>null</c> in
    /// non-impersonation scenarios.
    /// </summary>
    public string? OnBehalfOfUserId { get; set; }

    /// <summary>CLR full type name of the audited entity.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Fully-qualified table name (including schema) the entity maps to.</summary>
    public string EntityTable { get; set; } = string.Empty;

    /// <summary>
    /// Primary-key value(s) serialized by the configured
    /// <see cref="IAuditEntityKeySerializer"/>. For single-column keys this is the
    /// key's string form; for composite keys it is the serializer's chosen encoding.
    /// </summary>
    public string EntityKey { get; set; } = string.Empty;

    /// <summary>The type of change. See <see cref="AuditOperation"/>.</summary>
    public AuditOperation Operation { get; set; }

    /// <summary>Navigation to the per-column detail rows.</summary>
    public ICollection<AuditDetail> Details { get; set; } = new List<AuditDetail>();
}
