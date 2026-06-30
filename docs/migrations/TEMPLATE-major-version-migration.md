# Migrating from vX to vY

> One-paragraph summary: what changed at a high level and roughly how much work
> the upgrade is for a typical consumer.

## At a glance

| | |
|---|---|
| **From** | vX.y.z |
| **To** | vY.0.0 |
| **Estimated effort** | Trivial / Moderate / Significant |
| **Database migration required?** | Yes / No |
| **Runtime behaviour change?** | Yes / No |

## Breaking-change inventory

| # | Change | Kind | Action required |
|---|---|---|---|
| 1 | `OldApi(...)` removed | API removal | Replace with `NewApi(...)` — see below |
| 2 | `AuditOptions.Foo` default changed | Behaviour | Set `Foo` explicitly if you relied on the old default |
| 3 | `AuditDetail.Bar` column added | Schema | Run the new migration / `audittrail migrate` |

## Before / after

### 1. `OldApi` → `NewApi`

```csharp
// Before (vX)
services.AddEfCoreAuditing(o => o.OldApi(...));

// After (vY)
services.AddEfCoreAuditing(o => o.NewApi(...));
```

## Database changes

Describe any schema changes and exactly how to apply them — EF migration,
`audittrail migrate`, or hand-run DDL. Include rollback notes.

## Recommended upgrade order

1. Bump the package version.
2. Fix compile errors using the before/after table above.
3. Apply the database migration.
4. Run your test suite.

## Deprecations (not yet removed)

List anything marked `[Obsolete]` in this release so consumers can migrate ahead
of the *next* major.
