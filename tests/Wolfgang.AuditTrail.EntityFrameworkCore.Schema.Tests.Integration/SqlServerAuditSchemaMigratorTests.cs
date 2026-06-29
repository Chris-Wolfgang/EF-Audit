using System.Diagnostics.CodeAnalysis;
using Wolfgang.AuditTrail.EntityFrameworkCore.Schema.Tests.Integration.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.EntityFrameworkCore.Schema.Tests.Integration;



[ExcludeFromCodeCoverage]
public sealed class SqlServerAuditSchemaMigratorTests
    : AuditSchemaMigratorIntegrationTestsBase, IClassFixture<SqlServerSchemaFixture>
{
    public SqlServerAuditSchemaMigratorTests(SqlServerSchemaFixture fixture)
        : base(fixture)
    {
    }
}
