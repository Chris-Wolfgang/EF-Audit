using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit.Tests.Unit.TestSupport;



/// <summary>
/// Fixture for tests exercising the <see cref="AuditSaveChangesInterceptor"/> path.
/// Builds a plain <see cref="DbContext"/> with the interceptor wired in via
/// <c>AddInterceptors</c>. Mirrors how a real consumer would compose
/// <c>AddDbContext(...).UseAuditing(serviceProvider)</c>.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class InterceptorFixture : IDisposable
{
    private readonly DbConnection _connection;
    private readonly AuditSaveChangesInterceptor _interceptor;



    public InterceptorFixture
    (
        string userId = "test-user",
        string? onBehalfOfUserId = null,
        bool captureDeletedValues = false
    )
    {
        Options = new AuditOptions
        {
            CaptureDeletedValues = captureDeletedValues,
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        UserProvider = new StaticAuditUserProvider(userId, onBehalfOfUserId);
        _interceptor = new AuditSaveChangesInterceptor(UserProvider, Options);

        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        using var seed = CreateContext();
        seed.Database.EnsureCreated();
    }



    public AuditOptions Options { get; }

    public StaticAuditUserProvider UserProvider { get; }



    public InterceptorTestDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<InterceptorTestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_interceptor);

        return new InterceptorTestDbContext(builder.Options, Options);
    }



    /// <summary>
    /// Exposes the underlying connection for tests that need to construct a context
    /// with custom <see cref="DbContextOptionsBuilder"/> configuration (e.g. replacing
    /// the execution-strategy factory) instead of using <see cref="CreateContext"/>.
    /// </summary>
    public DbConnection GetConnection() => _connection;



    public void Dispose()
    {
        _connection.Dispose();
    }
}
