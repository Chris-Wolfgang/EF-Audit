using System.Globalization;

namespace Wolfgang.Audit.Serializers;

/// <summary>
/// v1 default <see cref="IAuditValueSerializer"/>. Writes every value as culture-invariant
/// text to the <c>ValueText</c> column on the <c>AuditDetail</c> table.
/// </summary>
/// <remarks>
/// <para>
/// Encoding rules:
/// </para>
/// <list type="bullet">
///   <item><description><c>String</c> stored as-is.</description></item>
///   <item><description>Integral, decimal, and floating-point types use <see cref="CultureInfo.InvariantCulture"/>.</description></item>
///   <item><description><see cref="DateTime"/> and <see cref="DateTimeOffset"/> use the round-trip <c>"o"</c> format.</description></item>
///   <item><description><see cref="Guid"/> uses the <c>"D"</c> format (32 digits separated by hyphens).</description></item>
///   <item><description><see cref="bool"/> renders as <c>True</c> / <c>False</c>.</description></item>
///   <item><description><c>byte[]</c> is base64-encoded.</description></item>
///   <item><description>Enums use <see cref="Enum.ToString()"/> (the symbolic name); the discriminator records the enum's full type name.</description></item>
///   <item><description><c>null</c> writes SQL <c>NULL</c>; the discriminator still records the declared CLR type.</description></item>
/// </list>
/// </remarks>
public sealed class StringAuditValueSerializer : IAuditValueSerializer
{
    /// <summary>The name of the single text column this serializer writes.</summary>
    public const string ValueColumnName = "ValueText";

    private static readonly IReadOnlyList<AuditValueColumn> _columns = new[]
    {
        new AuditValueColumn(Name: ValueColumnName),
    };

    /// <inheritdoc />
    public IReadOnlyList<AuditValueColumn> Columns => _columns;

    /// <inheritdoc />
    public string Encode(object? value, Type clrType, IAuditValueWriter writer)
    {
        ArgumentNullException.ThrowIfNull(clrType);
        ArgumentNullException.ThrowIfNull(writer);

        var underlying = Nullable.GetUnderlyingType(clrType) ?? clrType;
        var valueType = ResolveValueType(underlying);

        if (value is null)
        {
            writer.WriteText(ValueColumnName, value: null);
            return valueType;
        }

        var text = FormatValue(value, underlying);
        writer.WriteText(ValueColumnName, text);
        return valueType;
    }

    private static string? FormatValue(object value, Type underlying)
    {
        if (underlying.IsEnum)
        {
            return value.ToString();
        }

        return value switch
        {
            string s => s,
            bool b => b ? "True" : "False",
            Guid g => g.ToString("D", CultureInfo.InvariantCulture),
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            byte[] bytes => Convert.ToBase64String(bytes),
            IFormattable formattable => formattable.ToString(format: null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
    }

    /// <inheritdoc />
    public object? Decode(IAuditValueReader reader, string valueType)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(valueType);

        var text = reader.ReadText(ValueColumnName);
        if (text is null)
        {
            return null;
        }

        if (valueType.StartsWith("Enum:", StringComparison.Ordinal))
        {
            var enumTypeName = valueType.Substring("Enum:".Length);
            var enumType = Type.GetType(enumTypeName, throwOnError: false);
            return enumType is not null
                ? Enum.Parse(enumType, text)
                : text;
        }

        return valueType switch
        {
            "String" => text,
            "Boolean" => bool.Parse(text),
            "Byte" => byte.Parse(text, CultureInfo.InvariantCulture),
            "SByte" => sbyte.Parse(text, CultureInfo.InvariantCulture),
            "Int16" => short.Parse(text, CultureInfo.InvariantCulture),
            "UInt16" => ushort.Parse(text, CultureInfo.InvariantCulture),
            "Int32" => int.Parse(text, CultureInfo.InvariantCulture),
            "UInt32" => uint.Parse(text, CultureInfo.InvariantCulture),
            "Int64" => long.Parse(text, CultureInfo.InvariantCulture),
            "UInt64" => ulong.Parse(text, CultureInfo.InvariantCulture),
            "Single" => float.Parse(text, CultureInfo.InvariantCulture),
            "Double" => double.Parse(text, CultureInfo.InvariantCulture),
            "Decimal" => decimal.Parse(text, CultureInfo.InvariantCulture),
            "Guid" => Guid.ParseExact(text, "D"),
            "DateTime" => DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            "DateTimeOffset" => DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
            "Bytes" => Convert.FromBase64String(text),
            _ => text,
        };
    }

    private static string ResolveValueType(Type underlying)
    {
        if (underlying.IsEnum)
        {
            return "Enum:" + underlying.FullName;
        }

        if (underlying == typeof(byte[]))
        {
            return "Bytes";
        }

        return underlying.Name;
    }
}
