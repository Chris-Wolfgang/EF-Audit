using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.AuditTrail.Entities;
using Wolfgang.AuditTrail.Serializers;
using Wolfgang.AuditTrail.Tests.Unit.TestSupport;
using Xunit;

namespace Wolfgang.AuditTrail.Tests.Unit;



/// <summary>
/// Pins every statement in <see cref="ModelBuilderExtensions.ApplyAuditing"/> by
/// asserting the resulting EF model metadata — table names (the no-schema default;
/// the schema-present branch is covered in <c>SmallCoverageGapsTests</c>), keys,
/// value generation, nullability, max lengths, precision, indexes, the
/// header→detail cascade FK, and the Operation value converter. Removing or
/// altering any single configuration statement changes the metadata a test below
/// inspects.
/// </summary>
public sealed class ModelBuilderConfigurationTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public ModelBuilderConfigurationTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();
    }

    public void Dispose() => _connection.Dispose();

    private TestDbContext BuildContext(string? schema)
    {
        var options = new AuditOptions
        {
            Schema = schema,
            ValueSerializer = new StringAuditValueSerializer(),
            EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
        };
        var builder = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            // EF caches the model per (context type, provider). Without this, the
            // first test to touch TestDbContext.Model caches it and later tests
            // reuse it — so ConfigureAudit* never re-executes here and mutation
            // testing can't attribute those mutants to these assertions. Force a
            // fresh model per build so each test actually exercises the config.
            .ReplaceService<IModelCacheKeyFactory, UncachedModelCacheKeyFactory>();
        return new TestDbContext(builder.Options, new StaticAuditUserProvider("u", null), options);
    }

    // Interface-mandated signatures; parameters are intentionally unused because a
    // unique key per call is exactly what disables the model cache.
#pragma warning disable RCS1163 // Unused parameter
    private sealed class UncachedModelCacheKeyFactory : IModelCacheKeyFactory
    {
        public object Create(DbContext context, bool designTime) => new object();

        public object Create(DbContext context) => new object();
    }
