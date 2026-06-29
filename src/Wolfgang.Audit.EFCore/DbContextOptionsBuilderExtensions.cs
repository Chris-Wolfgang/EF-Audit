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
    /// <remarks>
    /// <para>
    /// <strong>You must also call <c>modelBuilder.ApplyAuditing(options)</c> from
    /// your <c>DbContext.OnModelCreating</c> override.</strong> Unlike
    /// <see cref="AuditingDbContext"/> (which applies the audit entity mappings
    /// itself in its <c>OnModelCreating</c>), the interceptor cannot reach into
    /// the consumer's model — the EF Core model is sealed by the time the
    /// interceptor first runs. Without the <c>ApplyAuditing</c> call, the first
    /// save with audit data will throw an "entity type was not found in the
    /// model" exception when the interceptor tries to <c>Add</c> the
    /// <see cref="Entities.AuditHeader"/> / <see cref="Entities.AuditDetail"/>
    /// entities.
    /// </para>
    /// <para>Minimal correct wiring:</para>
    /// <code>
    /// services.AddEfCoreAuditing&lt;MyUserProvider&gt;();
    /// services.AddDbContext&lt;AppDbContext&gt;((sp, opts) =&gt; opts
    ///     .UseSqlServer(connStr)
    ///     .UseAuditing(sp));
    ///
    /// public class AppDbContext : IdentityDbContext&lt;AppUser&gt;
    /// {
    ///     private readonly AuditOptions _auditOptions;
    ///     public AppDbContext(DbContextOptions options, AuditOptions auditOptions)
    ///         : base(options) =&gt; _auditOptions = auditOptions;
    ///
    ///     protected override void OnModelCreating(ModelBuilder modelBuilder)
    ///     {
    ///         base.OnModelCreating(modelBuilder);
    ///         modelBuilder.ApplyAuditing(_auditOptions);  // &lt;-- required
    ///     }
    /// }
    /// </code>
    /// </remarks>
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
