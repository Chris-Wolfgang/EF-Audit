using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Exercises the <see cref="AuditingDbContext"/> constructor argument
/// validation. Each test instantiates <see cref="TestDbContext"/> (which
/// forwards through to <c>AuditingDbContext</c>) so the protected base
/// constructor's null-guards are reached.
/// </summary>
public class AuditingDbContextConstructorTests
{
    private static DbContextOptions<TestDbContext> BuildOptions()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        return new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connection).Options;
    }



    private static AuditOptions ValidOptions() => new()
    {
        ValueSerializer     = new StringAuditValueSerializer(),
        EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
    };



    [Fact]
    public void Constructor_throws_when_user_provider_is_null()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TestDbContext(BuildOptions(), userProvider: null!, ValidOptions()));
        Assert.Equal("userProvider", ex.ParamName);
    }



    [Fact]
    public void Constructor_throws_when_audit_options_is_null()
    {
        var userProvider = new StaticAuditUserProvider("u");

        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TestDbContext(BuildOptions(), userProvider, auditOptions: null!));
        Assert.Equal("auditOptions", ex.ParamName);
    }



    [Fact]
    public void Constructor_throws_when_value_serializer_is_null()
    {
        var userProvider = new StaticAuditUserProvider("u");
        var auditOptions = new AuditOptions
        {
            ValueSerializer     = null,
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            new TestDbContext(BuildOptions(), userProvider, auditOptions));
        Assert.Equal("auditOptions", ex.ParamName);
        Assert.Contains("ValueSerializer", ex.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void Constructor_throws_when_entity_key_serializer_is_null()
    {
        var userProvider = new StaticAuditUserProvider("u");
        var auditOptions = new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = null,
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            new TestDbContext(BuildOptions(), userProvider, auditOptions));
        Assert.Equal("auditOptions", ex.ParamName);
        Assert.Contains("EntityKeySerializer", ex.Message, StringComparison.Ordinal);
    }



    [Fact]
    public void AuditOptions_property_exposes_the_injected_options()
    {
        var userProvider = new StaticAuditUserProvider("u");
        var auditOptions = ValidOptions();

        using var context = new TestDbContext(BuildOptions(), userProvider, auditOptions);

        Assert.Same(auditOptions, context.AuditOptions);
    }
}
