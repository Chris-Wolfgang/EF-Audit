# Wolfgang.Audit

An EF Core change-tracking library. Derive your `DbContext` from `AuditingDbContext` (or attach the auto-transaction interceptor) and every existing `context.SaveChangesAsync()` call site captures every Insert / Update / Delete via `ChangeTracker`, writing a row-by-row audit history — one **header** per changed entity plus per-column **detail** rows — into the **same transaction** as the user's save. Either both commit or both roll back, atomically. Uses EF Core's `IExecutionStrategy` under the hood so transient retries still work. **No call-site changes required.**

The header/detail-per-column schema is the same shape that [Z.EntityFramework.Plus.Audit](https://entityframework-plus.net/ef-core-audit) and the [ABP Framework auditing](https://abp.io/docs/latest/framework/infrastructure/audit-logging) use — chosen for queryability ("every change to `Customer.Email` ever") over the more common JSON-blob-per-change shape (Audit.NET, EntityFrameworkCore.AutoHistory).

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-181717?logo=github)](https://github.com/Chris-Wolfgang/EF-Audit)

---

## 📦 Packages

| Package | Purpose |
|---|---|
| `Wolfgang.Audit.Abstractions` | Shared contracts (interfaces, attributes, entity types). No EF Core dependency. |
| `Wolfgang.Audit.EFCore` | `AuditingDbContext` base class, auto-transaction `AuditSaveChangesInterceptor`, default serializers, DI helpers. Depends on EF Core 6+ Relational. |
| `Wolfgang.Audit.TestKit.Xunit` | xunit contract-test bases (FsCheck-powered) for validating custom `IAuditValueSerializer` implementations. |

All three are published to NuGet.org under the **`Wolfgang.Audit.*`** prefix.

```bash
dotnet add package Wolfgang.Audit.EFCore
```

---

## 🚀 Quick start

Two integration models — pick whichever fits your codebase. **In both cases your `SaveChangesAsync` call sites stay exactly as they are today.**

### Model 1 — `AuditingDbContext` base class (recommended)

For new contexts or any context whose only base is `DbContext`. One word changes; nothing else does.

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolfgang.Audit;

// 1. Register audit services + your DbContext as normal.
services.AddEfCoreAuditing<MyUserProvider>(opts =>
{
    opts.Schema = null; // null = provider default (dbo / public / none)
});

services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connStr));

// 2. Derive your DbContext from AuditingDbContext (the only line that changes).
public class AppDbContext : AuditingDbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IAuditUserProvider userProvider,
        AuditOptions auditOptions)
        : base(options, userProvider, auditOptions) { }

    public DbSet<Customer> Customers => Set<Customer>();
}

// 3. Save as usual. Audit rows are written in the same transaction.
ctx.Customers.Add(new Customer { Name = "Alice" });
await ctx.SaveChangesAsync();
```

### Model 2 — auto-transaction interceptor

For contexts already inheriting from a third-party base — `IdentityDbContext<TUser>`, multi-tenant bases, internal enterprise bases — that can't switch parents.

```csharp
services.AddEfCoreAuditing<MyUserProvider>();

services.AddDbContext<AppDbContext>((sp, opts) => opts
    .UseSqlServer(connStr)
    .UseAuditing(sp));        // <-- the only new line

// AppDbContext stays whatever it already was.
public class AppDbContext : IdentityDbContext<AppUser>
{
    // ... existing code unchanged
}

