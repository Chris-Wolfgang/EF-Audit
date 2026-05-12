using System;
using FsCheck.Xunit;
using Xunit;

namespace Wolfgang.Audit.TestKit.Xunit;

/// <summary>
/// Contract test base every <see cref="IAuditValueSerializer"/> implementation should
/// inherit. Mixes FsCheck-generated property tests (~100 random inputs per type per
/// run) with explicit boundary theories that document known edge cases.
/// </summary>
/// <typeparam name="TSut">The serializer type under test.</typeparam>
public abstract class AuditValueSerializerContractTests<TSut>
    where TSut : IAuditValueSerializer
{
    /// <summary>Override to construct a fresh serializer for each test.</summary>
    protected abstract TSut CreateSut();

    private object? RoundTrip<T>(T value)
    {
        var sut = CreateSut();
        var buffer = new InMemoryAuditValueBuffer();
        var valueType = sut.Encode(value, typeof(T), buffer);
        return sut.Decode(buffer, valueType);
    }

    [Fact]
    public void Columns_returns_at_least_one_column()
    {
        var sut = CreateSut();
        Assert.NotEmpty(sut.Columns);
    }

    [Property]
    public bool String_round_trips(string? value) =>
        Equals(value, RoundTrip(value));

    [Property]
    public bool Int16_round_trips(short value) =>
        Equals(value, RoundTrip(value));

    [Property]
    public bool Int32_round_trips(int value) =>
        Equals(value, RoundTrip(value));

    [Property]
    public bool Int64_round_trips(long value) =>
        Equals(value, RoundTrip(value));

    [Property]
    public bool Decimal_round_trips(decimal value) =>
        Equals(value, RoundTrip(value));

    [Property]
    public bool Boolean_round_trips(bool value) =>
        Equals(value, RoundTrip(value));

    [Property]
    public bool Guid_round_trips(Guid value) =>
        Equals(value, RoundTrip(value));

    [Property]
    public bool Single_round_trips_for_finite_values(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return true;
        }

        return Equals(value, RoundTrip(value));
    }

    [Property]
    public bool Double_round_trips_for_finite_values(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return true;
        }

        return Equals(value, RoundTrip(value));
    }

    [Fact]
    public void Double_NaN_round_trips()
    {
        var decoded = RoundTrip(double.NaN);
        Assert.True(decoded is double d && double.IsNaN(d));
    }

    [Fact]
    public void Double_PositiveInfinity_round_trips()
    {
        Assert.Equal(double.PositiveInfinity, RoundTrip(double.PositiveInfinity));
    }

    [Fact]
    public void Double_NegativeInfinity_round_trips()
    {
        Assert.Equal(double.NegativeInfinity, RoundTrip(double.NegativeInfinity));
    }

    [Fact]
    public void DateTime_MinValue_round_trips()
    {
        Assert.Equal(DateTime.MinValue, RoundTrip(DateTime.MinValue));
    }

    [Fact]
    public void DateTime_MaxValue_round_trips()
    {
        Assert.Equal(DateTime.MaxValue, RoundTrip(DateTime.MaxValue));
    }

    [Fact]
    public void DateTimeOffset_with_nonzero_offset_round_trips()
    {
        var original = new DateTimeOffset(2026, 5, 12, 9, 30, 0, TimeSpan.FromHours(5.5));
        Assert.Equal(original, RoundTrip(original));
    }

    [Fact]
    public void Decimal_MaxValue_round_trips()
    {
        Assert.Equal(decimal.MaxValue, RoundTrip(decimal.MaxValue));
    }

    [Fact]
    public void Decimal_MinValue_round_trips()
    {
        Assert.Equal(decimal.MinValue, RoundTrip(decimal.MinValue));
    }

    [Fact]
    public void Guid_Empty_round_trips()
    {
        Assert.Equal(Guid.Empty, RoundTrip(Guid.Empty));
    }

    [Fact]
    public void Empty_string_round_trips()
    {
        Assert.Equal(string.Empty, RoundTrip(string.Empty));
    }

    [Fact]
    public void String_with_embedded_null_round_trips()
    {
        const string value = "a\0b";
        Assert.Equal(value, RoundTrip(value));
    }

    [Fact]
    public void Null_string_round_trips()
    {
        Assert.Null(RoundTrip<string?>(value: null));
    }

    [Fact]
    public void Empty_byte_array_round_trips()
    {
        var decoded = RoundTrip<byte[]>(Array.Empty<byte>());
        Assert.Equal(Array.Empty<byte>(), decoded);
    }

    [Fact]
    public void Byte_array_with_high_bytes_round_trips()
    {
        var value = new byte[] { 0x00, 0xFF, 0x7F, 0x80, 0x01 };
        Assert.Equal(value, RoundTrip(value));
    }
}
