# Console example

Smallest possible end-to-end demo. Creates a SQLite database, inserts and updates a `Product`, then prints the resulting audit history to the console.

```
dotnet run --project examples/Wolfgang.Audit.EFCore.Example.Console
```

Expected output:

```
Audit history:
  [2026-05-12 ...] Insert ...Product key=1 by alice@example.com
      Name = Widget (String)
      Price = 9.99 (Decimal)
  [2026-05-12 ...] Update ...Product key=1 by alice@example.com
      Price = 12.49 (Decimal)
```
