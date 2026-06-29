namespace Wolfgang.Audit;

/// <summary>
/// Identifies the user responsible for a unit of audited work.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="UserId"/> is the principal whose credentials authenticated the operation —
/// typically the application's service account or the directly logged-in user.
/// </para>
/// <para>
/// <see cref="OnBehalfOfUserId"/> is populated in web/service scenarios where the
/// authenticated principal is acting on behalf of a real end user. Example: an ASP.NET Core
/// application runs as <c>svc-orders</c> (the service account =&gt; <see cref="UserId"/>) but
/// handles a request from <c>steve</c> (the human =&gt; <see cref="OnBehalfOfUserId"/>).
/// </para>
/// </remarks>
public readonly record struct AuditUser(string UserId, string? OnBehalfOfUserId = null);
