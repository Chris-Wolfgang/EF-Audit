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
    /// (scoped). Consumers then call
    /// <see cref="DbContextAuditExtensions.SaveChangesWithAuditAsync"/> on their
    /// <c>DbContext</c> to perform a save that includes audit rows.
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
