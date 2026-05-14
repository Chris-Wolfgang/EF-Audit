using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Wolfgang.Audit.Tests.Integration.TestSupport;

[ExcludeFromCodeCoverage]
public sealed class SqlServerFixture : IAsyncLifetime, IProviderFixture
{
    private const string TestDatabaseName = "auditdb";

    // Pinned to a specific patch tag so test reruns are reproducible. Bump
    // deliberately when a new patch is available; do not float on `:2022-latest`.
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .Build();

    public string ProviderName => "SqlServer";

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DbContextOptions<TestDbContext> CreateContextOptions(AuditSaveChangesInterceptor interceptor)
    {
        // The container's default connection string points at `master`, which
        // EnsureDeletedAsync / EnsureCreatedAsync cannot manage. Switch the
        // InitialCatalog to a dedicated test database so the lifecycle is
        // isolated. SQL Server's MasterConnection (used internally by
        // EnsureCreated / EnsureDeleted) takes care of CREATE / DROP DATABASE.
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = TestDatabaseName,
        }.ConnectionString;

        return new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(connectionString)
            .AddInterceptors(interceptor)
            .Options;
    }
}
