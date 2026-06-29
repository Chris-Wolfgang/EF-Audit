# AdventureWorks demo

End-to-end demo of `Wolfgang.Audit.EFCore` against a realistic schema. Spins up SQL Server 2022 via Testcontainers, restores the canonical [AdventureWorks2022 .bak](https://github.com/Microsoft/sql-server-samples/releases/tag/adventureworks), then exercises a few realistic operations:

1. **Update** — rename an employee (`Person.LastName`).
2. **Update** — change their email (`EmailAddress.EmailAddress`).
3. **Insert + Delete** — onboard then immediately offboard a contractor.

The demo prints the resulting `AuditHeader` + `AuditDetail` rows so you can see exactly what the library captures against production-style data.

## Prerequisites

- Docker Desktop running (Testcontainers needs the daemon).
- ~2 GB free disk for the SQL Server container + AdventureWorks data.

## Running

```bash
dotnet run --project examples/Wolfgang.Audit.EFCore.Example.AdventureWorks
```

First run takes ~60–90 sec because it pulls the SQL Server image and restores the `.bak`. Subsequent runs reuse the cached image and run in ~15 sec.

## What the output looks like

```
📦 Starting SQL Server container + restoring AdventureWorks2022...

✏️  Renaming an employee...
  Before: Ken J Sánchez (type=EM)
  After:  Ken J Sánchez-Smith

📧  Updating their email...
  Before: ken0@adventure-works.com
  After:  ken.sanchez-smith@adventure-works.com

👤  Onboarding then immediately offboarding a contractor...
  Onboarded BusinessEntityID=99999
  Offboarded BusinessEntityID=99999

📜 Audit history for affected rows:

  [2026-05-18 02:14:31Z] UPDATE on Person key=1 by hr-admin@adventure-works.com
      LastName = Sánchez-Smith  (String)

  [2026-05-18 02:14:31Z] UPDATE on EmailAddress key=1|1 by hr-admin@adventure-works.com
      EmailAddress = ken.sanchez-smith@adventure-works.com  (String)

  [2026-05-18 02:14:31Z] INSERT on Person key=99999 by hr-admin@adventure-works.com
      FirstName = Temp  (String)
      LastName = Contractor  (String)
      PersonType = GC  (String)

  [2026-05-18 02:14:31Z] DELETE on Person key=99999 by hr-admin@adventure-works.com
      (no detail rows — CaptureDeletedValues=false)

✅  Done — 4 audit rows captured atomically with the user data.
```

## What this demonstrates

1. **Composite keys** — `EmailAddress` has `(BusinessEntityID, EmailAddressID)`; the audit log captures it as `"1|1"` via `PipeDelimitedEntityKeySerializer`.
2. **Custom audit schema** — the `Audit` schema lives alongside AdventureWorks's own `Person`, `Sales`, etc. schemas. Configurable via `AuditOptions.Schema`.
3. **Single-transaction atomicity** — each save's data change + audit rows commit together. Roll back the consumer's transaction and the audit rows roll back too.
4. **Real-world non-ASCII data** — `Sánchez` round-trips correctly through the `StringAuditValueSerializer` (UTF-8 / `nvarchar(max)`).
5. **Delete behavior** — the default `CaptureDeletedValues = false` emits the delete header but no detail rows. Set `auditOptions.CaptureDeletedValues = true` in `Program.cs` to capture the pre-delete column values for forensic audits.

## What this does NOT demonstrate (intentionally)

- The auto-transaction interceptor model (the `AdventureWorksContext` derives from `AuditingDbContext` for simplicity). For an interceptor example see [the WebApi example](../Wolfgang.Audit.EFCore.Example.WebApi).
- `[NotAudited]` opt-out — the [Console example](../Wolfgang.Audit.EFCore.Example.Console) covers that.
- The `on-behalf-of` pattern — the [WebApi example](../Wolfgang.Audit.EFCore.Example.WebApi) covers that.
