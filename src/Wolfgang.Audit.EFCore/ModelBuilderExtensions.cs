using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.Audit.Entities;

namespace Wolfgang.Audit;

/// <summary>
/// EF Core model-configuration extensions that register the audit entity types on a
/// consumer's <see cref="DbContext"/>.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    /// Applies the <see cref="AuditHeader"/> and <see cref="AuditDetail"/> entity
    /// mappings using the supplied <see cref="AuditOptions"/>. Call from
    /// <c>OnModelCreating</c>.
    /// </summary>
    public static ModelBuilder ApplyAuditing(this ModelBuilder modelBuilder, AuditOptions options)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ArgumentNullException.ThrowIfNull(options);

        modelBuilder.Entity<AuditHeader>(builder => ConfigureAuditHeader(builder, options));
        modelBuilder.Entity<AuditDetail>(builder => ConfigureAuditDetail(builder, options));

        return modelBuilder;
    }

    private static void ConfigureAuditHeader(EntityTypeBuilder<AuditHeader> builder, AuditOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Schema))
        {
            builder.ToTable(options.HeaderTableName, options.Schema);
        }
        else
        {
            builder.ToTable(options.HeaderTableName);
        }

        builder.HasKey(h => h.HeaderId);
        builder.Property(h => h.HeaderId).ValueGeneratedNever();
        builder.Property(h => h.TransactionId).IsRequired();
        builder.Property(h => h.AuditedAtUtc).HasPrecision(6).IsRequired();
        builder.Property(h => h.UserId).HasMaxLength(256).IsRequired();
        builder.Property(h => h.OnBehalfOfUserId).HasMaxLength(256);
        builder.Property(h => h.EntityType).HasMaxLength(512).IsRequired();
        builder.Property(h => h.EntityTable).HasMaxLength(512).IsRequired();
        builder.Property(h => h.EntityKey).HasMaxLength(512).IsRequired();

        // Store the operation as the literal character 'I' / 'U' / 'D'. The enum's
        // backing byte values are the ASCII codes for those characters, so we cast
        // through byte then char on the way out, and char then byte (then enum) on
        // the way back in. Result in the DB is a 1-character column that reads
        // naturally when querying directly.
        builder.Property(h => h.Operation)
            .HasConversion(
                v => (char)(byte)v,
                c => (AuditOperation)(byte)c)
            .HasMaxLength(1)
            .IsRequired();

        builder.HasIndex(h => h.TransactionId);
        builder.HasIndex(h => h.AuditedAtUtc);
        builder.HasIndex(h => new { h.EntityType, h.EntityKey });
    }

    private static void ConfigureAuditDetail(EntityTypeBuilder<AuditDetail> builder, AuditOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Schema))
        {
            builder.ToTable(options.DetailTableName, options.Schema);
        }
        else
        {
            builder.ToTable(options.DetailTableName);
        }

        builder.HasKey(d => d.DetailId);
        builder.Property(d => d.DetailId).ValueGeneratedOnAdd();
        builder.Property(d => d.HeaderId).IsRequired();
        builder.Property(d => d.ColumnName).HasMaxLength(256).IsRequired();
        builder.Property(d => d.ValueText);
        builder.Property(d => d.ValueBinary);

        // 256 accommodates the longest known discriminator shape: `Enum:<FullName>`
        // where <FullName> can be an arbitrarily-namespaced enum type (e.g.
        // `Enum:Acme.Domain.Catalog.Products.ProductCategory`).
        builder.Property(d => d.ValueType).HasMaxLength(256).IsRequired();

        builder.HasOne(d => d.Header)
            .WithMany(h => h.Details)
            .HasForeignKey(d => d.HeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.HeaderId);
    }
}
