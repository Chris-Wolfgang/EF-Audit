using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.TestKit.Xunit;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Pins the exact wire format and discriminator (<c>valueType</c>) that
/// <see cref="StringAuditValueSerializer"/> produces for each supported type,
/// plus the null and enum branches. Round-trip contract tests prove decode∘encode
/// is identity; these lock the *literal* stored text so a formatting change is
/// caught (e.g. Guid "D" vs "N", bool "True" vs "true", enum discriminator shape).
/// </summary>
public sealed class StringAuditValueSerializerExactFormatTests
{
    private enum Color
    {
        Red,
        Green,
    }

    private static (string? text, string valueType) Encode<T>(T value)
    {
        var sut = new StringAuditValueSerializer();
        var buffer = new InMemoryAuditValueBuffer();
        var valueType = sut.Encode(value, typeof(T), buffer);
        return (buffer.ReadText(StringAuditValueSerializer.ValueColumnName), valueType);
    }



    [Fact]
    public void Bool_true_is_literal_True_with_Boolean_discriminator()
    {
        var (text, valueType) = Encode(true);
        Assert.Equal("True", text);
        Assert.Equal("Boolean", valueType);
    }



    [Fact]
    public void Bool_false_is_literal_False()
    {
        Assert.Equal("False", Encode(false).text);
    }



    [Fact]
    public void Guid_uses_D_format()
    {
        var g = new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301");
        var (text, valueType) = Encode(g);
        Assert.Equal("3f2504e0-4f89-41d3-9a0c-0305e82c3301", text);
        Assert.Equal("Guid", valueType);
    }



    [Fact]
    public void DateTime_uses_round_trip_o_format()
    {
        var dt = new DateTime(2026, 6, 30, 13, 45, 30, DateTimeKind.Utc);
        var (text, valueType) = Encode(dt);
        Assert.Equal("2026-06-30T13:45:30.0000000Z", text);
        Assert.Equal("DateTime", valueType);
    }



    [Fact]
    public void DateTimeOffset_uses_round_trip_o_format()
    {
        var dto = new DateTimeOffset(2026, 6, 30, 13, 45, 30, TimeSpan.FromHours(5.5));
        var (text, valueType) = Encode(dto);
        Assert.Equal("2026-06-30T13:45:30.0000000+05:30", text);
        Assert.Equal("DateTimeOffset", valueType);
    }



    [Fact]
    public void Bytes_are_base64_with_Bytes_discriminator()
    {
        var (text, valueType) = Encode(new byte[] { 0x00, 0x01, 0xFF });
        Assert.Equal(Convert.ToBase64String(new byte[] { 0x00, 0x01, 0xFF }), text);
        Assert.Equal("Bytes", valueType);
    }



    [Fact]
    public void Int_uses_invariant_text_and_Int32_discriminator()
    {
        var (text, valueType) = Encode(-42);
        Assert.Equal("-42", text);
        Assert.Equal("Int32", valueType);
    }



    [Fact]
    public void Enum_discriminator_is_Enum_prefix_plus_assembly_qualified_name()
    {
        var (text, valueType) = Encode(Color.Green);
        Assert.Equal("Green", text);
        Assert.StartsWith("Enum:", valueType, StringComparison.Ordinal);
        // AssemblyQualifiedName (not just FullName) so cross-assembly enums resolve.
        Assert.Contains(typeof(Color).FullName!, valueType, StringComparison.Ordinal);
        Assert.Contains(",", valueType[5..], StringComparison.Ordinal);
    }



    [Fact]
    public void Null_writes_null_text_and_records_the_declared_type()
    {
        var (text, valueType) = Encode<string?>(null);
        Assert.Null(text);
        Assert.Equal("String", valueType);
    }



    [Fact]
    public void Null_nullable_int_records_Int32_not_a_nullable_wrapper()
    {
        var (text, valueType) = Encode<int?>(null);
        Assert.Null(text);
        Assert.Equal("Int32", valueType);
    }



    [Fact]
    public void Decode_null_text_returns_null_regardless_of_type()
    {
        var sut = new StringAuditValueSerializer();
        var buffer = new InMemoryAuditValueBuffer();
        buffer.WriteText(StringAuditValueSerializer.ValueColumnName, value: null);
        Assert.Null(sut.Decode(buffer, "Int32"));
    }



    [Fact]
    public void Decode_resolves_a_known_enum_back_to_the_enum_value()
    {
        var sut = new StringAuditValueSerializer();
        var buffer = new InMemoryAuditValueBuffer();
        var valueType = sut.Encode(Color.Green, typeof(Color), buffer);
        var decoded = sut.Decode(buffer, valueType);
        Assert.Equal(Color.Green, decoded);
    }



    private static object? Decode(string text, string valueType)
    {
        var sut = new StringAuditValueSerializer();
        var buffer = new InMemoryAuditValueBuffer();
        buffer.WriteText(StringAuditValueSerializer.ValueColumnName, text);
        return sut.Decode(buffer, valueType);
    }



    [Theory]
    [InlineData("True", "Boolean", true)]
    [InlineData("False", "Boolean", false)]
    [InlineData("42", "Byte", (byte)42)]
    [InlineData("-42", "SByte", (sbyte)-42)]
    [InlineData("-1234", "Int16", (short)-1234)]
    [InlineData("1234", "UInt16", (ushort)1234)]
    [InlineData("-100000", "Int32", -100000)]
    [InlineData("100000", "UInt32", 100000u)]
    [InlineData("-5000000000", "Int64", -5000000000L)]
    [InlineData("5000000000", "UInt64", 5000000000UL)]
    public void Decode_parses_each_discriminator_to_its_exact_typed_value(string text, string valueType, object expected)
    {
        Assert.Equal(expected, Decode(text, valueType));
    }



    [Fact]
    public void Decode_bytes_returns_the_original_byte_array()
    {
        var bytes = new byte[] { 0x00, 0x01, 0xFF, 0x7F };
        var decoded = Decode(Convert.ToBase64String(bytes), "Bytes");
        Assert.Equal(bytes, Assert.IsType<byte[]>(decoded));
    }



    [Fact]
    public void Decode_guid_uses_exact_D_parse()
    {
        Assert.Equal(
            new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301"),
            Decode("3f2504e0-4f89-41d3-9a0c-0305e82c3301", "Guid"));
    }



    [Fact]
    public void Decode_unknown_discriminator_falls_back_to_raw_text()
    {
        Assert.Equal("whatever", Decode("whatever", "SomethingUnknown"));
    }
}
