using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using Xunit;

namespace Wolfgang.Audit.Tests.Integration.TestSupport;

[ExcludeFromCodeCoverage]
public sealed class MySqlFixture : IAsyncLifetime, IProviderFixture
{
    private readonly MySqlContainer _container = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .Build();

    public string ProviderName => "MySQL";

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DbContextOptions<TestDbContext> CreateContextOptions(AuditSaveChangesInterceptor interceptor)
    {
        var connectionString = _container.GetConnectionString();
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .AddInterceptors(interceptor)
            .Options;
    }
}
