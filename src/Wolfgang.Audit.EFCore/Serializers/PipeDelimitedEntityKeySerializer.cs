using System.Globalization;
using System.Text;

namespace Wolfgang.Audit.Serializers;

/// <summary>
/// Default <see cref="IAuditEntityKeySerializer"/>. Joins key parts with the pipe
/// character (<c>'|'</c>).
/// </summary>
/// <remarks>
/// Each key value is rendered via <see cref="object.ToString"/> with
/// <see cref="CultureInfo.InvariantCulture"/> where applicable. <c>null</c> parts are
/// rendered as the empty string. Embedded pipes in key values are preserved as-is
/// (composite keys whose values may contain <c>'|'</c> should swap in a different
/// serializer such as JSON).
/// </remarks>
public sealed class PipeDelimitedEntityKeySerializer : IAuditEntityKeySerializer
{
    /// <inheritdoc />
    public string Serialize(IReadOnlyList<object?> keyValues)
    {
        ArgumentNullException.ThrowIfNull(keyValues);

        // Single-key fast path. Keyless entity types (HasNoKey) never reach this
        // serializer because they can't be in Added/Modified/Deleted state in the
        // change tracker — but if they ever did, the loop below produces "" naturally.
        if (keyValues.Count == 1)
        {
            return FormatPart(keyValues[0]);
        }

        var sb = new StringBuilder();
        for (var i = 0; i < keyValues.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('|');
            }

            sb.Append(FormatPart(keyValues[i]));
        }

        return sb.ToString();
    }

    private static string FormatPart(object? value)
    {
        return value switch
        {
            null => string.Empty,
            // byte[]: ToString() returns the type name ("System.Byte[]"), so
            // every binary key would collide. Hex-encode for a stable,
            // collision-free representation.
            byte[] bytes => FormatBytes(bytes),
            // DateTime / DateTimeOffset: IFormattable.ToString(null, Invariant)
            // uses the "G" format which truncates fractional seconds — distinct
            // instants can collide. Force "o" (round-trip ISO 8601).
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
#if NET6_0_OR_GREATER
            DateOnly d => d.ToString("o", CultureInfo.InvariantCulture),
            TimeOnly t => t.ToString("o", CultureInfo.InvariantCulture),
#endif
            IFormattable f => f.ToString(format: null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }



    private static string FormatBytes(byte[] bytes)
    {
#if NET6_0_OR_GREATER
        return Convert.ToHexString(bytes);
#else
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
        }
        return sb.ToString();
#endif
    }
}
