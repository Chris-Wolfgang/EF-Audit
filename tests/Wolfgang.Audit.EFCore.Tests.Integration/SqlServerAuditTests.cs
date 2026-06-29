using Wolfgang.Audit.Tests.Integration.TestSupport;

namespace Wolfgang.Audit.Tests.Integration;

public class SqlServerAuditTests : ProviderAuditTestsBase<SqlServerFixture>
{
    public SqlServerAuditTests(SqlServerFixture fixture) : base(fixture) { }
}
