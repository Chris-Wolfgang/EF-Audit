using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;

/// <summary>
/// Verifies the on-disk format of audit columns by issuing raw SQL against the
/// underlying connection — i.e. tests that the column literally contains
/// <c>'I'</c>/<c>'U'</c>/<c>'D'</c> for the operation flag, not the numeric byte
/// values (73/85/68) the original byte-conversion produced.
/// </summary>
public class AuditStorageFormatTests
{
    [Theory]
    [InlineData("I")]
    [InlineData("U")]
    [InlineData("D")]
    public async Task AuditHeader_Operation_column_stores_the_literal_character(string expected)
    {
        using var fixture = new AuditFixture();

        await using (var context = fixture.CreateContext())
        {
            var customer = new Customer { Name = "Alice" };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            if (string.Equals(expected, "U", StringComparison.Ordinal))
            {
                customer.Email = "alice@example.com";
                await context.SaveChangesAsync();
            }
            else if (string.Equals(expected, "D", StringComparison.Ordinal))
            {
                context.Customers.Remove(customer);
                await context.SaveChangesAsync();
            }
        }

        await using var verify = fixture.CreateContext();
        var connection = verify.Database.GetDbConnection();
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"SELECT Operation FROM AuditHeader WHERE Operation = '{expected}'";
        var stored = await cmd.ExecuteScalarAsync();

        Assert.Equal(expected, stored?.ToString());
    }
}
