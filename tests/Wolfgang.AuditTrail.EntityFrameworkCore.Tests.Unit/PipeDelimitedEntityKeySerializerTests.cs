// S3257: explicit `new object?[]` is intentional — every call site here is
// asserting how the serializer handles boxed-as-object key parts, which is the
// exact wire-format the interceptor passes in production.
#pragma warning disable S3257

using Wolfgang.AuditTrail.Serializers;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Targeted regression coverage for PR #2 cluster #3 — the
/// <see cref="PipeDelimitedEntityKeySerializer"/> previously had two
/// collision modes:
///
///   1. <c>byte[]</c> keys fell through to <c>object.ToString()</c> which
///      returned <c>"System.Byte[]"</c> for every value.
///   2. <c>DateTime</c> / <c>DateTimeOffset</c> went through
///      <c>IFormattable.ToString(null, Invariant)</c> (format "G"), which
///      truncates fractional seconds — distinct instants collided.
/// </summary>
public class PipeDelimitedEntityKeySerializerTests
{
    [Fact]
    public void Serialize_distinct_byte_array_keys_produce_distinct_strings()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        var a = sut.Serialize(new object?[] { new byte[] { 0x01, 0x02, 0x03 } });
        var b = sut.Serialize(new object?[] { new byte[] { 0x04, 0x05, 0x06 } });

        Assert.NotEqual(a, b);
        Assert.Equal("010203", a);
        Assert.Equal("040506", b);
    }



    [Fact]
    public void Serialize_empty_byte_array_produces_empty_string_not_type_name()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        Assert.Equal(string.Empty, sut.Serialize(new object?[] { Array.Empty<byte>() }));
    }



    [Fact]
    public void Serialize_DateTime_with_fractional_seconds_does_not_truncate()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        // Two timestamps 1 tick apart — pre-fix both serialized to the same
        // string under the "G" format ("2026-01-15 14:30:45").
        var a = new DateTime(2026, 1, 15, 14, 30, 45, DateTimeKind.Utc).AddTicks(1);
        var b = new DateTime(2026, 1, 15, 14, 30, 45, DateTimeKind.Utc).AddTicks(2);

        var sa = sut.Serialize(new object?[] { a });
        var sb = sut.Serialize(new object?[] { b });

        Assert.NotEqual(sa, sb);
        // "o" (round-trip) format preserves full tick precision + Z kind suffix.
        Assert.EndsWith("Z", sa, StringComparison.Ordinal);
    }



    [Fact]
    public void Serialize_DateTimeOffset_preserves_offset_and_fractional_seconds()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        var a = new DateTimeOffset(2026, 1, 15, 14, 30, 45, TimeSpan.FromHours(2)).AddTicks(1);
        var b = new DateTimeOffset(2026, 1, 15, 14, 30, 45, TimeSpan.FromHours(2)).AddTicks(2);
        // Same instant, different offsets — the round-trip format keeps both.
        var c = new DateTimeOffset(2026, 1, 15, 14, 30, 45, TimeSpan.FromHours(2));
        var d = new DateTimeOffset(2026, 1, 15, 12, 30, 45, TimeSpan.Zero);

        Assert.NotEqual(sut.Serialize(new object?[] { a }), sut.Serialize(new object?[] { b }));
        Assert.NotEqual(sut.Serialize(new object?[] { c }), sut.Serialize(new object?[] { d }));
    }



    [Fact]
    public void Serialize_composite_key_with_byte_array_keeps_other_parts_intact()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        var result = sut.Serialize(new object?[] { 42, new byte[] { 0xAB, 0xCD } });

        Assert.Equal("42|ABCD", result);
    }



    [Fact]
    public void Serialize_null_part_produces_empty_segment()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        Assert.Equal("a||b", sut.Serialize(new object?[] { "a", null, "b" }));
    }



    [Theory]
    [InlineData(123,        "123")]
    [InlineData(123.456,    "123.456")]
    [InlineData(true,       "True")]
    public void Serialize_primitive_types_use_invariant_formatting(object value, string expected)
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        Assert.Equal(expected, sut.Serialize(new object?[] { value }));
    }



    [Fact]
    public void Serialize_guid_uses_invariant_string_form()
    {
        var sut = new PipeDelimitedEntityKeySerializer();
        var guid = new Guid("11111111-2222-3333-4444-555555555555");

        Assert.Equal("11111111-2222-3333-4444-555555555555", sut.Serialize(new object?[] { guid }));
    }



    // Keyless entity types (HasNoKey — views, raw-SQL query types) are read-only
    // in EF Core and never reach the audit pipeline, so they never produce a key
    // to serialize. The capture path is still null-safe: FindPrimaryKey() == null
    // yields an empty key-value list, and the serializer must degrade to "" rather
    // than throw. This pins that documented fallback (see ADR-0004).
    [Fact]
    public void Serialize_empty_key_values_returns_empty_string()
    {
        var sut = new PipeDelimitedEntityKeySerializer();

        Assert.Equal(string.Empty, sut.Serialize(Array.Empty<object?>()));
    }
}
