using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit.Tests.Unit.TestSupport;

[ExcludeFromCodeCoverage]
public sealed class AuditFixture : IDisposable
{
    private readonly DbConnection _connection;

    public AuditFixture(
        string userId = "test-user",
        string? onBehalfOfUserId = null,
        bool captureDeletedValues = false,
        string headerTableName = "AuditHeader",
        string detailTableName = "AuditDetail",
        bool createOnConstruct = true)
    {
        Options = new AuditOptions
        {
            CaptureDeletedValues = captureDeletedValues,
            HeaderTableName = headerTableName,
            DetailTableName = detailTableName,
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        UserProvider = new StaticAuditUserProvider(userId, onBehalfOfUserId);

        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        if (createOnConstruct)
        {
            using var seed = CreateContext();
            seed.Database.EnsureCreated();
        }
    }

    public AuditOptions Options { get; }

    public StaticAuditUserProvider UserProvider { get; }

    public TestDbContext CreateContext()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection);

        return new TestDbContext(builder.Options, UserProvider, Options);
    }

    /// <summary>
    /// Convenience wrapper kept so individual tests don't have to restate intent.
    /// With the <see cref="AuditingDbContext"/> base, plain <c>SaveChangesAsync</c>
    /// already writes audit rows atomically — this wrapper just calls it.
    /// </summary>
    public Task<int> SaveAsync(TestDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
