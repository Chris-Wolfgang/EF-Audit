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
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:16.4-alpine3.20")
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
