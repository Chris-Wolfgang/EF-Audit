# Wolfgang.Audit

An EF Core change-tracking library. Calling `context.SaveChangesWithAuditAsync(...)` instead of `SaveChangesAsync` captures every Insert / Update / Delete via `ChangeTracker` and writes a row-by-row audit history — one **header** per changed entity plus per-column **detail** rows — into the **same transaction** as the user's save. Either both commit or both roll back, atomically. Uses EF Core's `IExecutionStrategy` under the hood so transient retries still work.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%208.0%20%7C%2010.0-purple.svg)](https://dotnet.microsoft.com/)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-181717?logo=github)](https://github.com/Chris-Wolfgang/EF-Audit)

---

## 📦 Packages

| Package | Purpose |
|---|---|
| `Wolfgang.Audit.Abstractions` | Shared contracts (interfaces, attributes, entity types). No EF Core dependency. |
| `Wolfgang.Audit.EFCore` | The `SaveChangesWithAuditAsync` extension method, default serializers, and DI helpers. Depends on EF Core 6+ Relational. |
| `Wolfgang.Audit.TestKit.Xunit` | xunit contract-test bases (FsCheck-powered) for validating custom `IAuditValueSerializer` implementations. |

All three are published to NuGet.org under the **`Wolfgang.Audit.*`** prefix.

```bash
dotnet add package Wolfgang.Audit.EFCore
```

---

## 🚀 Quick start

```csharp
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit;
using Wolfgang.Audit.Serializers;

// 1. Configure options + serializers.
var auditOptions = new AuditOptions
{
    Schema = null,                                   // null = provider default
    ValueSerializer = new StringAuditValueSerializer(),
    EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
};
IAuditUserProvider userProvider = new MyUserProvider();

// 2. Tell EF Core about the audit entity types in OnModelCreating.
public class AppDbContext : DbContext
{
    private readonly AuditOptions _auditOptions;
    public AppDbContext(DbContextOptions<AppDbContext> options, AuditOptions auditOptions)
        : base(options) => _auditOptions = auditOptions;

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyAuditing(_auditOptions);
}

// 3. Call SaveChangesWithAuditAsync instead of SaveChangesAsync.
await using var ctx = new AppDbContext(contextOptions, auditOptions);
ctx.Customers.Add(new Customer { Name = "Alice" });
await ctx.SaveChangesWithAuditAsync(userProvider, auditOptions);
// AuditHeader + AuditDetail rows for the insert are now in the same transaction.
```

### Why an extension method instead of an interceptor?

EF Core's implicit transaction commits *before* `SavedChangesAsync` fires (see [efcore#37131](https://github.com/dotnet/efcore/issues/37131)), so an interceptor cannot guarantee that audit rows commit in the same transaction as the user's data. Owning the transaction at the call site — what `SaveChangesWithAuditAsync` does via `IExecutionStrategy.ExecuteInTransactionAsync` — is the canonical pattern the EF Core team recommends. The cost is one extra method name to learn; the win is real atomicity, including for database-generated primary keys.

Two end-to-end samples ship in [`examples/`](./examples):

- [Console](./examples/Wolfgang.Audit.EFCore.Example.Console) — minimal SQLite demo that prints the audit history.
- [Web API](./examples/Wolfgang.Audit.EFCore.Example.WebApi) — ASP.NET Core minimal API showing the on-behalf-of pattern (service account as `UserId`, authenticated human as `OnBehalfOfUserId`).

---

## ✨ Features

| Feature | What it does |
|---|---|
| **Atomic audit writes** | Audit rows are persisted via the same `DbContext` as user data, so they share the user's transaction. Rollback = no orphaned audit history. |
| **Two-phase capture** | Snapshots in `SavingChangesAsync`, persists in `SavedChangesAsync` — so database-generated identity keys are resolved by the time the audit header is written. |
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

[`benchmarks/Wolfgang.Audit.EFCore.Benchmarks`](./benchmarks/Wolfgang.Audit.EFCore.Benchmarks) ships BenchmarkDotNet comparisons of plain `SaveChanges` vs `SaveChangesWithAuditAsync` across Insert, full Lifecycle (I→U→D), and MixedStates workloads. `MemoryDiagnoser` is enabled so allocation deltas are visible.

The [`benchmarks.yaml`](./.github/workflows/benchmarks.yaml) workflow runs them on every PR, fails the build if time or allocations regress beyond 2× the previous main-branch baseline, and auto-publishes the chart to [`gh-pages/dev/bench`](https://Chris-Wolfgang.github.io/EF-Audit/dev/bench/) on pushes to `main`.

```bash
dotnet run -c Release --project benchmarks/Wolfgang.Audit.EFCore.Benchmarks -- --filter '*'
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
