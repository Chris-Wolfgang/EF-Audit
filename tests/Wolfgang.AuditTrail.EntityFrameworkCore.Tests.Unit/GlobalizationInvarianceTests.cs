using System.Globalization;
using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.TestKit.Xunit;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Pins culture-invariant behaviour of the v1 serializers. The audit trail must
/// render and round-trip values identically regardless of the server's ambient
/// culture — a German server (decimal comma), a Turkish server (dotted-I), or a
/// Chinese/Japanese/Arabic server must not change how a number, date, or key is
/// written. Each test runs under a matrix of hostile cultures.
/// </summary>
public class GlobalizationInvarianceTests
{
    // "" == invariant culture. tr-TR (dotted-I), de-DE (decimal comma),
    // zh-CN (collation), ja-JP, ar-SA (non-Gregorian default calendar).
    public static IEnumerable<object[]> Cultures =>
        new[] { "", "en-US", "tr-TR", "de-DE", "zh-CN", "ja-JP", "ar-SA" }
            .Select(c => new object[] { c });



    private static void InCulture(string name, Action body)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            body();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }



    [Theory]
    [MemberData(nameof(Cultures))]
    public void StringAuditValueSerializer_decimal_encodes_invariantly_and_round_trips(string culture)
    {
        InCulture
        (
            culture,
            () =>
            {
                var sut = new StringAuditValueSerializer();
                var buffer = new InMemoryAuditValueBuffer();
                const decimal value = 1234567.89m;

                var valueType = sut.Encode(value, typeof(decimal), buffer);

                // Invariant: '.' as the decimal point under every culture — never ','.
                Assert.Equal("1234567.89", buffer.ReadText(StringAuditValueSerializer.ValueColumnName));
                Assert.Equal(value, sut.Decode(buffer, valueType));
            }
        );
    }



    [Theory]
    [MemberData(nameof(Cultures))]
    public void StringAuditValueSerializer_double_encodes_invariantly_and_round_trips(string culture)
    {
        InCulture
        (
            culture,
            () =>
            {
                var sut = new StringAuditValueSerializer();
                var buffer = new InMemoryAuditValueBuffer();
                const double value = 1234.5d;

                var valueType = sut.Encode(value, typeof(double), buffer);

                Assert.Equal("1234.5", buffer.ReadText(StringAuditValueSerializer.ValueColumnName));
                Assert.Equal(value, sut.Decode(buffer, valueType));
            }
        );
    }



    [Theory]
    [MemberData(nameof(Cultures))]
    public void StringAuditValueSerializer_DateTime_round_trips_under_any_culture(string culture)
    {
        InCulture
        (
            culture,
            () =>
            {
                var sut = new StringAuditValueSerializer();
                var buffer = new InMemoryAuditValueBuffer();
                var value = new DateTime(2026, 6, 30, 13, 45, 30, DateTimeKind.Utc);

                var valueType = sut.Encode(value, typeof(DateTime), buffer);

                // Round-trip "o" format is culture-independent and starts with the ISO date.
                var text = buffer.ReadText(StringAuditValueSerializer.ValueColumnName);
                Assert.StartsWith("2026-06-30T13:45:30", text, StringComparison.Ordinal);
                Assert.Equal(value, sut.Decode(buffer, valueType));
            }
        );
    }



    [Theory]
    [MemberData(nameof(Cultures))]
    public void StringAuditValueSerializer_Guid_round_trips_under_any_culture(string culture)
    {
        InCulture
        (
            culture,
            () =>
            {
                var sut = new StringAuditValueSerializer();
                var buffer = new InMemoryAuditValueBuffer();
                var value = new Guid("3f2504e0-4f89-41d3-9a0c-0305e82c3301");

                var valueType = sut.Encode(value, typeof(Guid), buffer);

                Assert.Equal("3f2504e0-4f89-41d3-9a0c-0305e82c3301", buffer.ReadText(StringAuditValueSerializer.ValueColumnName));
                Assert.Equal(value, sut.Decode(buffer, valueType));
            }
        );
    }



    [Theory]
    [MemberData(nameof(Cultures))]
    public void PipeDelimitedEntityKeySerializer_renders_numeric_parts_invariantly(string culture)
    {
        InCulture
        (
            culture,
            () =>
            {
                var sut = new PipeDelimitedEntityKeySerializer();

                // A composite key mixing an int and a decimal: the decimal must use
                // '.' under de-DE, not ',', or the joined key drifts per server.
                var key = sut.Serialize(new object?[] { 1234, 5678.9m });

                Assert.Equal("1234|5678.9", key);
            }
        );
    }
}
