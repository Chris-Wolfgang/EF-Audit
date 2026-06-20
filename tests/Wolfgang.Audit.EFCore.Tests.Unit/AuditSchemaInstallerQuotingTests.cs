using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Schema;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Locks in the provider-specific identifier quoting added in response to
/// the PR #2 review cluster: unquoted PascalCase identifiers fold to
/// lowercase on PostgreSQL, so <c>DROP TABLE AuditHeader</c> silently
/// targets a non-existent <c>auditheader</c> and leaves the real
/// <c>"AuditHeader"</c> orphaned.
/// </summary>
public class AuditSchemaInstallerQuotingTests
{
    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer", null,    "AuditHeader",  "[AuditHeader]")]
    [InlineData("Microsoft.EntityFrameworkCore.SqlServer", "audit", "AuditHeader",  "[audit].[AuditHeader]")]
    [InlineData("Npgsql.EntityFrameworkCore.PostgreSQL",   null,    "AuditHeader",  "\"AuditHeader\"")]
    [InlineData("Npgsql.EntityFrameworkCore.PostgreSQL",   "audit", "AuditHeader",  "\"audit\".\"AuditHeader\"")]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite",    null,    "AuditDetail",  "\"AuditDetail\"")]
    [InlineData("Microsoft.EntityFrameworkCore.Sqlite",    "main",  "AuditDetail",  "\"main\".\"AuditDetail\"")]
    [InlineData("Pomelo.EntityFrameworkCore.MySql",        null,    "AuditHeader",  "`AuditHeader`")]
    [InlineData("Pomelo.EntityFrameworkCore.MySql",        "audit", "AuditHeader",  "`audit`.`AuditHeader`")]
    [InlineData("MySql.EntityFrameworkCore",               "audit", "AuditDetail",  "`audit`.`AuditDetail`")]
    [InlineData(null,                                       null,    "AuditHeader",  "\"AuditHeader\"")] // unknown provider → ANSI default
    [InlineData("",                                         "audit", "AuditHeader",  "\"audit\".\"AuditHeader\"")] // empty → ANSI default
    public void QuoteIdentifier_uses_provider_specific_syntax(string? providerName, string? schema, string table, string expected)
    {
        Assert.Equal(expected, AuditSchemaInstaller.QuoteIdentifier(providerName, schema, table));
    }



    [Fact]
    public async Task DropTablesAsync_round_trips_with_quoted_identifiers_on_sqlite()
    {
        // Regression check: with the quoted form ("AuditHeader"), SQLite still
        // resolves to the EF-created table and DROP IF EXISTS succeeds. Prior
        // bug — unquoted on PG — would have folded the name and silently no-op'd.
        using var fixture = new AuditFixture();
        var installer = new AuditSchemaInstaller(fixture.Options);

        await using (var context = fixture.CreateContext())
        {
            Assert.Empty(await context.Set<AuditHeader>().ToListAsync());
            await installer.DropTablesAsync(context);
        }

        await using var verify = fixture.CreateContext();
        var ex = await Record.ExceptionAsync(async () => await verify.Set<AuditHeader>().ToListAsync());
        Assert.NotNull(ex);
    }
}
