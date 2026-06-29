using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wolfgang.AuditTrail.Entities;

namespace Wolfgang.AuditTrail;

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
        // EntityType / EntityKey are indexed together (see HasIndex below) and
        // must fit inside the tightest cross-provider nonclustered key limit
        // — SQL Server's 1700 bytes for a nonclustered key. With nvarchar at
        // 2 bytes/char that gives an 850-char combined ceiling; with MySQL
        // InnoDB on utf8mb4 (4 bytes/char) that gives a 768-char ceiling. We
        // size each at 256 -> 512 chars combined = 1024 bytes nvarchar /
        // 2048 bytes utf8mb4. Comfortable headroom on every supported provider,
        // and still well above realistic upper bounds: CLR full type names are
        // typically <200 chars and serialized PKs <50 chars. EntityTable is
        // not part of any composite index and stays at 384.
        builder.Property(h => h.EntityType).HasMaxLength(256).IsRequired();
        builder.Property(h => h.EntityTable).HasMaxLength(384).IsRequired();
        builder.Property(h => h.EntityKey).HasMaxLength(256).IsRequired();

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
        // 256 (not the historical 20) so the StringAuditValueSerializer's
        // "Enum:<AssemblyQualifiedName>" discriminator fits for any realistic
        // namespaced enum type. AssemblyQualifiedName is bounded by the CLR
        // type-name limit, which is comfortably under 256 for practical types.
        builder.Property(d => d.ValueType).HasMaxLength(256).IsRequired();

        builder.HasOne(d => d.Header)
            .WithMany(h => h.Details)
            .HasForeignKey(d => d.HeaderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.HeaderId);

        // Standalone ColumnName index — the README positions the detail table
        // as queryable by changed column ("every change to Customer.Email").
        // Without this index, such queries full-scan AuditDetail at production
        // sizes. The HeaderId index alone serves only header-rooted queries.
        builder.HasIndex(d => d.ColumnName);
    }
}
