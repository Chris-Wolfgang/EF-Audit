using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Regression coverage for PR #2 review comment cluster: <c>AuditDetail.ColumnName</c>
/// must reflect the EF Core mapped database column name, honouring
/// <c>[Column]</c> / <c>HasColumnName(...)</c> overrides — not the CLR
/// property name.
/// </summary>
public sealed class AuditCaptureColumnNameTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;
    private readonly AuditOptions _auditOptions;
    private readonly StaticAuditUserProvider _userProvider;



    public AuditCaptureColumnNameTests()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
        _connection.Open();

        _auditOptions = new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        _userProvider = new StaticAuditUserProvider("test-user");

        using var seed = CreateContext();
        seed.Database.EnsureCreated();
    }



    public void Dispose() => _connection.Dispose();



    private MappedColumnContext CreateContext()
    {
        var opts = new DbContextOptionsBuilder<MappedColumnContext>().UseSqlite(_connection).Options;
        return new MappedColumnContext(opts, _userProvider, _auditOptions);
    }



    [Fact]
    public async Task AuditDetail_ColumnName_uses_mapped_DB_column_when_Column_attribute_is_present()
    {
        await using (var ctx = CreateContext())
        {
            ctx.Items.Add(new MappedItem { DisplayName = "Hello" });
            await ctx.SaveChangesAsync();
        }

        await using var verify = CreateContext();
        var detail = await verify.Set<AuditDetail>().SingleAsync();
        // [Column("item_display")] on DisplayName -> ColumnName should be "item_display"
        // not the CLR property name "DisplayName".
        Assert.Equal("item_display", detail.ColumnName);
        Assert.Equal("Hello", detail.ValueText);
    }



    private sealed class MappedColumnContext : AuditingDbContext
    {
        public MappedColumnContext(DbContextOptions<MappedColumnContext> options, IAuditUserProvider provider, AuditOptions auditOptions)
            : base(options, provider, auditOptions)
        {
        }

        public DbSet<MappedItem> Items => Set<MappedItem>();
    }



    private sealed class MappedItem
    {
        public int Id { get; set; }

        [Column("item_display")]
        public string DisplayName { get; set; } = string.Empty;
    }
}
