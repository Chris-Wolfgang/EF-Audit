using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Integration.TestSupport;

[ExcludeFromCodeCoverage]
public sealed class SqlServerFixture : IAsyncLifetime, IProviderFixture
{
    // Pinned to a specific patch tag so test reruns are reproducible. Bump
    // deliberately when a new patch is available; do not float on `:2022-latest`.
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
        .Build();

    public string ProviderName => "SqlServer";

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DbContextOptions<TestDbContext> CreateContextOptions()
    {
        // Route to a per-test "auditdb" database rather than the container's
        // default "master". EnsureDeletedAsync runs ALTER DATABASE ... SET
        // SINGLE_USER which fails on master ("Option 'SINGLE_USER' cannot be set
        // in database 'master'"), so we have to use a non-system DB.
        var builder = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "auditdb",
        };

        return new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlServer(builder.ConnectionString)
            .Options;
    }
}
