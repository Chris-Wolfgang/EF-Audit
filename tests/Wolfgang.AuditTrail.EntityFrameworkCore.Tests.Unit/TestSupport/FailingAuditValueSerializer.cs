using System.Diagnostics.CodeAnalysis;
using Wolfgang.AuditTrail.Serializers;

namespace Wolfgang.AuditTrail.Tests.Unit.TestSupport;

/// <summary>
/// Wraps <see cref="StringAuditValueSerializer"/> and throws unconditionally on
/// every <see cref="Encode"/> call, simulating a corrupt or buggy serializer.
/// Used to verify that an audit-save failure rolls back the user's data save
/// (atomicity contract).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class FailingAuditValueSerializer : IAuditValueSerializer
{
    private readonly StringAuditValueSerializer _inner = new();

    public IReadOnlyList<AuditValueColumn> Columns => _inner.Columns;

    public string Encode(object? value, Type clrType, IAuditValueWriter writer)
        => throw new InvalidOperationException("Simulated audit serializer failure.");

    public object? Decode(IAuditValueReader reader, string valueType)
        => _inner.Decode(reader, valueType);
}