// Save call sites are unchanged.
await ctx.SaveChangesAsync();
```

### Which one should I pick?

| Situation | Use |
|---|---|
| New `DbContext` | **Model 1 (`AuditingDbContext`)** |
| Context inherits from `DbContext` only | **Model 1** |
| Context inherits from `IdentityDbContext<TUser>` or another base | **Model 2 (interceptor)** |
| `EnableRetryOnFailure` enabled on the connection | **Model 1** (Model 2 throws at runtime — see [Retry caveat](#retry-strategy-caveat) below) |

### Retry-strategy caveat

EF Core's retrying execution strategies (e.g. `SqlServerRetryingExecutionStrategy` from `EnableRetryOnFailure`) refuse user-initiated transactions opened outside `strategy.ExecuteAsync(...)`. Model 1 handles this correctly because the base class owns the strategy wrap; Model 2's interceptor cannot, and throws a clear `InvalidOperationException` at the first save pointing you back at Model 1. If you're on Azure SQL or any other connection with retry enabled, derive from `AuditingDbContext`.

Two end-to-end samples ship in [`examples/`](./examples):

- [Console](./examples/Wolfgang.Audit.EFCore.Example.Console) — minimal SQLite demo that prints the audit history.
- [Web API](./examples/Wolfgang.Audit.EFCore.Example.WebApi) — ASP.NET Core minimal API showing the on-behalf-of pattern (service account as `UserId`, authenticated human as `OnBehalfOfUserId`).

---

## ✨ Features

| Feature | What it does |
|---|---|
| **Atomic audit writes** | Audit rows are persisted via the same `DbContext` as user data, so they share the user's transaction. Rollback = no orphaned audit history. |
| **Two-phase capture** | Snapshots the `ChangeTracker` before the user save, then materializes audit rows from that snapshot after the user save completes — so database-generated identity keys are resolved by the time the audit header is written. The `AuditingDbContext` base class drives both passes inside `IExecutionStrategy.ExecuteInTransactionAsync`; the optional interceptor drives them across `SavingChanges`/`SavedChanges`. |
| **Pluggable value serializer** | `IAuditValueSerializer` owns the detail-row column shape. v1 ships `StringAuditValueSerializer`; future binary / hybrid variants slot in without changing the contract. |
| **Pluggable entity-key serializer** | `IAuditEntityKeySerializer` controls how composite keys render into the `EntityKey` column. v1 default is pipe-delimited. |
| **Opt-out via `[NotAudited]`** | Apply at the class level to exclude an entity; apply at the property level to exclude a single column. Mirrors EF Core's `[NotMapped]`. |
| **Configurable schema + table names** | `AuditOptions.Schema`, `.HeaderTableName`, `.DetailTableName` — multiple apps can coexist in one database under different schemas. |
| **Microsecond timestamps** | `AuditedAtUtc` is `DateTime` UTC with `HasPrecision(6)` so resolution is identical across SQL Server, PostgreSQL, MySQL, and SQLite. |
| **On-behalf-of user identity** | `AuditHeader.UserId` records the service account; `OnBehalfOfUserId` records the authenticated human (web scenarios). |
| **Configurable delete capture** | `CaptureDeletedValues = false` (default) writes header only on Delete; `true` writes pre-delete column values for forensic audits. |
| **Same TransactionId per save** | All header rows produced by a single `SaveChanges` share a `Guid` so consumers can group "what changed together". |

---

## 🎯 Target frameworks

| Package | TFMs |
|---|---|
| `Wolfgang.Audit.Abstractions` | `netstandard2.0`, `net8.0`, `net10.0` |
| `Wolfgang.Audit.EFCore` | `net6.0`, `net8.0`, `net10.0` |
| `Wolfgang.Audit.TestKit.Xunit` | `netstandard2.0`, `net8.0`, `net10.0` |

EF Core 6, 7, 8, 9, and 10 are all supported (the library targets the LTS net6.0 / net8.0 / net10.0; an EF Core 7 consumer running on net6.0+ or net7.0+ resolves the appropriate TFM automatically).

---

## 🧪 Testing

Three test projects + the shipped TestKit contract base:

- **`tests/Wolfgang.Audit.EFCore.Tests.Unit`** — SQLite in-memory, fast, runs on every PR.
- **`tests/Wolfgang.Audit.EFCore.Tests.Integration`** — Testcontainers against SQL Server 2022, PostgreSQL 16, MySQL 8.0. Opt-in via `RunIntegrationTests=true`; the dedicated [`integration.yaml`](./.github/workflows/integration.yaml) workflow runs them on Linux with Docker pre-installed.
- **`tests/Wolfgang.Audit.EFCore.Tests.Smoke`** — end-to-end order-lifecycle scenario validating history reconstruction.
- **`src/Wolfgang.Audit.TestKit.Xunit`** — shipped NuGet package containing `AuditValueSerializerContractTests<TSut>`. Inherit + provide `CreateSut()` and you get FsCheck-powered property tests plus boundary-value theories for every supported CLR type.

```bash
# Unit + smoke tests
dotnet test

