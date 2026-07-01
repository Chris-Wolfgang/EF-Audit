using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MySql;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Integration.TestSupport;

[ExcludeFromCodeCoverage]
public sealed class MySqlFixture : IAsyncLifetime, IProviderFixture
{
    // Pinned to a specific patch tag so test reruns are reproducible. Bump
    // deliberately; do not float on `:8.0`. WithDatabase routes to a non-default
    // "auditdb" so EnsureDeletedAsync can drop and recreate it freely.
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.0.39")
        .WithDatabase("auditdb")
        .Build();

    private ServerVersion? _serverVersion;

    public string ProviderName => "MySQL";

    public async Task InitializeAsync()
    {
        await _container.StartAsync().ConfigureAwait(false);

        // Detect the server version once at fixture init, not on every
        // CreateContextOptions call. AutoDetect opens a new connection each
        // time, which adds latency and a failure surface to every test.
        _serverVersion = ServerVersion.AutoDetect(_container.GetConnectionString());
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    public DbContextOptions<TestDbContext> CreateContextOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseMySql(
                _container.GetConnectionString(),
                _serverVersion ?? throw new System.InvalidOperationException("Fixture has not been initialized."))
            .Options;
    }
}
