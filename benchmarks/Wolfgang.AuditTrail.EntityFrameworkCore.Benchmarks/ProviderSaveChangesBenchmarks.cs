using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Testcontainers.PostgreSql;
using Wolfgang.AuditTrail.Serializers;

namespace Wolfgang.AuditTrail.Benchmarks;



/// <summary>
/// Which database engine a benchmark iteration runs against.
/// </summary>
public enum BenchmarkProvider
{
    Sqlite,
    SqlServer,
    PostgreSQL,
}



/// <summary>
/// Compares unaudited <c>SaveChangesAsync</c> against audited
/// <c>SaveChangesAsync</c> (via <see cref="AuditingDbContext"/>) across each of
/// the supported providers. Each <see cref="BenchmarkProvider"/> value spins
/// up its own engine — Testcontainers for SQL Server / PostgreSQL, in-memory
/// for SQLite — in <c>GlobalSetup</c> and reuses it across iterations.
/// </summary>
/// <remarks>
/// <para>
/// <strong>MySQL is intentionally excluded.</strong> Pomelo
/// (<c>Pomelo.EntityFrameworkCore.MySql</c>) ships stable for EF Core 8 and 9
/// (latest stable as of 2026-05: <c>9.0.0</c>) but no EF Core 10 release yet,
/// while this benchmark project targets net10.0 / EF Core 10. Re-add once
/// Pomelo ships a 10.x stable; the BenchmarkProvider enum just needs a MySQL
/// value and the GlobalSetup switch needs the container wiring.
/// </para>
/// <para>
/// <strong>Docker required</strong> for SQL Server and PostgreSQL iterations
/// — same prerequisite as Tests.Integration. SQLite iterations run with no
/// external dependency.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class ProviderSaveChangesBenchmarks
{
    private MsSqlContainer? _sqlServerContainer;
    private PostgreSqlContainer? _postgresContainer;
    private Microsoft.Data.Sqlite.SqliteConnection? _sqliteConnection;
    private string _connectionString = string.Empty;

    private AuditOptions _options = null!;
    private StaticAuditUserProvider _userProvider = null!;
    private Customer[] _existingRows = Array.Empty<Customer>();



    [Params(BenchmarkProvider.Sqlite, BenchmarkProvider.SqlServer, BenchmarkProvider.PostgreSQL)]
    public BenchmarkProvider Provider { get; set; }


    [Params(1, 10, 50)]
    public int BatchSize { get; set; }



    [GlobalSetup]
    public async Task GlobalSetup()
    {
        _options = new AuditOptions
        {
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        _userProvider = new StaticAuditUserProvider();

        switch (Provider)
        {
            case BenchmarkProvider.Sqlite:
                _sqliteConnection = new Microsoft.Data.Sqlite.SqliteConnection("Filename=:memory:");
                await _sqliteConnection.OpenAsync().ConfigureAwait(false);
                _connectionString = _sqliteConnection.ConnectionString;
                break;

            case BenchmarkProvider.SqlServer:
                // Pin to the same exact image as Tests.Integration's SqlServerFixture
                // so benchmark numbers can't drift when MSFT publishes a new "latest"
                // build under us. Bump intentionally + record a baseline reset when
                // updating.
                _sqlServerContainer = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
                    .Build();
                await _sqlServerContainer.StartAsync().ConfigureAwait(false);
                _connectionString = _sqlServerContainer.GetConnectionString();
                break;

            case BenchmarkProvider.PostgreSQL:
                // Pin to the same exact image as Tests.Integration's PostgresFixture
                // for the same reproducibility reason as the SQL Server case above.
                _postgresContainer = new PostgreSqlBuilder("postgres:16.4-alpine3.20")
                    .WithDatabase("auditbench")
                    .Build();
                await _postgresContainer.StartAsync().ConfigureAwait(false);
                _connectionString = _postgresContainer.GetConnectionString();
                break;

            default:
                throw new NotSupportedException($"Unknown provider {Provider}");
        }

        using var seed = CreateAuditedContext();
        await seed.Database.EnsureCreatedAsync().ConfigureAwait(false);
    }



    [GlobalCleanup]
    public async Task GlobalCleanup()
    {
        if (_sqlServerContainer is not null)
        {
            await _sqlServerContainer.DisposeAsync().ConfigureAwait(false);
        }
        if (_postgresContainer is not null)
        {
            await _postgresContainer.DisposeAsync().ConfigureAwait(false);
        }
        if (_sqliteConnection is not null)
        {
            await _sqliteConnection.DisposeAsync().ConfigureAwait(false);
        }
    }



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
        // Provider-specific identifier quoting; DELETE works on all three.
        var customerTable = Provider switch
        {
            BenchmarkProvider.PostgreSQL => "\"Customers\"",
            _ => "Customers",
        };
        var detailTable = Provider switch
        {
            BenchmarkProvider.PostgreSQL => "\"AuditDetail\"",
            _ => "AuditDetail",
        };
        var headerTable = Provider switch
        {
            BenchmarkProvider.PostgreSQL => "\"AuditHeader\"",
            _ => "AuditHeader",
        };
#pragma warning disable EF1002 // Static SQL with no user input.
        ctx.Database.ExecuteSqlRaw($"DELETE FROM {detailTable}");
        ctx.Database.ExecuteSqlRaw($"DELETE FROM {headerTable}");
        ctx.Database.ExecuteSqlRaw($"DELETE FROM {customerTable}");
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
        var builder = new DbContextOptionsBuilder<AuditedBenchmarkDbContext>();
        ApplyProvider(builder);
        return new AuditedBenchmarkDbContext(builder.Options, _userProvider, _options);
    }



    private UnauditedBenchmarkDbContext CreateUnauditedContext()
    {
        var builder = new DbContextOptionsBuilder<UnauditedBenchmarkDbContext>();
        ApplyProvider(builder);
        return new UnauditedBenchmarkDbContext(builder.Options);
    }



    private void ApplyProvider<TContext>(DbContextOptionsBuilder<TContext> builder)
        where TContext : DbContext
    {
        switch (Provider)
        {
            case BenchmarkProvider.Sqlite:
                builder.UseSqlite(_sqliteConnection!);
                break;
            case BenchmarkProvider.SqlServer:
                builder.UseSqlServer(_connectionString);
                break;
            case BenchmarkProvider.PostgreSQL:
                builder.UseNpgsql(_connectionString);
                break;
            default:
                throw new NotSupportedException($"Unknown provider {Provider}");
        }
    }



    [Benchmark(Baseline = true)]
    public async Task Insert_without_audit()
    {
        using var ctx = CreateUnauditedContext();
        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Add(new Customer { Name = $"User{i}", Email = $"u{i}@x.com", LoyaltyPoints = i });
        }
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }



    [Benchmark]
    public async Task Insert_with_audit()
    {
        using var ctx = CreateAuditedContext();
        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Add(new Customer { Name = $"User{i}", Email = $"u{i}@x.com", LoyaltyPoints = i });
        }
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }



    [Benchmark]
    public async Task Lifecycle_without_audit()
    {
        using var ctx = CreateUnauditedContext();
        var rows = new Customer[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            rows[i] = new Customer { Name = $"L{i}", Email = $"l{i}@x.com", LoyaltyPoints = i };
            ctx.Customers.Add(rows[i]);
        }
        await ctx.SaveChangesAsync().ConfigureAwait(false);

        for (var i = 0; i < BatchSize; i++)
        {
            rows[i].Email = $"updated-{i}@x.com";
        }
        await ctx.SaveChangesAsync().ConfigureAwait(false);

        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Remove(rows[i]);
        }
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }



    [Benchmark]
    public async Task Lifecycle_with_audit()
    {
        using var ctx = CreateAuditedContext();
        var rows = new Customer[BatchSize];
        for (var i = 0; i < BatchSize; i++)
        {
            rows[i] = new Customer { Name = $"L{i}", Email = $"l{i}@x.com", LoyaltyPoints = i };
            ctx.Customers.Add(rows[i]);
        }
        await ctx.SaveChangesAsync().ConfigureAwait(false);

        for (var i = 0; i < BatchSize; i++)
        {
            rows[i].Email = $"updated-{i}@x.com";
        }
        await ctx.SaveChangesAsync().ConfigureAwait(false);

        for (var i = 0; i < BatchSize; i++)
        {
            ctx.Customers.Remove(rows[i]);
        }
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }



    [Benchmark]
    public async Task MixedStates_per_save_without_audit()
    {
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
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }



    [Benchmark]
    public async Task MixedStates_per_save_with_audit()
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
        await ctx.SaveChangesAsync().ConfigureAwait(false);
    }
}
