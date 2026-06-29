using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.TestKit.Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit.Serializers;

public class StringAuditValueSerializerContractTests
    : AuditValueSerializerContractTests<StringAuditValueSerializer>
{
    protected override StringAuditValueSerializer CreateSut() => new();
}
