using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Covers <see cref="DbContextOptionsBuilderExtensions.UseAuditing"/>.
/// </summary>
public class UseAuditingTests
{
    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddEfCoreAuditing<DefaultCtorTestUserProvider>();
        return services.BuildServiceProvider();
    }



    [Fact]
    public void UseAuditing_adds_AuditSaveChangesInterceptor_to_DbContextOptions()
    {
        using var sp = BuildProvider();
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var builder = new DbContextOptionsBuilder()
            .UseSqlite(connection)
            .UseAuditing(sp);

        var interceptors = builder.Options
            .FindExtension<CoreOptionsExtension>()
            ?.Interceptors;

        Assert.NotNull(interceptors);
        Assert.Contains(interceptors!, i => i is AuditSaveChangesInterceptor);
    }



    [Fact]
    public void UseAuditing_returns_same_builder_for_chaining()
    {
        using var sp = BuildProvider();
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();

        var builder = new DbContextOptionsBuilder().UseSqlite(connection);

        var returned = builder.UseAuditing(sp);

        Assert.Same(builder, returned);
    }



    [Fact]
    public void UseAuditing_throws_on_null_options_builder()
    {
        using var sp = BuildProvider();
        DbContextOptionsBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() => builder.UseAuditing(sp));
    }



    [Fact]
    public void UseAuditing_throws_on_null_service_provider()
    {
        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder().UseSqlite(connection);

        Assert.Throws<ArgumentNullException>(() => builder.UseAuditing(serviceProvider: null!));
    }



    [Fact]
    public void UseAuditing_throws_when_AuditOptions_missing_from_service_provider()
    {
        // Build a service provider that has IAuditUserProvider but NOT AuditOptions.
        // GetRequiredService<AuditOptions>() inside UseAuditing should throw.
        var services = new ServiceCollection();
        services.AddScoped<IAuditUserProvider, DefaultCtorTestUserProvider>();
        using var sp = services.BuildServiceProvider();

        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder().UseSqlite(connection);

        Assert.Throws<InvalidOperationException>(() => builder.UseAuditing(sp));
    }



    [Fact]
    public void UseAuditing_throws_when_user_provider_missing_from_service_provider()
    {
        // Has AuditOptions but no IAuditUserProvider.
        var services = new ServiceCollection();
        services.AddSingleton(new AuditOptions
        {
            ValueSerializer     = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        });
        using var sp = services.BuildServiceProvider();

        using var connection = new SqliteConnection("Filename=:memory:");
        connection.Open();
        var builder = new DbContextOptionsBuilder().UseSqlite(connection);

        Assert.Throws<InvalidOperationException>(() => builder.UseAuditing(sp));
    }
}
