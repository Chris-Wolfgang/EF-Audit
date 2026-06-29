using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Locks in the index / column-length shape changes from PR #2 cluster #4
/// (r3255402615 — composite index size bust on SQL Server, r3255402647 —
/// missing ColumnName index for the README's "every change to X" query).
/// </summary>
public class ModelBuilderIndexShapeTests
{
    [Fact]
    public void AuditHeader_EntityType_and_EntityKey_lengths_fit_SqlServer_nonclustered_limit()
    {
        using var fixture = new AuditFixture();
        using var ctx = fixture.CreateContext();
        var header = ctx.Model.FindEntityType(typeof(AuditHeader))!;

        var entityType = header.FindProperty(nameof(AuditHeader.EntityType))!;
        var entityKey  = header.FindProperty(nameof(AuditHeader.EntityKey))!;

        Assert.Equal(256, entityType.GetMaxLength());
        Assert.Equal(256, entityKey.GetMaxLength());

        // Combined nvarchar bytes = (256 + 256) * 2 = 1024 — well under
        // SQL Server's 1700-byte nonclustered key limit.
        var combinedNvarcharBytes = ((entityType.GetMaxLength() ?? 0) + (entityKey.GetMaxLength() ?? 0)) * 2;
        Assert.True(combinedNvarcharBytes <= 1700, $"Composite (EntityType, EntityKey) index would exceed SQL Server's 1700-byte limit: {combinedNvarcharBytes}");

        // Combined utf8mb4 bytes = 512 * 4 = 2048 — well under MySQL InnoDB's
        // 3072-byte limit.
        var combinedUtf8mb4Bytes = ((entityType.GetMaxLength() ?? 0) + (entityKey.GetMaxLength() ?? 0)) * 4;
        Assert.True(combinedUtf8mb4Bytes <= 3072, $"Composite (EntityType, EntityKey) index would exceed MySQL InnoDB's 3072-byte limit: {combinedUtf8mb4Bytes}");
    }



    [Fact]
    public void AuditHeader_has_composite_index_on_EntityType_and_EntityKey()
    {
        using var fixture = new AuditFixture();
        using var ctx = fixture.CreateContext();
        var header = ctx.Model.FindEntityType(typeof(AuditHeader))!;

        var composite = header.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 2 &&
            string.Equals(i.Properties[0].Name, nameof(AuditHeader.EntityType), StringComparison.Ordinal) &&
            string.Equals(i.Properties[1].Name, nameof(AuditHeader.EntityKey),  StringComparison.Ordinal));

        Assert.NotNull(composite);
    }



    [Fact]
    public void AuditDetail_has_standalone_index_on_ColumnName()
    {
        // README promises "find all changes to Customer.Email" — that requires
        // a standalone index on ColumnName. Without it, queries by column name
        // full-scan the detail table.
        using var fixture = new AuditFixture();
        using var ctx = fixture.CreateContext();
        var detail = ctx.Model.FindEntityType(typeof(AuditDetail))!;

        var byColumnName = detail.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 &&
            string.Equals(i.Properties[0].Name, nameof(AuditDetail.ColumnName), StringComparison.Ordinal));

        Assert.NotNull(byColumnName);
    }



    [Fact]
    public void AuditDetail_still_has_HeaderId_index()
    {
        // Regression check — the new ColumnName index must not displace the
        // existing HeaderId index used for header-rooted joins.
        using var fixture = new AuditFixture();
        using var ctx = fixture.CreateContext();
        var detail = ctx.Model.FindEntityType(typeof(AuditDetail))!;

        var byHeaderId = detail.GetIndexes().FirstOrDefault(i =>
            i.Properties.Count == 1 &&
            string.Equals(i.Properties[0].Name, nameof(AuditDetail.HeaderId), StringComparison.Ordinal));

        Assert.NotNull(byHeaderId);
    }
}
