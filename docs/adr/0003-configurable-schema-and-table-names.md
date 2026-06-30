# 0003 — Configurable schema and table names

- **Status:** Accepted
- **Date:** 2026-06-28
- **Deciders:** Chris Wolfgang

## Context and problem statement

The audit tables (`AuditHeader`, `AuditDetail`) live in the consumer's database
alongside their own tables. Consumers have existing naming conventions, schema
separation policies (e.g. a dedicated `audit` schema), and sometimes hard
constraints from a DBA. A library that hard-codes `dbo.AuditHeader` cannot live
in those databases without friction.

## Considered options

- **Hard-coded names** — simplest, but collides with consumer conventions and
  forbids schema isolation.
- **Configurable schema + table names via `AuditOptions`** — flexible; the
  values must flow consistently to model building, DDL installation, and runtime
  writes.

## Decision

Expose `Schema`, `HeaderTableName` (default `AuditHeader`), and
`DetailTableName` (default `AuditDetail`) on `AuditOptions`. These values are
read at model-build time, at schema-install/migrate time, and at write time, so
the same configured names are used end to end.

### Rationale

- **Real deployments need it.** Schema isolation and naming conventions are
  table stakes for adoption in an existing database.
- **Single source of truth.** Threading the names through `AuditOptions` (rather
  than separate config in three places) prevents the model, the installer, and
  the runtime writer from disagreeing about where the rows live.
- **Safe defaults.** Out of the box the names are sensible (`AuditHeader` /
  `AuditDetail`, provider-default schema) so the zero-config path still works.

## Consequences

- **Positive:** the library drops into databases with strict naming/schema
  policies; the audit tables can be isolated in their own schema.
- **Negative:** every component that emits a table reference must read the
  configured names — identifiers are quoted per provider to tolerate unusual
  names (see the schema installer's quoting tests).
- **Follow-ups:** the CLI (`audittrail migrate`) accepts `--schema`,
  `--header-table`, and `--detail-table` so out-of-process schema installation
  honours the same configuration as the in-process path.
