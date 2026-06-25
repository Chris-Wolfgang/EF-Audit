#if NET8_0_OR_GREATER
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Wolfgang.Audit.Schema;



/// <summary>
/// EF Core caches the materialised model per <see cref="DbContext"/> type. Without
/// overriding the cache key, two <see cref="AuditMigrationsDbContext"/> instances
/// configured with different <see cref="AuditOptions.Schema"/> /
/// <see cref="AuditOptions.HeaderTableName"/> / <see cref="AuditOptions.DetailTableName"/>
/// would share the first instance's model and silently route DDL + queries to
/// the wrong tables. This factory folds those three options into the key so
/// each distinct configuration gets its own cached model.
/// </summary>
internal sealed class AuditMigrationsModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime)
    {
        ArgumentNullException.ThrowIfNull(context);
        var options = ((AuditMigrationsDbContext)context).Options;
        return new AuditMigrationsModelCacheKey(
            ContextType:     context.GetType(),
            DesignTime:      designTime,
            Schema:          options.Schema,
            HeaderTableName: options.HeaderTableName,
            DetailTableName: options.DetailTableName);
    }
}



#endif
