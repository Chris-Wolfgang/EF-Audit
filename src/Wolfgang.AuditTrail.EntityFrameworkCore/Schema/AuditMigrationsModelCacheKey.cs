#if NET8_0_OR_GREATER
namespace Wolfgang.AuditTrail.Schema;



/// <summary>
/// Cache key paired with <see cref="AuditMigrationsModelCacheKeyFactory"/>. Two
/// <see cref="AuditMigrationsDbContext"/> instances built with different audit
/// option overrides hash to distinct keys so each gets its own EF Core model.
/// </summary>
internal sealed record AuditMigrationsModelCacheKey
(
    Type ContextType,
    bool DesignTime,
    string? Schema,
    string HeaderTableName,
    string DetailTableName
);
#endif
