# Architecture Decision Records

This folder records the **non-obvious design decisions** behind
`Wolfgang.AuditTrail.*`, so the reasoning survives the PR that introduced it.

Each record is a short, immutable document: it captures the context, the
decision, and the consequences at a point in time. When a decision is later
reversed, we add a **new** ADR that supersedes the old one rather than editing
history.

Format: [MADR](https://adr.github.io/madr/) (lightweight). Start a new record by
copying [`TEMPLATE.md`](TEMPLATE.md) and giving it the next number.

| ADR | Title | Status |
|---|---|---|
| [0001](0001-header-detail-per-column-schema.md) | Header/detail-per-column audit schema | Accepted |
| [0002](0002-same-transaction-atomicity.md) | Same-transaction atomicity via `IExecutionStrategy` | Accepted |
| [0003](0003-configurable-schema-and-table-names.md) | Configurable schema and table names | Accepted |
| [0004](0004-pipe-delimited-default-key-serializer.md) | Pipe-delimited default entity-key serializer | Accepted |
