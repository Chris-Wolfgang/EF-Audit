using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit;
using Wolfgang.Audit.Schema;

namespace Wolfgang.Audit.EFCore.Schema.Tests.Integration.TestSupport;



/// <summary>
/// Provider-agnostic surface the test class needs to drive
/// <see cref="AuditSchemaMigrator"/> against a real database. Each provider's
/// fixture (Testcontainers SQL Server / PostgreSQL) implements it so the same
/// test methods cover every RDBMS without copy-paste.
/// </summary>
public interface ISchemaProviderFixture
{
    /// <summary>Friendly name for test output (SqlServer / PostgreSQL).</summary>
    string ProviderName { get; }

    /// <summary>Schema name to use for the "custom schema" tests on this provider.</summary>
    string CustomSchema { get; }

    /// <summary>Returns a context bound to a unique database on the running container.</summary>
    Task<AuditMigrationsDbContext> CreateContextAsync(AuditOptions options);

    /// <summary>Lists user-table names in the most recently created database.</summary>
    Task<IReadOnlyList<TableInfo>> ListTablesAsync(string? schema);
}



/// <summary>
/// (schema, table) pair returned by <see cref="ISchemaProviderFixture.ListTablesAsync"/>.
/// Schema may be null on providers/configurations that don't surface one.
/// </summary>
public sealed record TableInfo(string? Schema, string Name);
