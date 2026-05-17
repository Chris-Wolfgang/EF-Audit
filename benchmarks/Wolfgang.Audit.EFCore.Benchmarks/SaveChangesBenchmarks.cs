using System.Data.Common;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit.Benchmarks;

/// <summary>
/// Compares <c>SaveChangesAsync</c> (unaudited baseline) with
/// <c>SaveChangesWithAuditAsync</c> across Insert, Lifecycle, and MixedStates
/// workloads. SQLite is used for a consistent, dependency-free baseline; the
/// relative delta between the two is what matters, not the absolute numbers.
/// </summary>
[MemoryDiagnoser]
public class SaveChangesBenchmarks
{
    private DbConnection _connection = null!;
    private AuditOptions _options = null!;
    private StaticAuditUserProvider _userProvider = null!;
    private Customer[] _existingRows = System.Array.Empty<Customer>();

    [Params(1, 10, 50)]
    public int BatchSize { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _options = new AuditOptions
        {
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        _userProvider = new StaticAuditUserProvider();

        using var seed = CreateAuditedContext();
        seed.Database.EnsureCreated();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection.Dispose();
    }

    /// <summary>
    /// Reset Customer and audit tables before every Insert / Lifecycle iteration so
    /// each measurement starts from identical empty state. Without this, the audited
    /// variant's audit tables would grow across iterations and skew the delta.
    /// </summary>
    [IterationSetup(Targets = new[]
    {
        nameof(Insert_without_audit),
        nameof(Insert_with_audit),
        nameof(Lifecycle_without_audit),
        nameof(Lifecycle_with_audit),
    })]
    public void ResetTablesBeforeInsertAndLifecycle()
    {
        TruncateAllTables();
    }

    /// <summary>
    /// MixedStates variants need pre-existing rows to modify / delete in the
    /// measured save. Seeding happens here (outside the measurement window), via
    /// the unaudited context so the audit tables stay empty going into the
    /// measured call.
    /// </summary>
    [IterationSetup(Targets = new[]
    {
        nameof(MixedStates_per_save_without_audit),
        nameof(MixedStates_per_save_with_audit),
    })]
    public void ResetAndSeedBeforeMixedStates()
    {
        TruncateAllTables();
        SeedExistingRowsUnaudited();
    }

    private void TruncateAllTables()
    {
        using var ctx = CreateUnauditedContext();
#pragma warning disable EF1002 // Static SQL with no user input.
        ctx.Database.ExecuteSqlRaw("DELETE FROM Customers");
        ctx.Database.ExecuteSqlRaw("DELETE FROM AuditDetail");
        ctx.Database.ExecuteSqlRaw("DELETE FROM AuditHeader");
#pragma warning restore EF1002
    }

    private void SeedExistingRowsUnaudited()
    {
        using var seedCtx = CreateUnauditedContext();
        var rows = new Customer[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            rows[i] = new Customer { Name = $"E{i}", LoyaltyPoints = i };
            seedCtx.Customers.Add(rows[i]);
        }
        seedCtx.SaveChanges();
        _existingRows = rows;
    }

    private AuditedBenchmarkDbContext CreateAuditedContext()
    {
        return new AuditedBenchmarkDbContext(
            new DbContextOptionsBuilder<AuditedBenchmarkDbContext>()
                .UseSqlite(_connection)
                .Options,
            _options);
    }

    private UnauditedBenchmarkDbContext CreateUnauditedContext()
    {
        return new UnauditedBenchmarkDbContext(
            new DbContextOptionsBuilder<UnauditedBenchmarkDbContext>()
                .UseSqlite(_connection)
                .Options);
    }

    private void SaveAudited(AuditedBenchmarkDbContext ctx)
        => ctx.SaveChangesWithAuditAsync(_userProvider, _options).GetAwaiter().GetResult();

    [Benchmark(Baseline = true)]
    public void Insert_without_audit()
    {
        using var ctx = CreateUnauditedContext();
        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Add(new Customer { Name = $"User{i}", Email = $"u{i}@x.com", LoyaltyPoints = i });
        }
        ctx.SaveChanges();
    }

    [Benchmark]
    public void Insert_with_audit()
    {
        using var ctx = CreateAuditedContext();
        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Add(new Customer { Name = $"User{i}", Email = $"u{i}@x.com", LoyaltyPoints = i });
        }
        SaveAudited(ctx);
    }

    [Benchmark]
    public void Lifecycle_without_audit()
    {
        using var ctx = CreateUnauditedContext();
        var rows = new Customer[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            rows[i] = new Customer { Name = $"L{i}", Email = $"l{i}@x.com", LoyaltyPoints = i };
            ctx.Customers.Add(rows[i]);
        }
        ctx.SaveChanges();

        for (var i = 0; i < BatchSize; i++)
        {
            rows[i].Email = $"updated-{i}@x.com";
        }
        ctx.SaveChanges();

        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Remove(rows[i]);
        }
        ctx.SaveChanges();
    }

    [Benchmark]
    public void Lifecycle_with_audit()
    {
        using var ctx = CreateAuditedContext();
        var rows = new Customer[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            rows[i] = new Customer { Name = $"L{i}", Email = $"l{i}@x.com", LoyaltyPoints = i };
            ctx.Customers.Add(rows[i]);
        }
        SaveAudited(ctx);

        for (var i = 0; i < BatchSize; i++)
        {
            rows[i].Email = $"updated-{i}@x.com";
        }
        SaveAudited(ctx);

        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Remove(rows[i]);
        }
        SaveAudited(ctx);
    }

    [Benchmark]
    public void MixedStates_per_save_without_audit()
    {
        // _existingRows holds BatchSize rows seeded by ResetAndSeedBeforeMixedStates.
        // The measured save modifies the first half, deletes the second half, and
        // inserts BatchSize new rows — 2*BatchSize operations in a single save.
        using var ctx = CreateUnauditedContext();
        ctx.Customers.AttachRange(_existingRows);
        for (var i = 0; i < BatchSize / 2; i++)
        {
            _existingRows[i].Email = $"u{i}@x.com";
            ctx.Entry(_existingRows[i]).State = EntityState.Modified;
        }
        for (var i = BatchSize / 2; i < BatchSize; i++)
        {
            ctx.Customers.Remove(_existingRows[i]);
        }
        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Add(new Customer { Name = $"N{i}", LoyaltyPoints = i });
        }
        ctx.SaveChanges();
    }

    [Benchmark]
    public void MixedStates_per_save_with_audit()
    {
        using var ctx = CreateAuditedContext();
        ctx.Customers.AttachRange(_existingRows);
        for (var i = 0; i < BatchSize / 2; i++)
        {
            _existingRows[i].Email = $"u{i}@x.com";
            ctx.Entry(_existingRows[i]).State = EntityState.Modified;
        }
        for (var i = BatchSize / 2; i < BatchSize; i++)
        {
            ctx.Customers.Remove(_existingRows[i]);
        }
        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Add(new Customer { Name = $"N{i}", LoyaltyPoints = i });
        }
        SaveAudited(ctx);
    }
}
