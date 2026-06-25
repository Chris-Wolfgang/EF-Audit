using Wolfgang.Audit.Tests.Integration.TestSupport;

namespace Wolfgang.Audit.Tests.Integration;

public class PostgresAuditTests : ProviderAuditTestsBase<PostgresFixture>
{
    public PostgresAuditTests(PostgresFixture fixture) : base(fixture) { }
}
