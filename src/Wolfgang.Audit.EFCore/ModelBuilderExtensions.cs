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

        modelBuilder.Entity<AuditHeader>(b => ConfigureAuditHeader(b, options));
        modelBuilder.Entity<AuditDetail>(b => ConfigureAuditDetail(b, options));

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

        // Store Operation as the literal character 'I' / 'U' / 'D' so the column
        // reads naturally when querying directly. The cast chain enum → byte →
        // char → string yields a 1-character string that SQLite stores as TEXT
        // (a bare char would be stored as INTEGER codepoint instead).
        builder.Property(h => h.Operation)
            .HasConversion(
                v => new string((char)(byte)v, 1),
                s => (AuditOperation)(byte)s[0])
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
        builder.Property(d => d.ValueType).HasMaxLength(20).IsRequired();

        builder.HasOne(d => d.Header)
            .WithMany(h => h.Details)
            .HasForeignKey(d => d.HeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.HeaderId);
    }
}
