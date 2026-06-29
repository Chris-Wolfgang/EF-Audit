# `Wolfgang.AuditTrail.EntityFrameworkCore.Identity` — design sketch

## Why this exists

`AuditingDbContext` is the recommended integration model — one-word change at the class declaration, audit rows ride every `SaveChangesAsync` atomically, retry-safe. But it requires the consumer's `DbContext` to inherit from `DbContext` directly. ASP.NET Core Identity apps almost universally derive from `IdentityDbContext<TUser>` (or a related Identity variant) instead, so they can't also derive from `AuditingDbContext` — C# is single-inheritance.

Today those apps fall back to the `AuditSaveChangesInterceptor` path. That works, but carries documented caveats:

- Doesn't preserve `SaveChanges(acceptAllChangesOnSuccess: false)` (throws at runtime).
- Has a narrow cancellation-cleanup gap on EF Core 7+ (the `SaveChangesCanceledAsync` callback the interceptor doesn't implement because that method isn't on the EF Core 6 interface surface).
- Requires consumers to remember `modelBuilder.ApplyAuditing(options)` in `OnModelCreating`.

The Identity sub-package eliminates all three: it ships pre-derived flavors of every Identity base so Identity users get the same one-word swap that vanilla `DbContext` users get.

This pattern matches what [Audit.NET](https://github.com/thepirat000/Audit.NET) ships (`AuditIdentityDbContext<TUser>`, etc.) — pre-derived bases are a real, well-trodden solution to single-inheritance composition with Identity.

## What ships

New project: **`src/Wolfgang.AuditTrail.EntityFrameworkCore.Identity/`** — a thin add-on package that depends on `Wolfgang.AuditTrail.EntityFrameworkCore` + `Microsoft.AspNetCore.Identity.EntityFrameworkCore`. Exposes pre-derived `AuditingIdentity*DbContext` classes — one per Identity base — that re-implement the same audit-aware `SaveChanges`/`SaveChangesAsync` overrides as `AuditingDbContext`.

| Identity base | Audit-aware variant |
|---|---|
| `IdentityDbContext` | `AuditingIdentityDbContext` |
| `IdentityDbContext<TUser>` | `AuditingIdentityDbContext<TUser>` |
| `IdentityDbContext<TUser, TRole, TKey>` | `AuditingIdentityDbContext<TUser, TRole, TKey>` |
| `IdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>` | `AuditingIdentityDbContext<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken>` |
| `IdentityUserContext<TUser>` | `AuditingIdentityUserContext<TUser>` |
| `IdentityUserContext<TUser, TKey>` | `AuditingIdentityUserContext<TUser, TKey>` |
| `IdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken>` | `AuditingIdentityUserContext<TUser, TKey, TUserClaim, TUserLogin, TUserToken>` |

Seven variants total. Each inherits from its corresponding `IdentityDbContext`/`IdentityUserContext` *and* shares the same audit-pass override logic via a single `internal static class AuditingSaveHelper` (extracted from `AuditingDbContext` to avoid duplication).

## Shared helper

To avoid copy-pasting the override across seven classes, factor the audit-pass logic out of `AuditingDbContext` into an internal helper inside `Wolfgang.AuditTrail.EntityFrameworkCore`:

```csharp
internal static class AuditingSaveHelper
{
    public static int SaveChanges(
        DbContext context,
        bool acceptAllChangesOnSuccess,
        IAuditUserProvider userProvider,
        AuditOptions options,
        Func<bool, int> baseSaveChanges,
        ref bool isAuditingSaveFlag) { /* ... */ }

    public static Task<int> SaveChangesAsync(
        DbContext context,
        bool acceptAllChangesOnSuccess,
        IAuditUserProvider userProvider,
        AuditOptions options,
        Func<bool, CancellationToken, Task<int>> baseSaveChangesAsync,
        Action<bool> setIsAuditingSaveFlag,
        Func<bool> getIsAuditingSaveFlag,
        CancellationToken cancellationToken) { /* ... */ }

    public static void OnModelCreating(ModelBuilder modelBuilder, AuditOptions options)
        => modelBuilder.ApplyAuditing(options);
}
```

Both `AuditingDbContext` and every `AuditingIdentity*DbContext` delegate to this helper. The behavior stays in lockstep; future fixes (retry semantics, recursion-guard edge cases, etc.) automatically apply to all variants.

## Consumer's diff

**Before** (today, using the interceptor):

```csharp
public class AppDbContext : IdentityDbContext<AppUser>
{
    private readonly AuditOptions _auditOptions;
    public AppDbContext(DbContextOptions options, AuditOptions auditOptions)
        : base(options) => _auditOptions = auditOptions;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyAuditing(_auditOptions);  // required
    }
}

// In Program.cs:
services.AddEfCoreAuditing<MyUserProvider>();
services.AddDbContext<AppDbContext>((sp, opts) => opts
    .UseSqlServer(connStr)
    .UseAuditing(sp));  // wires the interceptor
```

**After** (with the Identity sub-package):

```csharp
public class AppDbContext : AuditingIdentityDbContext<AppUser>
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IAuditUserProvider userProvider,
        AuditOptions auditOptions)
        : base(options, userProvider, auditOptions) { }
}

// In Program.cs:
services.AddEfCoreAuditing<MyUserProvider>();
services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(connStr));
```

No interceptor wiring, no `ApplyAuditing` to remember, no `UseAuditing` call. The audit-pass override lives on the base.

## Tradeoffs (why this isn't day-1)

1. **Maintenance surface.** Seven pre-derived classes is a real package to maintain across EF Core versions. When Microsoft adds a new Identity overload (rare, but happens), this package needs a corresponding new flavor.
2. **Test matrix.** Each variant needs its own integration test (at minimum a smoke test confirming audit rows get written through that base). FsCheck-style contract tests would be appropriate.
3. **Package-discovery cost.** Yet another package on NuGet under the `Wolfgang.AuditTrail.*` umbrella. Users have to know it exists — README docs and the integration matrix table need to point at it.
4. **Identity version matrix.** ASP.NET Core Identity has had API churn across .NET 6/7/8/9/10. The package needs to either pin to one Identity version (limiting consumers) or multi-target.

## Suggested implementation path

1. Land v1 with just `AuditingDbContext` + interceptor (current state).
2. Wait for consumer demand signal — issues / discussions asking for Identity support.
3. Extract `AuditingSaveHelper` from `AuditingDbContext` in a refactor PR (no behavior change). Confirms the helper API works on one consumer before fanning out.
4. Land `Wolfgang.AuditTrail.EntityFrameworkCore.Identity` as a separate sub-package. Use the same csproj conventions as the main library (net6.0/net8.0/net10.0 TFMs, same analyzer stack).
5. Update README integration matrix: "Identity-based context → use `Wolfgang.AuditTrail.EntityFrameworkCore.Identity`'s pre-derived variant" replacing the interceptor recommendation for that case.

## Scope NOT for the Identity sub-package

- Multi-tenant bases (Finbuckle.MultiTenant). Same single-inheritance problem but Finbuckle's API moves more often and is a smaller user base. Document the interceptor as the answer there.
- Enterprise / internal base contexts. By definition consumer-specific; no library can pre-derive every shape.
- ABP Framework / Clean Architecture template bases. Out of scope; those project templates can apply the helper pattern themselves if they care.
