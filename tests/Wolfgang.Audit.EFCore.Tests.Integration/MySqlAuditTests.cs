using Wolfgang.Audit.Tests.Integration.TestSupport;

namespace Wolfgang.Audit.Tests.Integration;

public class MySqlAuditTests : ProviderAuditTestsBase<MySqlFixture>
{
    public MySqlAuditTests(MySqlFixture fixture) : base(fixture) { }
}
