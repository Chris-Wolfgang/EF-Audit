using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Wolfgang.Audit;



/// <summary>
/// EF Core <see cref="DbContextOptionsBuilder"/> extensions that wire the
/// <see cref="AuditSaveChangesInterceptor"/> into a consumer's <c>DbContext</c>.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Registers the audit save-changes interceptor on this
    /// <see cref="DbContextOptionsBuilder"/>. Use this for applications whose
    /// <c>DbContext</c> already inherits from a third-party base (e.g.
    /// <c>IdentityDbContext&lt;TUser&gt;</c>) and therefore cannot also inherit
    /// from <see cref="AuditingDbContext"/>.
    /// </summary>
    /// <param name="optionsBuilder">The EF Core options builder.</param>
    /// <param name="serviceProvider">
    /// The DI service provider; must already contain <see cref="AuditOptions"/>
    /// and <see cref="IAuditUserProvider"/> (register them via
    /// <c>services.AddEfCoreAuditing&lt;TUserProvider&gt;()</c>).
    /// </param>
    /// <returns><paramref name="optionsBuilder"/> for chaining.</returns>
    public static DbContextOptionsBuilder UseAuditing
    (
        this DbContextOptionsBuilder optionsBuilder,
        IServiceProvider serviceProvider
    )
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var userProvider = serviceProvider.GetRequiredService<IAuditUserProvider>();
        var options = serviceProvider.GetRequiredService<AuditOptions>();

        return optionsBuilder.AddInterceptors(new AuditSaveChangesInterceptor(userProvider, options));
    }
}
