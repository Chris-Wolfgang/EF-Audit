using System.Diagnostics.CodeAnalysis;
using Wolfgang.Audit.EFCore.Schema.Tests.Integration.TestSupport;
using Xunit;

namespace Wolfgang.Audit.EFCore.Schema.Tests.Integration;



[ExcludeFromCodeCoverage]
public sealed class SqlServerAuditSchemaMigratorTests
    : AuditSchemaMigratorIntegrationTestsBase, IClassFixture<SqlServerSchemaFixture>
{
    public SqlServerAuditSchemaMigratorTests(SqlServerSchemaFixture fixture)
        : base(fixture)
    {
    }
}
