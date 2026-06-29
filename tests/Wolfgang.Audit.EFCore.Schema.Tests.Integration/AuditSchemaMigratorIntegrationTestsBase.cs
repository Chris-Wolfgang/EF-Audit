using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit;
using Wolfgang.Audit.EFCore.Schema.Tests.Integration.TestSupport;
using Wolfgang.Audit.Schema;
using Wolfgang.Audit.Serializers;
using Xunit;

namespace Wolfgang.Audit.EFCore.Schema.Tests.Integration;



/// <summary>
/// Shared facts that <see cref="AuditSchemaMigrator"/> must satisfy on every
/// relational provider. Each concrete subclass binds the same body to a
/// provider-specific <see cref="ISchemaProviderFixture"/> via xunit's
/// <see cref="IClassFixture{TFixture}"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public abstract class AuditSchemaMigratorIntegrationTestsBase
{
    private readonly ISchemaProviderFixture _fixture;



    protected AuditSchemaMigratorIntegrationTestsBase(ISchemaProviderFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }



    private static AuditOptions BuildOptions(string? schema = null, string? header = null, string? detail = null) => new()
    {
        Schema              = schema,
        HeaderTableName     = header ?? "AuditHeader",
        DetailTableName     = detail ?? "AuditDetail",
        ValueSerializer     = new StringAuditValueSerializer(),
        EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
    };



    [Fact]
    public async Task RunAsync_on_fresh_database_creates_audit_tables_and_stamps_version()
    {
        var options = BuildOptions();
        await using var context = await _fixture.CreateContextAsync(options);

        await AuditSchemaMigrator.RunAsync(context);

        var tables = await _fixture.ListTablesAsync(schema: null);
        Assert.Contains(tables, t => string.Equals(t.Name, "AuditHeader", StringComparison.Ordinal));
        Assert.Contains(tables, t => string.Equals(t.Name, "AuditDetail", StringComparison.Ordinal));
        Assert.Contains(tables, t => string.Equals(t.Name, AuditSchemaConstants.VersionTableName, StringComparison.Ordinal));

        var version = await context.Set<AuditSchemaVersion>().AsNoTracking().SingleAsync();
        Assert.Equal(AuditSchemaConstants.CurrentSchemaVersion, version.Version);
    }



    [Fact]
    public async Task RunAsync_honors_custom_schema_and_table_names()
    {
        var options = BuildOptions(
            schema: _fixture.CustomSchema,
            header: "CustomHeader",
            detail: "CustomDetail");

        await using var context = await _fixture.CreateContextAsync(options);

        await AuditSchemaMigrator.RunAsync(context);

        var tables = await _fixture.ListTablesAsync(_fixture.CustomSchema);
        Assert.Contains(tables, t => string.Equals(t.Schema, _fixture.CustomSchema, StringComparison.Ordinal) && string.Equals(t.Name, "CustomHeader", StringComparison.Ordinal));
        Assert.Contains(tables, t => string.Equals(t.Schema, _fixture.CustomSchema, StringComparison.Ordinal) && string.Equals(t.Name, "CustomDetail", StringComparison.Ordinal));
        Assert.Contains(tables, t => string.Equals(t.Schema, _fixture.CustomSchema, StringComparison.Ordinal) && string.Equals(t.Name, AuditSchemaConstants.VersionTableName, StringComparison.Ordinal));

        // And no spurious AuditHeader/AuditDetail under any schema.
        Assert.DoesNotContain(tables, t => string.Equals(t.Name, "AuditHeader", StringComparison.Ordinal));
        Assert.DoesNotContain(tables, t => string.Equals(t.Name, "AuditDetail", StringComparison.Ordinal));
    }



    [Fact]
    public async Task RunAsync_is_idempotent_when_called_twice()
    {
        var options = BuildOptions();
        await using var context = await _fixture.CreateContextAsync(options);

        await AuditSchemaMigrator.RunAsync(context);
        // If the migrator tried to re-run the CREATE TABLEs it would throw
        // (object already exists). A clean no-op proves the version-check
        // early-out works on the real provider.
        await AuditSchemaMigrator.RunAsync(context);

        var version = await context.Set<AuditSchemaVersion>().AsNoTracking().SingleAsync();
        Assert.Equal(AuditSchemaConstants.CurrentSchemaVersion, version.Version);
    }



    [Fact]
    public async Task RunAsync_dryRun_returns_script_without_creating_tables()
    {
        var options = BuildOptions();
        await using var context = await _fixture.CreateContextAsync(options);

        var script = await AuditSchemaMigrator.RunAsync(context, dryRun: true);

        Assert.False(string.IsNullOrWhiteSpace(script));
        Assert.Contains("AuditHeader", script, StringComparison.Ordinal);
        Assert.Contains("AuditDetail", script, StringComparison.Ordinal);

        var tables = await _fixture.ListTablesAsync(schema: null);
        Assert.DoesNotContain(tables, t => string.Equals(t.Name, "AuditHeader", StringComparison.Ordinal));
        Assert.DoesNotContain(tables, t => string.Equals(t.Name, "AuditDetail", StringComparison.Ordinal));
        Assert.DoesNotContain(tables, t => string.Equals(t.Name, AuditSchemaConstants.VersionTableName, StringComparison.Ordinal));
    }
}
