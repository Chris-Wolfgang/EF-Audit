# Wolfgang.Audit.EFCore

EF Core audit / change-tracking interceptor. Captures every Insert / Update / Delete via `ChangeTracker` and writes header / detail rows inside the same transaction as the user's `SaveChanges`.

See the [project README](https://github.com/Chris-Wolfgang/EF-Audit) for full documentation.
