using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Wolfgang.Audit.Tests.Integration.TestSupport;

[ExcludeFromCodeCoverage]
public sealed class PostgresFixture : IAsyncLifetime, IProviderFixture
{
    // Pinned to a specific patch tag so test reruns are reproducible. Bump
    // deliberately; do not float on `:16-alpine`.
    // WithDatabase pins the container's user database to a dedicated name so
    // EnsureDeletedAsync / EnsureCreatedAsync target an isolated DB instead of
    // PostgreSQL's `postgres` system database.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.4-alpine3.20")
        .WithDatabase("auditdb")
        .Build();

    public string ProviderName => "PostgreSQL";

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DbContextOptions<TestDbContext> CreateContextOptions(AuditSaveChangesInterceptor interceptor)
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;
    }
}
