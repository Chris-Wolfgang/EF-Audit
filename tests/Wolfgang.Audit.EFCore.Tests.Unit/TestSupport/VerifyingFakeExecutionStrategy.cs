using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Wolfgang.Audit.Tests.Unit.TestSupport;



/// <summary>
/// Test-double <see cref="IExecutionStrategy"/> that actually runs the
/// operation AND calls <c>verifySucceeded</c> afterwards, simulating the
/// commit-lost retry probe that retrying strategies (e.g.
/// <c>SqlServerRetryingExecutionStrategy</c>) perform when a transient
/// failure during <c>COMMIT</c> leaves the outcome ambiguous.
/// </summary>
/// <remarks>
/// EF Core's stock <see cref="ExecutionStrategy"/> base class never calls
/// <c>verifySucceeded</c> on a successful first attempt — the probe only
/// fires when a transient error during commit triggers a retry. To exercise
/// <c>AuditingDbContext.VerifyAuditCommitted{,Async}</c> we need a strategy
/// that calls the probe unconditionally so the delegate body runs in tests.
/// </remarks>
[ExcludeFromCodeCoverage]
internal sealed class VerifyingFakeExecutionStrategy : IExecutionStrategy
{
    private readonly DbContext _context;



    public VerifyingFakeExecutionStrategy(DbContext context)
    {
        _context = context;
    }



    public bool RetriesOnFailure => false;



    public TResult Execute<TState, TResult>
    (
        TState state,
        Func<DbContext, TState, TResult> operation,
        Func<DbContext, TState, ExecutionResult<TResult>>? verifySucceeded
    )
    {
        var result = operation(_context, state);

        // Always probe via verifySucceeded so the delegate body runs in tests.
        // The real strategies only do this after a transient commit failure;
        // here we use it as a coverage lever.
        _ = verifySucceeded?.Invoke(_context, state);

        return result;
    }



    public async Task<TResult> ExecuteAsync<TState, TResult>
    (
        TState state,
        Func<DbContext, TState, CancellationToken, Task<TResult>> operation,
        Func<DbContext, TState, CancellationToken, Task<ExecutionResult<TResult>>>? verifySucceeded,
        CancellationToken cancellationToken
    )
    {
        var result = await operation(_context, state, cancellationToken).ConfigureAwait(false);

        if (verifySucceeded is not null)
        {
            _ = await verifySucceeded(_context, state, cancellationToken).ConfigureAwait(false);
        }

        return result;
    }
}



/// <summary>
/// <see cref="IExecutionStrategyFactory"/> that yields
/// <see cref="VerifyingFakeExecutionStrategy"/> instances bound to the
/// current <see cref="DbContext"/>.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class VerifyingFakeExecutionStrategyFactory : IExecutionStrategyFactory
{
    private readonly ICurrentDbContext _current;



    public VerifyingFakeExecutionStrategyFactory(ICurrentDbContext current)
    {
        _current = current;
    }



    public IExecutionStrategy Create() => new VerifyingFakeExecutionStrategy(_current.Context);
}
