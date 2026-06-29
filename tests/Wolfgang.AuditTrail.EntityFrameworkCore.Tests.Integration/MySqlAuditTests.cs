using Wolfgang.AuditTrail.Tests.Integration.TestSupport;

namespace Wolfgang.AuditTrail.Tests.Integration;

public class MySqlAuditTests : ProviderAuditTestsBase<MySqlFixture>
{
    public MySqlAuditTests(MySqlFixture fixture) : base(fixture) { }
}
