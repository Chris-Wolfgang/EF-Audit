using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage;

namespace Wolfgang.Audit.Tests.Unit.TestSupport;



/// <summary>
/// Test-double <see cref="IExecutionStrategyFactory"/> that always returns a
/// <see cref="FakeRetryingExecutionStrategy"/>. Registered via
/// <c>DbContextOptionsBuilder.ReplaceService</c> to drive the interceptor's
/// retry-strategy preflight check.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeRetryingExecutionStrategyFactory : IExecutionStrategyFactory
{
    public IExecutionStrategy Create() => new FakeRetryingExecutionStrategy();
}
