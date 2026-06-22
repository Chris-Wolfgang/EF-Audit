using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;

public class AuditNullAndCompositeKeyTests
{
    [Fact]
    public async Task SaveChangesAsync_when_string_property_is_null_writes_a_detail_row_with_SQL_NULL_and_String_ValueType()
    {
        using var fixture = new AuditFixture();

        await using (var context = fixture.CreateContext())
        {
            context.Customers.Add(new Customer { Name = "Alice", Email = null });
            await fixture.SaveAsync(context);
        }

        await using var verify = fixture.CreateContext();
        var header = await verify
            .Set<AuditHeader>()
            .Include(h => h.Details)
            .SingleAsync();

        var emailDetail = Assert.Single(header.Details, d => string.Equals(d.ColumnName, "Email", StringComparison.Ordinal));
        Assert.Null(emailDetail.ValueText);
        Assert.Equal("String", emailDetail.ValueType);
    }

    [Fact]
    public async Task SaveChangesAsync_for_composite_key_entity_writes_header_with_pipe_joined_EntityKey()
    {
        using var fixture = new AuditFixture();

        await using (var context = fixture.CreateContext())
        {
            context.OrderLines.Add(new OrderLine { OrderId = 7, LineNumber = 3, Description = "Widget" });
            await fixture.SaveAsync(context);
        }

        await using var verify = fixture.CreateContext();
        var header = await verify
            .Set<AuditHeader>()
            .SingleAsync();

        Assert.Equal("7|3", header.EntityKey);
    }
}
