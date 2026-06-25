#if NET8_0_OR_GREATER
using Wolfgang.Audit.Schema;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Exercises the auto-generated equality of <see cref="AuditMigrationsModelCacheKey"/>.
/// EF Core's model cache uses these keys for hit/miss decisions, and the
/// cache key only distinguishes contexts when its <c>Equals</c> /
/// <c>GetHashCode</c> behave correctly.
/// </summary>
public class AuditMigrationsModelCacheKeyTests
{
    [Fact]
    public void Equal_keys_have_equal_hash_codes()
    {
        var a = new AuditMigrationsModelCacheKey(typeof(AuditMigrationsDbContext), DesignTime: false, Schema: "audit", HeaderTableName: "H", DetailTableName: "D");
        var b = new AuditMigrationsModelCacheKey(typeof(AuditMigrationsDbContext), DesignTime: false, Schema: "audit", HeaderTableName: "H", DetailTableName: "D");

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }



    [Theory]
    [InlineData(null,    "H",  "D",  "audit", "H",  "D")]    // schema differs
    [InlineData("audit", "H",  "D",  "audit", "H2", "D")]    // header differs
    [InlineData("audit", "H",  "D",  "audit", "H",  "D2")]   // detail differs
    public void Keys_with_different_overrides_are_not_equal(
        string? schemaA, string headerA, string detailA,
        string? schemaB, string headerB, string detailB)
    {
        var a = new AuditMigrationsModelCacheKey(typeof(AuditMigrationsDbContext), DesignTime: false, schemaA, headerA, detailA);
        var b = new AuditMigrationsModelCacheKey(typeof(AuditMigrationsDbContext), DesignTime: false, schemaB, headerB, detailB);

        Assert.NotEqual(a, b);
    }



    [Fact]
    public void Property_getters_return_constructor_values()
    {
        var key = new AuditMigrationsModelCacheKey(typeof(AuditMigrationsDbContext), DesignTime: true, Schema: "s", HeaderTableName: "h", DetailTableName: "d");

        Assert.Equal(typeof(AuditMigrationsDbContext), key.ContextType);
        Assert.True(key.DesignTime);
        Assert.Equal("s", key.Schema);
        Assert.Equal("h", key.HeaderTableName);
        Assert.Equal("d", key.DetailTableName);
    }



    [Fact]
    public void Keys_with_different_design_time_flag_are_not_equal()
    {
        var a = new AuditMigrationsModelCacheKey(typeof(AuditMigrationsDbContext), DesignTime: false, Schema: null, HeaderTableName: "H", DetailTableName: "D");
        var b = new AuditMigrationsModelCacheKey(typeof(AuditMigrationsDbContext), DesignTime: true,  Schema: null, HeaderTableName: "H", DetailTableName: "D");

        Assert.NotEqual(a, b);
    }
}
#endif
