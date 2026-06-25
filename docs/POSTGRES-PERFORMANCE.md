# PostgreSQL audit-write performance

## TL;DR

If you're running `Wolfgang.Audit.EFCore` against PostgreSQL and seeing high
audit-pass latency at moderate batch sizes (10+ rows per `SaveChanges`),
set Npgsql's `MaxBatchSize` higher on your `UseNpgsql(...)` call:

```csharp
services.AddDbContext<AppDbContext>((sp, opts) =>
    opts.UseNpgsql(
        connectionString,
        npgsql => npgsql.MaxBatchSize(100))   // <-- the knob
);
```

A value of `100` is a reasonable starting point. `MaxBatchSize` caps how many
INSERT statements Npgsql will combine into a single batched command; the default
is conservative enough that audit rows from a 50-row save end up as ~100 round
trips. Raising it lets the audit pass collapse to a handful of multi-row INSERTs.

This is a consumer-side configuration. The library doesn't touch your
`DbContextOptionsBuilder` — the recommendation is to set this on your own
context registration.

---

## Why this matters

Benchmark data from `ProviderSaveChangesBenchmarks` shows PostgreSQL paying a
disproportionate time cost for audit writes compared to SQL Server and SQLite:

| Provider | Batch | Without audit | With audit | **Time ratio** | Alloc ratio |
|---|---:|---:|---:|---:|---:|
| SQLite | 50 | 2.2 ms | 15.7 ms | **7.2×** | 6.5× |
| SQL Server | 50 | 56.3 ms | 295.4 ms | **5.3×** | 5.6× |
| **PostgreSQL** | **50** | **4.1 ms** | **110.0 ms** | **26.7×** | **6.9×** |

Note the divergence: on SQLite and SQL Server, time-ratio tracks
allocation-ratio (linear scaling). On PostgreSQL, time-ratio is ~4×
allocation-ratio, suggesting per-statement overhead the audit pass amplifies.

The smaller-batch trend confirms the pattern:

| Provider | Batch 1 | Batch 10 | Batch 50 |
|---|---:|---:|---:|
| SQLite | 2.07× | 5.24× | 7.19× |
| SQL Server | 1.18× | 6.90× | 5.25× |
| PostgreSQL | 2.46× | **14.0×** | **26.7×** |

(Numbers from a 14700HX laptop, BDN `--job short`, EF Core 10.0.0,
PostgreSQL 16.4 via Testcontainers.)

---

## Mechanism

Each audited entity produces **1 header row + N detail rows** (one per changed
column). For a 50-row batch save with one changed column per entity, the audit
pass writes ~100 rows. Without batching, that's ~100 round-trips against the
PostgreSQL server.

- **SqlClient** (SQL Server) batches multi-row INSERTs aggressively by default
  — the audit pass collapses to a few `INSERT ... VALUES (...), (...), ...`
  statements.
- **Npgsql** has a `MaxBatchSize` that defaults to a low value. EF Core's
  Npgsql provider uses this to cap how many statements get sent in one
  command. Audit rows produced one-at-a-time via `Add()` (not `AddRange`) tend
  to fragment across many commands.
- **SQLite** has no network overhead, so even un-batched inserts are cheap.

Raising `MaxBatchSize` is a non-invasive way to let EF Core / Npgsql batch
more aggressively. There's no library code change required.

---

## How to apply

### `AuditingDbContext` integration model

```csharp
public class AppDbContext : AuditingDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> opts, IAuditUserProvider userProvider, AuditOptions auditOptions)
        : base(opts, userProvider, auditOptions) { }

    public DbSet<Customer> Customers => Set<Customer>();
}

// Program.cs / Startup.cs
services.AddEfCoreAuditing<MyUserProvider>();
services.AddDbContext<AppDbContext>((sp, opts) =>
    opts.UseNpgsql(
        Configuration.GetConnectionString("Default")!,
        npgsql => npgsql.MaxBatchSize(100))
);
```

### Interceptor integration model

```csharp
services.AddEfCoreAuditing<MyUserProvider>();
services.AddDbContext<AppDbContext>((sp, opts) => opts
    .UseNpgsql(
        Configuration.GetConnectionString("Default")!,
        npgsql => npgsql.MaxBatchSize(100))
    .UseAuditing(sp)
);
```

The `MaxBatchSize(100)` call goes on the `UseNpgsql` configuration, not on
`UseAuditing` — the audit interceptor doesn't override batching behavior.

---

## How to verify locally

Run the cross-RDBMS benchmark with a `--filter` targeting just the
PostgreSQL row:

```bash
dotnet run -c Release --project benchmarks/Wolfgang.Audit.EFCore.Benchmarks -- \
  --filter '*ProviderSaveChangesBenchmarks.Insert*' --job short
```

Docker is required (PostgreSQL is provisioned via Testcontainers). The
benchmark project currently does **not** set `MaxBatchSize` — it measures the
out-of-the-box configuration. If you want to measure the tuned configuration
locally, edit
`benchmarks/Wolfgang.Audit.EFCore.Benchmarks/ProviderSaveChangesBenchmarks.cs`
and add the `MaxBatchSize(100)` argument to the `UseNpgsql` call in the
PostgreSQL setup; numbers should drop substantially at batch sizes ≥ 10.

A future PR can add a parameterized variant
(`[Params(false, true)] public bool TunedNpgsqlBatching { get; set; }`) so the
two configurations show side-by-side in the chart.

---

## When tuning isn't enough

For very large bulk operations (thousands of rows per save), even `MaxBatchSize`
maxed out won't fully close the gap. The Postgres-native path for bulk insert
is the `COPY` streaming protocol, exposed through Npgsql's
`NpgsqlConnection.BeginBinaryImport`. A future provider-specific code path in
`AuditCapture.AddAuditEntities` (or an opt-in `AuditOptions.UseBulkInsertWhenAvailable`
flag) could route audit writes through `COPY` once the row count crosses a
threshold. That's a v1.1+ feature; track via the same #26 thread.

---

## References

- Issue: [#26](https://github.com/Chris-Wolfgang/EF-Audit/issues/26)
- Npgsql docs: [Batching](https://www.npgsql.org/efcore/index.html#performance)
- Npgsql `MaxBatchSize`: caps the count of statements per command (≠ `BatchSize`
  in some other ORMs, which means rows per multi-row INSERT)
