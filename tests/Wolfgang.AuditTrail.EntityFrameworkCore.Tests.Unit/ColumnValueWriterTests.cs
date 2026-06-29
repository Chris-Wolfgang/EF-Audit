using Wolfgang.AuditTrail.Entities;
using Wolfgang.AuditTrail.Internal;
using Wolfgang.AuditTrail.Serializers;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Direct unit tests for <see cref="ColumnValueWriter"/>. The capture path is
/// exercised end-to-end by the interceptor tests, but the throw branch for
/// unknown columns has no other coverage — a custom <c>IAuditValueSerializer</c>
/// has to misbehave to reach it.
/// </summary>
public class ColumnValueWriterTests
{
    [Fact]
    public void WriteText_when_column_is_ValueText_writes_to_AuditDetail_ValueText()
    {
        var detail = new AuditDetail();
        var writer = new ColumnValueWriter(detail);

        writer.WriteText(StringAuditValueSerializer.ValueColumnName, "hello");

        Assert.Equal("hello", detail.ValueText);
    }



    [Fact]
    public void WriteText_when_value_is_null_writes_null_to_ValueText()
    {
        var detail = new AuditDetail { ValueText = "seeded" };
        var writer = new ColumnValueWriter(detail);

        writer.WriteText(StringAuditValueSerializer.ValueColumnName, value: null);

        Assert.Null(detail.ValueText);
    }



    [Fact]
    public void WriteText_when_columnName_is_null_throws_ArgumentNullException()
    {
        var writer = new ColumnValueWriter(new AuditDetail());

        Assert.Throws<ArgumentNullException>(() => writer.WriteText(columnName: null!, value: "x"));
    }



    [Fact]
    public void WriteText_when_column_is_unknown_throws_InvalidOperationException()
    {
        var detail = new AuditDetail();
        var writer = new ColumnValueWriter(detail);

        var ex = Assert.Throws<InvalidOperationException>
        (
            () => writer.WriteText("ValueBinary", "anything")
        );

        Assert.Contains("ValueBinary", ex.Message, StringComparison.Ordinal);
        Assert.Contains(StringAuditValueSerializer.ValueColumnName, ex.Message, StringComparison.Ordinal);
        Assert.Null(detail.ValueText);
    }
}
