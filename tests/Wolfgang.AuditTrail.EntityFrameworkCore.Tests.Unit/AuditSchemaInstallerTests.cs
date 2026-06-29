using Microsoft.EntityFrameworkCore;
using Wolfgang.AuditTrail.Entities;
using Wolfgang.AuditTrail.Schema;
using Wolfgang.AuditTrail.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;

public class AuditSchemaInstallerTests
{
    [Fact]
    public async Task CreateTablesAsync_is_idempotent_when_called_twice()
    {
        using var fixture = new AuditFixture(createOnConstruct: false);
        var installer = new AuditSchemaInstaller(fixture.Options);

        await using var context = fixture.CreateContext();

#pragma warning disable CS0618 // Obsolete on purpose — covers the back-compat path until removed
        await installer.CreateTablesAsync(context);
        await installer.CreateTablesAsync(context);
#pragma warning restore CS0618

        // The audited tables exist and the entity set is queryable.
        Assert.Empty(await context.Set<AuditHeader>().ToListAsync());
    }

    // TODO: Options.HeaderTableName / DetailTableName overrides are honored at the
    // EF Core model layer (see ModelBuilderExtensions.ApplyAuditing). Validating
    // this via tests requires either a distinct DbContext type per test or a
    // library-level IModelCacheKeyFactory, because EF Core caches IModel
    // process-wide per DbContext type. Both paths are deferred to a focused PR.
}
