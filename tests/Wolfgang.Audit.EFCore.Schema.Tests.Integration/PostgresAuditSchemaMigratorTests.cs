using System.Diagnostics.CodeAnalysis;
using Wolfgang.Audit.EFCore.Schema.Tests.Integration.TestSupport;
using Xunit;

namespace Wolfgang.Audit.EFCore.Schema.Tests.Integration;



[ExcludeFromCodeCoverage]
public sealed class PostgresAuditSchemaMigratorTests
    : AuditSchemaMigratorIntegrationTestsBase, IClassFixture<PostgresSchemaFixture>
{
    public PostgresAuditSchemaMigratorTests(PostgresSchemaFixture fixture)
        : base(fixture)
    {
    }
}
