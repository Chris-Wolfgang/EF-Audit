using Microsoft.AspNetCore.Http;

namespace Wolfgang.AuditTrail.Example.WebApi;

/// <summary>
/// Demonstrates the on-behalf-of pattern. The service principal (the account the
/// app pool runs under) is recorded as <see cref="AuditUser.UserId"/>; the
/// authenticated end user supplying the request is recorded as
/// <see cref="AuditUser.OnBehalfOfUserId"/>. The end user is read from the
/// <c>X-User</c> header for demo simplicity — a real app would resolve it from
/// <c>HttpContext.User</c> after authentication.
/// </summary>
public sealed class HttpContextAuditUserProvider : IAuditUserProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpContextAuditUserProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public AuditUser GetCurrentUser()
    {
        const string ServiceAccount = "svc-orders";
        var actingUser = _httpContextAccessor.HttpContext?
            .Request.Headers["X-User"].ToString();

        return new AuditUser(
            UserId: ServiceAccount,
            OnBehalfOfUserId: string.IsNullOrWhiteSpace(actingUser) ? null : actingUser);
    }
}
