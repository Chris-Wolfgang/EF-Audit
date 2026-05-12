using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Wolfgang.Audit.Tests.Integration.TestSupport;

[ExcludeFromCodeCoverage]
public sealed class SqlServerFixture : IAsyncLifetime, IProviderFixture
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ProviderName => "SqlServer";

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DbContextOptions<TestDbContext> CreateContextOptions(AuditSaveChangesInterceptor interceptor)
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(_container.GetConnectionString())
            .AddInterceptors(interceptor)
            .Options;
    }
}
