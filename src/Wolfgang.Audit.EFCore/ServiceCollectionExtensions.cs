using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wolfgang.Audit.Serializers;

namespace Wolfgang.Audit;

/// <summary>
/// Dependency-injection helpers for registering the audit interceptor and its
/// supporting services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the audit interceptor, <typeparamref name="TUserProvider"/> as
    /// <see cref="IAuditUserProvider"/>, and default serializers. The caller is
    /// responsible for wiring the interceptor onto each <see cref="DbContext"/> via
    /// <c>DbContextOptionsBuilder.AddInterceptors(...)</c> or by resolving it through DI.
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
        services.TryAddScoped<AuditSaveChangesInterceptor>();

        return services;
    }
}