#pragma warning restore RCS1163

    // Model metadata is immutable and outlives the context, so it is safe to read
    // after the context is disposed. Dispose it here so the helpers don't leak.
    private IEntityType Header(string? schema = null)
    {
        using var ctx = BuildContext(schema);
        return ctx.Model.FindEntityType(typeof(AuditHeader))!;
    }

    private IEntityType Detail(string? schema = null)
    {
        using var ctx = BuildContext(schema);
        return ctx.Model.FindEntityType(typeof(AuditDetail))!;
    }



    [Fact]
    public void Header_table_name_is_the_configured_name_with_no_schema_by_default()
    {
        var header = Header(schema: null);
        Assert.Equal("AuditHeader", header.GetTableName());
        Assert.Null(header.GetSchema());
    }



    [Fact]
    public void Detail_table_name_is_the_configured_name_with_no_schema_by_default()
    {
        var detail = Detail(schema: null);
        Assert.Equal("AuditDetail", detail.GetTableName());
        Assert.Null(detail.GetSchema());
    }



    [Fact]
    public void Header_primary_key_is_HeaderId_and_is_never_generated()
    {
        var header = Header();
        var pk = header.FindPrimaryKey()!;
        Assert.Equal(nameof(AuditHeader.HeaderId), Assert.Single(pk.Properties).Name);
        Assert.Equal(ValueGenerated.Never, header.FindProperty(nameof(AuditHeader.HeaderId))!.ValueGenerated);
    }



    [Fact]
    public void Detail_primary_key_is_DetailId_and_is_generated_on_add()
    {
        var detail = Detail();
        var pk = detail.FindPrimaryKey()!;
        Assert.Equal(nameof(AuditDetail.DetailId), Assert.Single(pk.Properties).Name);
        Assert.Equal(ValueGenerated.OnAdd, detail.FindProperty(nameof(AuditDetail.DetailId))!.ValueGenerated);
    }



    [Theory]
    [InlineData(nameof(AuditHeader.TransactionId), false, null)]
    [InlineData(nameof(AuditHeader.AuditedAtUtc), false, null)]
    [InlineData(nameof(AuditHeader.UserId), false, 256)]
    [InlineData(nameof(AuditHeader.OnBehalfOfUserId), true, 256)]
    [InlineData(nameof(AuditHeader.EntityType), false, 256)]
    [InlineData(nameof(AuditHeader.EntityTable), false, 384)]
    [InlineData(nameof(AuditHeader.EntityKey), false, 256)]
    public void Header_property_nullability_and_length(string name, bool nullable, int? maxLength)
    {
        var prop = Header().FindProperty(name)!;
        Assert.Equal(nullable, prop.IsNullable);
        Assert.Equal(maxLength, prop.GetMaxLength());
    }



    [Fact]
    public void Header_AuditedAtUtc_has_precision_6()
    {
        Assert.Equal(6, Header().FindProperty(nameof(AuditHeader.AuditedAtUtc))!.GetPrecision());
    }



    [Theory]
    [InlineData(nameof(AuditDetail.HeaderId), false, null)]
    [InlineData(nameof(AuditDetail.ColumnName), false, 256)]
    [InlineData(nameof(AuditDetail.ValueText), true, null)]
    [InlineData(nameof(AuditDetail.ValueType), false, 256)]
    public void Detail_property_nullability_and_length(string name, bool nullable, int? maxLength)
    {
        var prop = Detail().FindProperty(name)!;
        Assert.Equal(nullable, prop.IsNullable);
        Assert.Equal(maxLength, prop.GetMaxLength());
    }



    [Fact]
    public void Header_Operation_is_stored_as_a_single_char_string_and_round_trips()
    {
        var prop = Header().FindProperty(nameof(AuditHeader.Operation))!;
        Assert.Equal(1, prop.GetMaxLength());
        Assert.False(prop.IsNullable);

        var converter = prop.GetValueConverter()!;
        // enum -> provider string
        Assert.Equal("U", converter.ConvertToProvider(AuditOperation.Update));
        Assert.Equal("I", converter.ConvertToProvider(AuditOperation.Insert));
        Assert.Equal("D", converter.ConvertToProvider(AuditOperation.Delete));
        // provider string -> enum
        Assert.Equal(AuditOperation.Update, converter.ConvertFromProvider("U"));
        Assert.Equal(AuditOperation.Insert, converter.ConvertFromProvider("I"));
        Assert.Equal(AuditOperation.Delete, converter.ConvertFromProvider("D"));
    }



    [Fact]
    public void Header_has_exactly_the_three_configured_indexes()
    {
        var indexes = Header().GetIndexes()
            .Select(i => string.Join(",", i.Properties.Select(p => p.Name)))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(nameof(AuditHeader.TransactionId), indexes);
        Assert.Contains(nameof(AuditHeader.AuditedAtUtc), indexes);
        Assert.Contains($"{nameof(AuditHeader.EntityType)},{nameof(AuditHeader.EntityKey)}", indexes);
        // Exactly these three — no extra index slips in unnoticed.
        Assert.Equal(3, indexes.Count);
    }



    [Fact]
    public void Detail_has_HeaderId_and_ColumnName_indexes()
    {
        var indexes = Detail().GetIndexes()
            .Select(i => string.Join(",", i.Properties.Select(p => p.Name)))
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains(nameof(AuditDetail.HeaderId), indexes);
        Assert.Contains(nameof(AuditDetail.ColumnName), indexes);
        // Exactly these two — the FK reuses the HeaderId index rather than adding one.
        Assert.Equal(2, indexes.Count);
    }



    [Fact]
    public void Detail_has_cascade_delete_foreign_key_to_header()
    {
        var fk = Assert.Single(Detail().GetForeignKeys());
        Assert.Equal(typeof(AuditHeader), fk.PrincipalEntityType.ClrType);
        Assert.Equal(nameof(AuditDetail.HeaderId), Assert.Single(fk.Properties).Name);
        Assert.Equal(DeleteBehavior.Cascade, fk.DeleteBehavior);
    }
}
