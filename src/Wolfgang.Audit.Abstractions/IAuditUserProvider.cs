namespace Wolfgang.Audit;

/// <summary>
/// Supplies the <see cref="AuditUser"/> stamped onto each audit header row.
/// </summary>
/// <remarks>
/// Consumers implement this and register it with their DI container. The interceptor
/// resolves it once per <c>SaveChanges</c> call and writes the result to every header
/// row produced by that call.
/// </remarks>
public interface IAuditUserProvider
{
    /// <summary>
    /// Returns the user identity to associate with the currently-in-flight save.
    /// </summary>
    AuditUser GetCurrentUser();
}
