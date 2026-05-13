using System.Data.Common;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit.Benchmarks;

/// <summary>
/// Compares <c>SaveChanges</c> (unaudited baseline) against the audited variant
/// for Insert workloads. SQLite is used for a consistent, dependency-free
/// baseline; the relative delta between the two is what matters, not the
/// absolute numbers. Update / Delete / Mixed workloads land in a follow-up.
/// </summary>
[MemoryDiagnoser]
public class SaveChangesBenchmarks
{
    private DbConnection _connection = null!;
    private AuditOptions _options = null!;
    private AuditSaveChangesInterceptor _interceptor = null!;

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
        _interceptor = new AuditSaveChangesInterceptor(new StaticAuditUserProvider(), _options);

        using var seed = CreateAuditedContext();
        seed.Database.EnsureCreated();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _connection.Dispose();
    }

    private AuditedBenchmarkDbContext CreateAuditedContext()
    {
        return new AuditedBenchmarkDbContext(
            new DbContextOptionsBuilder<AuditedBenchmarkDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(_interceptor)
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
        ctx.SaveChanges();
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
    public void MixedStates_per_save_without_audit()
    {
        // First, seed half the batch so we have rows to update/delete in the measured save.
        using var seedCtx = CreateUnauditedContext();
        var existing = new Customer[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            existing[i] = new Customer { Name = $"E{i}", LoyaltyPoints = i };
            seedCtx.Customers.Add(existing[i]);
        }
        seedCtx.SaveChanges();
        seedCtx.Dispose();

        using var ctx = CreateUnauditedContext();
        ctx.Customers.AttachRange(existing);
        // Update first half.
        for (var i = 0; i < BatchSize / 2; i++)
        {
            existing[i].Email = $"u{i}@x.com";
            ctx.Entry(existing[i]).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        }
        // Delete second half.
        for (var i = BatchSize / 2; i < BatchSize; i++)
        {
            ctx.Customers.Remove(existing[i]);
        }
        // Insert new ones.
        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Add(new Customer { Name = $"N{i}", LoyaltyPoints = i });
        }
        ctx.SaveChanges();
    }

    [Benchmark]
    public void MixedStates_per_save_with_audit()
    {
        using var seedCtx = CreateAuditedContext();
        var existing = new Customer[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            existing[i] = new Customer { Name = $"E{i}", LoyaltyPoints = i };
            seedCtx.Customers.Add(existing[i]);
        }
        seedCtx.SaveChanges();
        seedCtx.Dispose();

        using var ctx = CreateAuditedContext();
        ctx.Customers.AttachRange(existing);
        for (var i = 0; i < BatchSize / 2; i++)
        {
            existing[i].Email = $"u{i}@x.com";
            ctx.Entry(existing[i]).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        }
        for (var i = BatchSize / 2; i < BatchSize; i++)
        {
            ctx.Customers.Remove(existing[i]);
        }
        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Add(new Customer { Name = $"N{i}", LoyaltyPoints = i });
        }
        ctx.SaveChanges();
    }
}
