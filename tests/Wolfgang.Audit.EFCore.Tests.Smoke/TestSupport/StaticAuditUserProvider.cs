using System.Diagnostics.CodeAnalysis;

namespace Wolfgang.Audit.Tests.Smoke.TestSupport;

[ExcludeFromCodeCoverage]
public sealed class StaticAuditUserProvider : IAuditUserProvider
{
    private readonly AuditUser _user;

    public StaticAuditUserProvider(string userId, string? onBehalfOfUserId = null)
    {
        _user = new AuditUser(userId, onBehalfOfUserId);
    }

    public AuditUser GetCurrentUser() => _user;
}
