using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.TestKit.Xunit;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Plugs small coverage holes in the v1 serializers: catch-all branches in
/// <see cref="StringAuditValueSerializer.Encode"/> / <c>Decode</c> and the
/// null-value branch in <see cref="PipeDelimitedEntityKeySerializer"/>.
/// </summary>
public class SerializerEdgeCaseTests
{
    private sealed class NonFormattableThing
    {
        public override string ToString() => "non-formattable";
    }



    [Fact]
    public void StringAuditValueSerializer_Encode_falls_back_to_ToString_for_non_formattable_reference_type()
    {
        var sut = new StringAuditValueSerializer();
        var buffer = new InMemoryAuditValueBuffer();

        var thing = new NonFormattableThing();
        var valueType = sut.Encode(thing, thing.GetType(), buffer);

        Assert.Equal("non-formattable", buffer.ReadText(StringAuditValueSerializer.ValueColumnName));
        // ResolveValueType falls through to underlying.Name for non-special types.
        Assert.Equal(nameof(NonFormattableThing), valueType);
    }



    [Fact]
    public void StringAuditValueSerializer_Decode_returns_raw_text_for_unknown_valueType()
    {
        var sut = new StringAuditValueSerializer();
        var buffer = new InMemoryAuditValueBuffer();
        buffer.WriteText(StringAuditValueSerializer.ValueColumnName, "raw-payload");

        // Unknown valueType discriminator — Decode falls through to the catch-all
        // and returns the text as-is.
        var roundTripped = sut.Decode(buffer, valueType: "SomeUnknownDiscriminator");

        Assert.Equal("raw-payload", roundTripped);
    }



    [Fact]
    public void PipeDelimitedEntityKeySerializer_when_part_is_null_emits_empty_segment()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        // Two-part composite key with the first part null: rendered as "|abc".
        var result = sut.Serialize(new object?[] { null, "abc" });

        Assert.Equal("|abc", result);
    }
}
