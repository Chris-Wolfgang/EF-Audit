# Threat model — Wolfgang.AuditTrail

A [STRIDE](https://en.wikipedia.org/wiki/STRIDE_model) threat-modeling pass over
the public API surface and runtime behaviour of `Wolfgang.AuditTrail.*`.

This library is **not** a network-facing service. It is an in-process EF Core
component that runs with the consumer's database credentials and writes audit
rows into the consumer's database. The relevant attack surface is therefore the
**integrity and completeness of the audit trail** and the **data flowing into
it**, not transport or authentication (which the host owns).

## Assets

| Asset | Why it matters |
|---|---|
| **Audit row integrity** | The audit trail's value is that it accurately reflects what happened. A trail that can be silently altered or made to disagree with the data is worse than none. |
| **Audit completeness** | Every Insert/Update/Delete on a tracked entity should produce audit rows. Silent gaps undermine compliance use. |
| **Captured values** | Old/new column values may include sensitive data (PII, secrets). |
| **The acting-user attribution** | `AuditHeader` records *who* made the change; mis-attribution enables repudiation. |

## Trust boundaries

- The library executes **inside the consumer's process** with the consumer's
  `DbContext` and DB credentials. It does not elevate privilege or open new
  channels.
- Inputs it does not control: entity values (from the consumer's domain), the
  acting user (from the consumer-supplied `IAuditUserProvider`), and the
  configured schema/table names (`AuditOptions`).

## STRIDE analysis

### S — Spoofing (acting-user attribution)

- **Threat:** the user stamped on a header is wrong, enabling one actor to
  appear as another.
- **Mitigation:** attribution is delegated to the consumer's
  `IAuditUserProvider` — the library faithfully records whatever it returns and
  makes no authentication claims. **Accepted risk / consumer responsibility:**
  the provider must be wired to the host's authenticated identity, not
  attacker-controlled input. Documented as a provider contract.

### T — Tampering (audit integrity)

- **Threat 1:** audit rows are written but later silently diverge from the data
  (e.g. data commits, audit does not).
  - **Mitigation:** audit rows are written in the **same transaction** as the
    user's save and wrapped in the execution strategy — atomic commit/rollback
    (see [ADR-0002](adr/0002-same-transaction-atomicity.md)). This is the
    library's core integrity guarantee and is covered by atomicity tests.
- **Threat 2:** an attacker with write access to the database edits/deletes
  audit rows after the fact.
  - **Accepted risk:** out of scope for an in-database trail. Mitigation belongs
    to the consumer's DB hardening — restrict `UPDATE`/`DELETE` grants on the
    audit tables, ship rows to append-only/WORM storage, or use database
    temporal/ledger features. Called out so consumers make a conscious choice.

### R — Repudiation

- **Threat:** an actor denies making a change.
- **Mitigation:** every change produces a header with user + UTC timestamp +
  operation, joined to per-column detail rows. Combined with the tampering
  mitigations above, this supports non-repudiation **to the extent the consumer
  protects the rows** (see Threat T2).

### I — Information disclosure (sensitive captured values)

- **Threat:** old/new values for sensitive columns (passwords, tokens, PII) are
  persisted in plaintext in `AuditDetail`.
  - **Mitigation:** value rendering is pluggable via `IAuditValueSerializer`,
    so consumers can redact/hash/encrypt sensitive columns before they are
    written. **Consumer responsibility:** the library does not guess which
    columns are sensitive. Recommend documenting a redaction policy and
    excluding/transforming sensitive properties.
  - **Follow-up:** consider a first-class `[AuditRedact]`-style opt-out attribute
    in a future version (tracked as an enhancement).

### D — Denial of service (audit-write amplification)

- **Threat:** the per-column schema multiplies row writes; a wide bulk update
  could amplify into a very large audit write and slow/abort the save.
  - **Mitigation:** the cost is bounded by and proportional to the consumer's own
    change set, in the same transaction, so it cannot exceed the work the
    consumer already initiated. Performance characteristics and the planned
    COPY-protocol bulk path are documented
    ([POSTGRES-PERFORMANCE.md](POSTGRES-PERFORMANCE.md), v1.1 bulk-insert issue).
    **Accepted:** no separate rate limiting — the library is not a public entry
    point.

### E — Elevation of privilege

- **Threat:** the library performs operations beyond what the consumer's
  credentials allow, or executes attacker-influenced DDL/SQL.
  - **Mitigation:** the library runs purely with the consumer's existing
    `DbContext` connection — no privilege elevation. Schema/table identifiers
    from `AuditOptions` are **quoted per provider** before use in DDL (schema
    installer quoting tests) to prevent identifier injection via unusual
    configured names. Runtime writes go through EF Core parameterisation, not
    string-concatenated SQL.

## Accepted risks (summary)

1. **Post-write row tampering** in the database is the consumer's DB-hardening
   responsibility (restrict grants / WORM / temporal tables).
2. **Correct user attribution** depends on the consumer wiring
   `IAuditUserProvider` to a trustworthy identity.
3. **Sensitive-value redaction** depends on the consumer supplying a redacting
   `IAuditValueSerializer` for sensitive columns.

## Re-review triggers

Revisit this model when: a new public surface is added that accepts external
input; the value/key serialization contracts change; or out-of-process schema
tooling (the `audittrail` CLI) gains the ability to run against credentials it
did not previously use.
