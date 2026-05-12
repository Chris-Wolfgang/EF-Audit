using System.Diagnostics.CodeAnalysis;

namespace Wolfgang.Audit.Tests.Smoke.TestSupport;

[ExcludeFromCodeCoverage]
public class Order
{
    public int OrderId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal Total { get; set; }

    public string Status { get; set; } = "Pending";
}
