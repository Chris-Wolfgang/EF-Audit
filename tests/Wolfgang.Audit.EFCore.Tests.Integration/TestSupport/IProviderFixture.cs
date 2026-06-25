using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Tests.Integration.TestSupport;

/// <summary>
/// Abstracts the provider-specific bits of an integration test fixture so the same
/// test body can run against SQL Server, PostgreSQL, or MySQL.
/// </summary>
public interface IProviderFixture
{
    DbContextOptions<TestDbContext> CreateContextOptions();

    string ProviderName { get; }
}
