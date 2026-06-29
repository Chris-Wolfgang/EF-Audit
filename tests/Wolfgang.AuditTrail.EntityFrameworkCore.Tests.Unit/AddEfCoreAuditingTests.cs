using Microsoft.Extensions.DependencyInjection;
using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Covers <see cref="ServiceCollectionExtensions.AddEfCoreAuditing{TUserProvider}(IServiceCollection, Action{AuditOptions}?)"/>.
/// </summary>
public class AddEfCoreAuditingTests
{
    [Fact]
    public void AddEfCoreAuditing_registers_AuditOptions_as_singleton()
    {
        var services = new ServiceCollection();

        services.AddEfCoreAuditing<DefaultCtorTestUserProvider>();

        using var sp = services.BuildServiceProvider();
        var first  = sp.GetRequiredService<AuditOptions>();
        var second = sp.GetRequiredService<AuditOptions>();

        Assert.Same(first, second);
    }



    [Fact]
    public void AddEfCoreAuditing_registers_user_provider_as_scoped()
    {
        var services = new ServiceCollection();

        services.AddEfCoreAuditing<DefaultCtorTestUserProvider>();

        using var sp = services.BuildServiceProvider();

        IAuditUserProvider firstScope;
        IAuditUserProvider secondScope;

        using (var scope = sp.CreateScope())
        {
            firstScope = scope.ServiceProvider.GetRequiredService<IAuditUserProvider>();
            var alsoFirst = scope.ServiceProvider.GetRequiredService<IAuditUserProvider>();
            Assert.Same(firstScope, alsoFirst); // same within a scope
        }
        using (var scope = sp.CreateScope())
        {
            secondScope = scope.ServiceProvider.GetRequiredService<IAuditUserProvider>();
        }

        Assert.NotSame(firstScope, secondScope); // distinct across scopes
    }



    [Fact]
    public void AddEfCoreAuditing_seeds_default_value_and_entity_key_serializers()
    {
        var services = new ServiceCollection();

        services.AddEfCoreAuditing<DefaultCtorTestUserProvider>();

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<AuditOptions>();

        Assert.IsType<StringAuditValueSerializer>(options.ValueSerializer);
        Assert.IsType<PipeDelimitedEntityKeySerializer>(options.EntityKeySerializer);
    }



    [Fact]
    public void AddEfCoreAuditing_runs_configure_callback_with_default_serializers_pre_populated()
    {
        var services = new ServiceCollection();

        services.AddEfCoreAuditing<DefaultCtorTestUserProvider>(opts =>
        {
            Assert.NotNull(opts.ValueSerializer);
            Assert.NotNull(opts.EntityKeySerializer);

            opts.Schema               = "audit";
            opts.HeaderTableName      = "MyHeader";
            opts.DetailTableName      = "MyDetail";
            opts.CaptureDeletedValues = true;
        });

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<AuditOptions>();

        Assert.Equal("audit",    options.Schema);
        Assert.Equal("MyHeader", options.HeaderTableName);
        Assert.Equal("MyDetail", options.DetailTableName);
        Assert.True(options.CaptureDeletedValues);
    }



    [Fact]
    public void AddEfCoreAuditing_when_configure_nulls_serializers_restores_defaults()
    {
        var services = new ServiceCollection();

        services.AddEfCoreAuditing<DefaultCtorTestUserProvider>(opts =>
        {
            // Deliberately wipe to confirm the post-callback null-coalesce kicks in.
            opts.ValueSerializer     = null;
            opts.EntityKeySerializer = null;
        });

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<AuditOptions>();

        Assert.IsType<StringAuditValueSerializer>(options.ValueSerializer);
        Assert.IsType<PipeDelimitedEntityKeySerializer>(options.EntityKeySerializer);
    }



    [Fact]
    public void AddEfCoreAuditing_when_called_twice_keeps_first_registration()
    {
        var services = new ServiceCollection();

        services.AddEfCoreAuditing<DefaultCtorTestUserProvider>(opts => opts.HeaderTableName = "First");
        services.AddEfCoreAuditing<DefaultCtorTestUserProvider>(opts => opts.HeaderTableName = "Second");

        using var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<AuditOptions>();

        // TryAddSingleton semantics: first registration wins.
        Assert.Equal("First", options.HeaderTableName);
    }



    [Fact]
    public void AddEfCoreAuditing_throws_on_null_services()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() =>
            services.AddEfCoreAuditing<DefaultCtorTestUserProvider>());
    }
}
