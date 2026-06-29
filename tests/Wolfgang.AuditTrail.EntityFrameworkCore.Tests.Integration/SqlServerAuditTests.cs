using Wolfgang.AuditTrail.Tests.Integration.TestSupport;

namespace Wolfgang.AuditTrail.Tests.Integration;

public class SqlServerAuditTests : ProviderAuditTestsBase<SqlServerFixture>
{
    public SqlServerAuditTests(SqlServerFixture fixture) : base(fixture) { }
}
