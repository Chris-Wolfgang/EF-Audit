# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

_(empty — open this section as work for v0.2.0 begins.)_

## [0.1.0] — 2026-06-28

First tagged release. The `Wolfgang.Audit.*` package family is published to
NuGet.org. Multi-targets `netstandard2.0`, `net6.0`, `net8.0`, `net10.0` for
source projects; tests cover net462 → net10.0 inclusive.

### Added

- `Wolfgang.Audit.Abstractions` — shared contracts (`IAuditUserProvider`,
  `IAuditValueSerializer`, `IAuditEntityKeySerializer`, `AuditOptions`,
  `[NotAudited]`, `AuditHeader` / `AuditDetail` entities).
- `Wolfgang.Audit.EFCore` — two integration models:
  - **`AuditingDbContext`** base class (recommended): derive your context from it
    and call `SaveChangesAsync` as usual; audit rows are written atomically in the
    same transaction via `IExecutionStrategy.ExecuteInTransactionAsync` (composes
    correctly with `EnableRetryOnFailure`).
  - **`AuditSaveChangesInterceptor`** + `UseAuditing(serviceProvider)`: for
    contexts already inheriting from a third-party base (`IdentityDbContext<TUser>`,
    multi-tenant bases, etc.). Routes capture through `SavingChanges`/`SavedChanges`.
- `StringAuditValueSerializer` — v1 default value serializer (single
  `ValueText nvarchar(max)`).
- `PipeDelimitedEntityKeySerializer` — v1 default composite-key serializer.
- `Wolfgang.Audit.TestKit.Xunit` — `AuditValueSerializerContractTests<T>` base for
  validating custom `IAuditValueSerializer` implementations.
- `Wolfgang.Audit.EFCore.Schema.AuditSchemaMigrator` — provider-agnostic
  schema installer using EF Core's own `IMigrationsModelDiffer` +
  `IMigrationsSqlGenerator`. Supports SQL Server, PostgreSQL, MySQL, and SQLite
  from one codebase. Idempotent + transactional + version-stamped via
  `__AuditSchemaVersion`.
- `MigrateAuditSchemaAsync()` extension on `AuditingDbContext` for consumers
  who want to install the schema from application code without invoking the CLI.
- `Wolfgang.Audit.Cli` — `audit` command-line tool. Provider auto-detected from
  the connection string; `--dry-run` prints the SQL without applying.
- Benchmarks: `Wolfgang.Audit.EFCore.Benchmarks` (BenchmarkDotNet) covering
  Insert / Lifecycle / MixedStates workloads with SQLite, plus cross-RDBMS via
  Testcontainers (`ProviderSaveChangesBenchmarks`). Charts auto-publish to
  [gh-pages/dev/bench](https://Chris-Wolfgang.github.io/EF-Audit/dev/bench/).
- Two end-to-end example apps under `examples/` (Console, ASP.NET Core WebApi).
- Documentation:
  - `README.md` with quick-start, two-model integration matrix, retry-strategy
    caveat, benchmark numbers
  - `docs/POSTGRES-PERFORMANCE.md` — PostgreSQL `MaxBatchSize` tuning recipe
  - `docs/IDENTITY-SUBPACKAGE-DESIGN.md` — roadmap for the future
    `AuditingIdentity*DbContext` sub-package

### Notes for the first tagged release

- MySQL is **not** wired in the CLI as of this snapshot: Pomelo
  `EntityFrameworkCore.MySql` 9.0.0 caps at EF Core 9.x while the CLI targets
  EF Core 10. The CLI's `migrate` subcommand throws `NotSupportedException`
  with a clear message when invoked with `--provider mysql`. Re-enable when
  Pomelo ships an EF Core 10 release.
- `Wolfgang.Audit.EFCore` targets `net6.0;net8.0;net10.0`. The schema
  migrator (`AuditSchemaMigrator`, `MigrateAuditSchemaAsync`) is gated on
  `NET8_0_OR_GREATER` because EF Core's design-time model API requires the
  newer runtime.

---

[Unreleased]: https://github.com/Chris-Wolfgang/EF-Audit/compare/initial-dev...HEAD
