using Wolfgang.AuditTrail.Tests.Integration.TestSupport;

namespace Wolfgang.AuditTrail.Tests.Integration;

public class PostgresAuditTests : ProviderAuditTestsBase<PostgresFixture>
{
    public PostgresAuditTests(PostgresFixture fixture) : base(fixture) { }
}
