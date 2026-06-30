# 0001 — Header/detail-per-column audit schema

- **Status:** Accepted
- **Date:** 2026-06-28
- **Deciders:** Chris Wolfgang

## Context and problem statement

Capturing entity changes needs a persistence shape. The two dominant shapes in
the .NET ecosystem are:

1. **JSON-blob-per-change** — one row per changed entity, with the before/after
   state serialized into a single JSON column (Audit.NET,
   EntityFrameworkCore.AutoHistory).
2. **Header/detail-per-column** — one **header** row per changed entity plus one
   **detail** row per changed column (Z.EntityFramework.Plus.Audit, ABP
   Framework auditing).

The library's primary use case is answering historical questions about data:
"every change ever made to `Customer.Email`", "who changed this row and when",
"show the prior value of this one column". Those queries must be efficient and
expressible in SQL without unpacking a blob.

## Considered options

- **JSON blob per change** — compact, schema-flexible, trivial to write.
- **Header/detail per column** — relational, queryable per column, more rows.

## Decision

Use the **header/detail-per-column** schema: an `AuditHeader` row per changed
entity (entity type, key, operation, user, UTC timestamp) and an `AuditDetail`
row per changed column (column name, old value, new value), joined by header id.

### Rationale

- **Queryability is the product.** "Every change to `Customer.Email`" is a
  `WHERE ColumnName = 'Email'` against an indexed column — not a full-table JSON
  scan + client-side filter.
- **Indexable.** Per-column rows let the database index `(EntityType, ColumnName)`
  and similar, which a JSON blob cannot serve without computed columns or
  provider-specific JSON indexing.
- **Ecosystem familiarity.** Matches the shape consumers already know from
  EF Plus / ABP, lowering the learning curve.

## Consequences

- **Positive:** column-level history queries are first-class and index-friendly;
  the schema is portable across SQL Server / PostgreSQL / MySQL / SQLite without
  relying on JSON column support.
- **Negative:** more rows written per save (one per changed column) than the
  blob approach; wide updates produce wide detail fan-out. The PostgreSQL
  performance note ([docs/POSTGRES-PERFORMANCE.md](../POSTGRES-PERFORMANCE.md))
  and the v1.1 COPY-protocol bulk-insert issue track mitigations.
- **Follow-ups:** value serialization is pluggable (`IAuditValueSerializer`) so
  consumers who want JSON-per-detail can opt into it per column without changing
  the relational shape.
