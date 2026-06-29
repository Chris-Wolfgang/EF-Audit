using System.Diagnostics.CodeAnalysis;

namespace Wolfgang.AuditTrail.Tests.Integration.TestSupport;

[ExcludeFromCodeCoverage]
public class Customer
{
    public int CustomerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }
}
