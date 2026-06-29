# Wolfgang.Audit.TestKit.Xunit

xunit contract-test bases for the `Wolfgang.Audit.*` family. Inherit
`AuditValueSerializerContractTests<TSut>` to validate any
`IAuditValueSerializer` implementation against the full property-based +
boundary-value suite.

```csharp
public class StringAuditValueSerializerTests
    : AuditValueSerializerContractTests<StringAuditValueSerializer>
{
    protected override StringAuditValueSerializer CreateSut() => new();
}
```

See the [project README](https://github.com/Chris-Wolfgang/EF-Audit).
