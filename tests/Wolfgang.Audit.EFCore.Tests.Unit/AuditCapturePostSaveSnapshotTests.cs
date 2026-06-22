using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Serializers;
using Wolfgang.Audit.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.Audit.Tests.Unit;



/// <summary>
/// Regression coverage for PR #2 cluster #2: insert detail values used to be
/// snapshotted before the user's <c>SaveChanges</c>. Database-generated
/// columns (defaults, computed, identity) were therefore audited as the
/// pre-save CLR default rather than the post-save value the row actually
/// holds. <c>AuditCapture.ResolveDetailValue</c> now re-reads
/// <c>CurrentValue</c> from the still-tracked <c>EntityEntry</c> after the
/// save completes, so DB-generated values land in the audit row.
/// </summary>
public class AuditCapturePostSaveSnapshotTests
{
    [Fact]
    public async Task Insert_captures_db_generated_default_after_SaveChanges()
    {
        using var fixture = new GeneratedDefaultFixture();

        await using (var context = fixture.CreateContext())
        {
            // RowVersion is HasDefaultValueSql("42") — the CLR default for int is 0
            // before save; EF reads back 42 after the insert commits.
            context.Widgets.Add(new Widget { Name = "w1" });
            await context.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();

        var rowVersionDetail = await verify.Set<AuditDetail>()
            .Where(d => d.ColumnName == "RowVersion")
            .OrderByDescending(d => d.DetailId)
            .FirstAsync();

        // Pre-fix this would have been "0" (the CLR default at pre-save snapshot
        // time). The post-save read picks up the DB-applied default.
        Assert.Equal("42", rowVersionDetail.ValueText);
    }



    [Fact]
    public async Task Update_captures_modified_value_after_SaveChanges()
    {
        using var fixture = new GeneratedDefaultFixture();

        int widgetId;
        await using (var context = fixture.CreateContext())
        {
            var widget = new Widget { Name = "before" };
            context.Widgets.Add(widget);
            await context.SaveChangesAsync();
            widgetId = widget.WidgetId;
        }

        await using (var context = fixture.CreateContext())
        {
            var widget = await context.Widgets.SingleAsync(w => w.WidgetId == widgetId);
            widget.Name = "after";
            await context.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();

        var nameDetail = await verify.Set<AuditDetail>()
            .Where(d => d.ColumnName == "Name" && d.Header!.Operation == AuditOperation.Update)
            .OrderByDescending(d => d.DetailId)
            .FirstAsync();

        Assert.Equal("after", nameDetail.ValueText);
    }



    [Fact]
    public async Task Delete_captures_OriginalValue_from_pre_save_snapshot()
    {
        // Sanity check: deletes still work — the post-save re-read does NOT
        // apply to deletes (the entry detaches), so we go on the captured
        // OriginalValue from CapturePending.
        using var fixture = new GeneratedDefaultFixture(captureDeletedValues: true);

        int widgetId;
        await using (var context = fixture.CreateContext())
        {
            var widget = new Widget { Name = "to-delete" };
            context.Widgets.Add(widget);
            await context.SaveChangesAsync();
            widgetId = widget.WidgetId;
        }

        await using (var context = fixture.CreateContext())
        {
            var widget = await context.Widgets.SingleAsync(w => w.WidgetId == widgetId);
            context.Widgets.Remove(widget);
            await context.SaveChangesAsync();
        }

        await using var verify = fixture.CreateContext();

        var nameDetail = await verify.Set<AuditDetail>()
            .Where(d => d.ColumnName == "Name" && d.Header!.Operation == AuditOperation.Delete)
            .OrderByDescending(d => d.DetailId)
            .FirstAsync();

        Assert.Equal("to-delete", nameDetail.ValueText);
    }



    // ── Fixture and entity isolated from the shared TestDbContext ───────────

    [ExcludeFromCodeCoverage]
    private sealed class Widget
    {
        public int WidgetId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int RowVersion { get; set; }
    }



    [ExcludeFromCodeCoverage]
    private sealed class GeneratedDefaultDbContext : AuditingDbContext
    {
        public GeneratedDefaultDbContext
        (
            DbContextOptions<GeneratedDefaultDbContext> options,
            IAuditUserProvider userProvider,
            AuditOptions auditOptions
        )
            : base(options, userProvider, auditOptions)
        {
        }

        public DbSet<Widget> Widgets => Set<Widget>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ArgumentNullException.ThrowIfNull(modelBuilder);

            modelBuilder.Entity<Widget>(b =>
            {
                b.HasKey(w => w.WidgetId);
                // DB-side default — the whole point of the test. The CLR default
                // for int is 0; after save, EF reads back 42.
                b.Property(w => w.RowVersion).HasDefaultValueSql("42").ValueGeneratedOnAdd();
            });

            base.OnModelCreating(modelBuilder);
        }
    }



    [ExcludeFromCodeCoverage]
    private sealed class GeneratedDefaultFixture : IDisposable
    {
        private readonly DbConnection _connection;

        public GeneratedDefaultFixture(bool captureDeletedValues = false)
        {
            Options = new AuditOptions
            {
                CaptureDeletedValues = captureDeletedValues,
                ValueSerializer      = new StringAuditValueSerializer(),
                EntityKeySerializer  = new PipeDelimitedEntityKeySerializer(),
            };
            UserProvider = new StaticAuditUserProvider("u");

            _connection = new SqliteConnection("Filename=:memory:");
            _connection.Open();

            using var seed = CreateContext();
            seed.Database.EnsureCreated();
        }

        public AuditOptions Options { get; }
        public StaticAuditUserProvider UserProvider { get; }

        public GeneratedDefaultDbContext CreateContext()
        {
            var builder = new DbContextOptionsBuilder<GeneratedDefaultDbContext>()
                .UseSqlite(_connection);
            return new GeneratedDefaultDbContext(builder.Options, UserProvider, Options);
        }

        public void Dispose() => _connection.Dispose();
    }
}
