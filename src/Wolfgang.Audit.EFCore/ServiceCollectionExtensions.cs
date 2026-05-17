using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit;

/// <summary>
/// Dependency-injection helpers for registering the audit options and user provider.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="AuditOptions"/> (as a singleton) and
    /// <typeparamref name="TUserProvider"/> as <see cref="IAuditUserProvider"/>
    /// (scoped). Pick one of the two integration models:
    /// <list type="bullet">
    /// <item>Derive your <c>DbContext</c> from <see cref="AuditingDbContext"/>
    /// (recommended for greenfield contexts), or</item>
    /// <item>Call <see cref="DbContextOptionsBuilderExtensions.UseAuditing"/> on
    /// the <c>DbContextOptionsBuilder</c> when registering your context (for
    /// contexts already inheriting from a third-party base such as
    /// <c>IdentityDbContext&lt;TUser&gt;</c>).</item>
    /// </list>
    /// Either way, consumers continue to call plain
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync(CancellationToken)"/>
    /// at every call site — audit rows are written atomically in the same
    /// transaction.
    /// </summary>
    public static IServiceCollection AddEfCoreAuditing<TUserProvider>
    (
        this IServiceCollection services,
        Action<AuditOptions>? configure = null
    )
        where TUserProvider : class, IAuditUserProvider
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new AuditOptions
        {
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };

        configure?.Invoke(options);
        options.ValueSerializer ??= new StringAuditValueSerializer();
        options.EntityKeySerializer ??= new PipeDelimitedEntityKeySerializer();

        services.TryAddSingleton(options);
        services.TryAddScoped<IAuditUserProvider, TUserProvider>();

        return services;
    }
}
