using Microsoft.EntityFrameworkCore;
using Wolfgang.AuditTrail.Entities;
using Wolfgang.AuditTrail.Schema;
using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Covers <see cref="AuditSchemaInstaller.DropTablesAsync"/> and the identifier
/// validation paths that protect the raw-SQL <c>DROP TABLE</c> statement.
/// Issue #30 noted that <c>DropTablesAsync</c> had 0% coverage and the
/// installer as a whole was at 33%.
/// </summary>
public class AuditSchemaInstallerDropTests
{
    [Fact]
    public async Task DropTablesAsync_round_trips_with_CreateTablesAsync()
    {
        using var fixture = new AuditFixture(createOnConstruct: false);
        var installer = new AuditSchemaInstaller(fixture.Options);

        await using (var context = fixture.CreateContext())
        {
#pragma warning disable CS0618 // Exercises the obsolete back-compat path.
            await installer.CreateTablesAsync(context);
#pragma warning restore CS0618

            // After create, querying the audit set should succeed and return zero rows.
            Assert.Empty(await context.Set<AuditHeader>().ToListAsync());

            await installer.DropTablesAsync(context);
        }

        // After drop, the table is gone — selecting from it should throw.
        await using var verify = fixture.CreateContext();
        var ex = await Record.ExceptionAsync(async () =>
            await verify.Set<AuditHeader>().ToListAsync());
        Assert.NotNull(ex);
    }



    [Fact]
    public async Task DropTablesAsync_throws_on_null_context()
    {
        var installer = new AuditSchemaInstaller(new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        });

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            installer.DropTablesAsync(context: null!));
    }



    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DropTablesAsync_throws_when_table_name_is_whitespace(string badName)
    {
        using var fixture = new AuditFixture(createOnConstruct: false);
        fixture.Options.HeaderTableName = badName;
        var installer = new AuditSchemaInstaller(fixture.Options);

        await using var context = fixture.CreateContext();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            installer.DropTablesAsync(context));
    }



    [Fact]
    public async Task DropTablesAsync_throws_when_table_name_contains_unsafe_characters()
    {
        using var fixture = new AuditFixture(createOnConstruct: false);
        // Attempts a SQL-injection-style identifier; the EnsureSafeIdentifier
        // helper must reject it before any SQL is interpolated.
        fixture.Options.HeaderTableName = "AuditHeader; DROP TABLE Foo;--";
        var installer = new AuditSchemaInstaller(fixture.Options);

        await using var context = fixture.CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            installer.DropTablesAsync(context));
    }



    [Fact]
    public async Task DropTablesAsync_throws_when_table_name_starts_with_digit()
    {
        using var fixture = new AuditFixture(createOnConstruct: false);
        fixture.Options.DetailTableName = "1Detail";
        var installer = new AuditSchemaInstaller(fixture.Options);

        await using var context = fixture.CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            installer.DropTablesAsync(context));
    }



    [Fact]
    public async Task DropTablesAsync_throws_when_schema_contains_unsafe_characters()
    {
        using var fixture = new AuditFixture(createOnConstruct: false);
        fixture.Options.Schema = "schema-with-dash";
        var installer = new AuditSchemaInstaller(fixture.Options);

        await using var context = fixture.CreateContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            installer.DropTablesAsync(context));
    }



    [Fact]
    public void Constructor_throws_on_null_options()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AuditSchemaInstaller(options: null!));
    }
}
