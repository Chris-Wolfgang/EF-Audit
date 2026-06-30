# 0002 — Same-transaction atomicity via `IExecutionStrategy`

- **Status:** Accepted
- **Date:** 2026-06-28
- **Deciders:** Chris Wolfgang

## Context and problem statement

Audit rows must be trustworthy: if the user's save commits, its audit history
must commit; if the user's save rolls back, the audit history must roll back
too. An audit trail that can drift out of sync with the data it describes is
worse than none — it lies.

Writing audit rows in a *separate* transaction (or after the user's
`SaveChanges` returns) opens a window where the data commits but the audit write
fails, or vice versa. Meanwhile EF Core consumers frequently enable a
**retrying execution strategy** (e.g. `EnableRetryOnFailure`) for transient
fault handling, and a retrying strategy forbids user-initiated transactions
unless the whole operation is wrapped in the strategy's own
`ExecuteAsync` envelope.

## Considered options

- **Write audit rows after `SaveChanges` in a second transaction** — simple, but
  not atomic, and breaks under retrying execution strategies.
- **Write audit rows in the same transaction, wrapped in the consumer's
  `IExecutionStrategy`** — atomic and retry-safe, at the cost of owning the
  transaction lifetime.

## Decision

Capture and persist audit rows inside the **same transaction** as the user's
save, and wrap the whole unit in the context's
`Database.CreateExecutionStrategy()` so transient-retry strategies still
function. Either both the data and its audit history commit, or both roll back.

### Rationale

- **Atomicity is the core guarantee** the library sells. It must hold under
  failure, not just on the happy path.
- **Retry compatibility is non-negotiable** for production EF Core usage;
  silently breaking `EnableRetryOnFailure` would be an unacceptable regression
  at the consumer's call site.
- **No call-site changes.** Consumers keep calling `SaveChangesAsync()`; the
  transaction + strategy wrapping happens inside `AuditingDbContext` /
  the interceptor.

## Consequences

- **Positive:** the audit trail can never disagree with the committed data;
  works with retrying execution strategies; transparent to existing call sites.
- **Negative:** the library owns transaction creation in the inline path, which
  must coexist with any ambient transaction the consumer already opened (handled
  by detecting an existing `CurrentTransaction` and enlisting rather than
  nesting).
- **Follow-ups:** the interceptor-based model (auto-transaction) and the
  `AuditingDbContext` base-class model are documented side-by-side in the README
  so consumers can pick the integration style that fits their context ownership.
