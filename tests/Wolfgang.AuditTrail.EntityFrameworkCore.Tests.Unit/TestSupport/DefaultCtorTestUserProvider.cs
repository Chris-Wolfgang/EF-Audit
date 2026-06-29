using System.Diagnostics.CodeAnalysis;

namespace Wolfgang.AuditTrail.Tests.Unit.TestSupport;



/// <summary>
/// Parameterless <see cref="IAuditUserProvider"/> used by the
/// <see cref="ServiceCollectionExtensions.AddEfCoreAuditing{T}"/> tests, where
/// the constraint requires the type to be resolvable by DI without manual
/// instance registration. <see cref="StaticAuditUserProvider"/> can't be used
/// here because its constructor takes string arguments.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DefaultCtorTestUserProvider : IAuditUserProvider
{
    public AuditUser GetCurrentUser() => new("default-test-user");
}
