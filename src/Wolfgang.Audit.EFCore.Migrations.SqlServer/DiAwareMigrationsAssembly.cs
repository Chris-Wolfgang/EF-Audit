using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Wolfgang.Audit.Migrations.SqlServer;



/// <summary>
/// Custom <see cref="IMigrationsAssembly"/> that instantiates <see cref="Migration"/>
/// subclasses through the EF Core service provider, so migrations can declare
/// constructor dependencies (in our case, <see cref="AuditOptions"/>) instead of
/// being limited to the default parameterless constructor EF Core expects.
/// This is the linchpin of Approach B — without it, the migration .cs files
/// would need to hardcode schema/table names at build time.
/// </summary>
internal sealed class DiAwareMigrationsAssembly : MigrationsAssembly
{
    private readonly IServiceProvider _serviceProvider;



    public DiAwareMigrationsAssembly
    (
        ICurrentDbContext currentContext,
        IDbContextOptions options,
        IMigrationsIdGenerator idGenerator,
        IDiagnosticsLogger<DbLoggerCategory.Migrations> logger
    )
        : base(currentContext, options, idGenerator, logger)
    {
        _serviceProvider = ((IInfrastructure<IServiceProvider>)currentContext.Context).Instance;
    }



    public override Migration CreateMigration(TypeInfo migrationClass, string activeProvider)
    {
        ArgumentNullException.ThrowIfNull(migrationClass);
        ArgumentNullException.ThrowIfNull(activeProvider);

        // Resolve via the EF Core service provider — picks up AuditOptions and any
        // future migration constructor dependencies registered alongside it.
        var migration = (Migration)ActivatorUtilities.CreateInstance(_serviceProvider, migrationClass.AsType());
        migration.ActiveProvider = activeProvider;
        return migration;
    }
}
