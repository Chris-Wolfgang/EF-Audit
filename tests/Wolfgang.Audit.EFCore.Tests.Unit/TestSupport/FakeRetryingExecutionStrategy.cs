using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Wolfgang.Audit.Tests.Unit.TestSupport;



/// <summary>
/// Test double that reports <see cref="IExecutionStrategy.RetriesOnFailure"/> as
/// <c>true</c> so the interceptor's preflight check fires. Used to exercise the
/// retry-strategy rejection path without spinning up a real SQL Server connection.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class FakeRetryingExecutionStrategy : IExecutionStrategy
{
    public bool RetriesOnFailure => true;



    public TResult Execute<TState, TResult>
    (
        TState state,
        Func<DbContext, TState, TResult> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded
    )
    {
        throw new NotSupportedException("Test double — not expected to execute.");
    }



    public Task<TResult> ExecuteAsync<TState, TResult>
    (
        TState state,
        Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        CancellationToken cancellationToken
    )
    {
        throw new NotSupportedException("Test double — not expected to execute.");
    }
}
