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
            IFormattable f => f.ToString(format: null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}
