using System.Data.Common;
using BenchmarkDotNet.Attributes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit.Benchmarks;

/// <summary>
/// Compares <see cref="DbContext.SaveChanges"/> with and without the audit
/// interceptor across Insert, Update, Delete, and mixed workloads. SQLite is used
/// for a consistent, dependency-free baseline; the relative delta is what matters,
/// not absolute numbers.
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

    private BenchmarkDbContext CreateAuditedContext()
    {
        return new BenchmarkDbContext(
            new DbContextOptionsBuilder<BenchmarkDbContext>()
                .UseSqlite(_connection)
                .AddInterceptors(_interceptor)
                .Options,
            _options);
    }

    private BenchmarkDbContext CreateUnauditedContext()
    {
        return new BenchmarkDbContext(
            new DbContextOptionsBuilder<BenchmarkDbContext>()
                .UseSqlite(_connection)
                .Options,
            auditOptions: null);
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
}