# Integration tests (Docker required)
RunIntegrationTests=true dotnet test tests/Wolfgang.Audit.EFCore.Tests.Integration
```

---

## 📈 Benchmarks

[`benchmarks/Wolfgang.Audit.EFCore.Benchmarks`](./benchmarks/Wolfgang.Audit.EFCore.Benchmarks) ships BenchmarkDotNet comparisons of plain `SaveChangesAsync` on an unaudited `DbContext` vs `SaveChangesAsync` on an `AuditingDbContext` across Insert, full Lifecycle (I→U→D), and MixedStates workloads. `MemoryDiagnoser` is enabled so allocation deltas are visible. A separate `ProviderSaveChangesBenchmarks` class extends the comparison to SQL Server and PostgreSQL via Testcontainers (run locally with `--filter '*ProviderSaveChangesBenchmarks*'`).

### Measured audit-write cost (insert workload, `--job short`)

Numbers below are from a local run on a single 14700HX laptop; absolute values vary, but the **ratio** column is the load-bearing data — that's the cost of adding auditing to an existing save.

| Provider | Batch | Without audit | With audit | **Time ratio** | **Alloc ratio** |
|---|---:|---:|---:|---:|---:|
| SQLite | 1 | 353 µs | 729 µs | 2.07× | 1.91× |
| SQLite | 10 | 630 µs | 3.3 ms | 5.24× | 4.73× |
| SQLite | 50 | 2.2 ms | 15.7 ms | 7.19× | 6.54× |
| SQL Server | 1 | 9.9 ms | 11.7 ms | 1.18× | 1.42× |
| SQL Server | 10 | 8.5 ms | 58.9 ms | 6.90× | 3.19× |
| SQL Server | 50 | 56.3 ms | 295.4 ms | 5.25× | 5.59× |
| PostgreSQL | 1 | 1.8 ms | 4.5 ms | 2.46× | 1.94× |
| PostgreSQL | 10 | 3.9 ms | 53.6 ms | **14.0×** | 4.80× |
| PostgreSQL | 50 | 4.1 ms | 110 ms | **26.7×** | 6.89× |

> ⚠️ **PostgreSQL audit cost is disproportionately high at larger batch sizes.** The fix is consumer-side: pass `MaxBatchSize(100)` to your `UseNpgsql(...)` call. See [`docs/POSTGRES-PERFORMANCE.md`](./docs/POSTGRES-PERFORMANCE.md) for the recipe, the mechanism (Npgsql batches INSERTs less aggressively than SqlClient out-of-the-box), and benchmark numbers. Tracked: [#26](https://github.com/Chris-Wolfgang/EF-Audit/issues/26).

### CI gating

The [`benchmarks.yaml`](./.github/workflows/benchmarks.yaml) workflow runs the SQLite-only suite on every PR, fails the build if time or allocations regress beyond 2× the previous main-branch baseline, and auto-publishes the chart to [`gh-pages/dev/bench`](https://Chris-Wolfgang.github.io/EF-Audit/dev/bench/) on pushes to `main`. The multi-provider suite is opt-in (Docker required).

```bash
# SQLite-only fast suite (no Docker required)
dotnet run -c Release --project benchmarks/Wolfgang.Audit.EFCore.Benchmarks -- \
  --filter '*Wolfgang.Audit.Benchmarks.SaveChangesBenchmarks*'

# Cross-RDBMS suite (Docker required for SQL Server + PostgreSQL)
dotnet run -c Release --project benchmarks/Wolfgang.Audit.EFCore.Benchmarks -- \
  --filter '*ProviderSaveChangesBenchmarks*'
```

---

## 🔍 Code quality

Strict analyzer stack — see the template-inherited [Directory.Build.props](./Directory.Build.props):

- **Microsoft.CodeAnalysis.NetAnalyzers** (built-in)
- **Roslynator.Analyzers**, **AsyncFixer**, **Microsoft.VisualStudio.Threading.Analyzers**
- **Microsoft.CodeAnalysis.BannedApiAnalyzers** + repo-wide [`BannedSymbols.txt`](./BannedSymbols.txt) enforcing async-first I/O
- **Meziantou.Analyzer**, **SonarAnalyzer.CSharp**

Warnings are errors in Release builds.

---

## 🛠️ Building from source

```bash
git clone https://github.com/Chris-Wolfgang/EF-Audit.git
cd EF-Audit
dotnet build EF-Audit.slnx -c Release
dotnet test EF-Audit.slnx -c Release
```

---

## 📚 Documentation

- **API reference:** <https://Chris-Wolfgang.github.io/EF-Audit/>
- **Benchmark chart:** <https://Chris-Wolfgang.github.io/EF-Audit/dev/bench/>
- **Contributing guide:** [CONTRIBUTING.md](CONTRIBUTING.md)

---

## 🤝 Contributing

Pull requests welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for build/test/PR guidelines and the analyzer configuration details.

---

## 📄 License

[MIT](LICENSE).
