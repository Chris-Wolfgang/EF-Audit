using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.TestKit.Xunit;

namespace Wolfgang.Audit.Tests.Unit.Serializers;

public class StringAuditValueSerializerContractTests
    : AuditValueSerializerContractTests<StringAuditValueSerializer>
{
    protected override StringAuditValueSerializer CreateSut() => new();
}
