using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.Audit.Entities;

namespace Wolfgang.Audit.Internal;



/// <summary>
/// Shared snapshot / materialize logic used by both the <see cref="AuditingDbContext"/>
/// base class and the auto-transaction save-changes interceptor. Capturing the
/// change-tracker state in a single place keeps the two integration models
/// behaviorally identical.
/// </summary>
internal static class AuditCapture
{
    /// <summary>
    /// Walks <see cref="DbContext.ChangeTracker"/> and produces a snapshot of every
    /// entity that should produce an audit header. Skips the audit entities themselves
    /// and anything carrying <see cref="NotAuditedAttribute"/>.
    /// </summary>
    public static List<PendingAuditEntry> CapturePending
    (
        DbContext context,
        AuditOptions options
    )
    {
        var entries = context.ChangeTracker.Entries().ToList();
        var pending = new List<PendingAuditEntry>(entries.Count);

        foreach (var entry in entries)
        {
            var clrType = entry.Metadata.ClrType;
            if (clrType == typeof(AuditHeader) || clrType == typeof(AuditDetail))
            {
                continue;
            }

            if (clrType.GetCustomAttribute<NotAuditedAttribute>(inherit: false) is not null)
            {
                continue;
            }

            var operation = entry.State switch
            {
                EntityState.Added    => (AuditOperation?)AuditOperation.Insert,
                EntityState.Modified => AuditOperation.Update,
                EntityState.Deleted  => AuditOperation.Delete,
                _                    => null,
            };

            if (operation is null)
            {
                continue;
            }

            var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
            var keyValuesBeforeSave = keyProperties is null
                ? (IReadOnlyList<object?>)Array.Empty<object?>()
                : keyProperties.Select(p => entry.Property(p.Name).CurrentValue).ToList();

            pending.Add(new PendingAuditEntry
            {
                Entry = entry,
                Operation = operation.Value,
                EntityType = clrType.FullName ?? clrType.Name,
                EntityTable = entry.Metadata.GetSchemaQualifiedTableName() ?? entry.Metadata.GetTableName() ?? clrType.Name,
                ChangedValues = CaptureValues(entry, operation.Value, options),
                KeyValuesBeforeSave = keyValuesBeforeSave,
            });
        }

        return pending;
    }



    /// <summary>
    /// Materializes <see cref="AuditHeader"/> + <see cref="AuditDetail"/> entities from
    /// the pending snapshot and attaches them to <paramref name="context"/>. Caller is
    /// responsible for calling <c>SaveChanges</c> afterwards.
    /// </summary>
    public static void AddAuditEntities
    (
        DbContext context,
        List<PendingAuditEntry> pending,
        IAuditUserProvider userProvider,
        AuditOptions options,
        Guid transactionId
    )
    {
        var user = userProvider.GetCurrentUser();
        var auditedAt = DateTime.UtcNow;
        var keySerializer = options.EntityKeySerializer!;
        var valueSerializer = options.ValueSerializer!;

        foreach (var entry in pending)
        {
            var keyValues = entry.Operation == AuditOperation.Delete
                ? entry.KeyValuesBeforeSave
                : ResolvePostSaveKey(entry);

            var header = new AuditHeader
            {
                HeaderId = Guid.NewGuid(),
                TransactionId = transactionId,
                AuditedAtUtc = auditedAt,
                UserId = user.UserId,
                OnBehalfOfUserId = user.OnBehalfOfUserId,
                EntityType = entry.EntityType,
                EntityTable = entry.EntityTable,
                EntityKey = keySerializer.Serialize(keyValues),
                Operation = entry.Operation,
            };

            foreach (var changed in entry.ChangedValues)
            {
                var detail = new AuditDetail
                {
                    HeaderId = header.HeaderId,
                    ColumnName = changed.ColumnName,
                };

                var writer = new ColumnValueWriter(detail);
                detail.ValueType = valueSerializer.Encode(changed.Value, changed.ClrType, writer);
                header.Details.Add(detail);
            }

            context.Add(header);
        }
    }



    private static IReadOnlyList<PendingAuditValue> CaptureValues
    (
        EntityEntry entry,
        AuditOperation operation,
        AuditOptions options
    )
    {
        if (operation == AuditOperation.Delete && !options.CaptureDeletedValues)
        {
            return Array.Empty<PendingAuditValue>();
        }

        var values = new List<PendingAuditValue>();
        foreach (var property in entry.Properties)
        {
            var propInfo = property.Metadata.PropertyInfo;
            if (propInfo is not null && propInfo.GetCustomAttribute<NotAuditedAttribute>(inherit: false) is not null)
            {
                continue;
            }

            if (property.Metadata.IsPrimaryKey())
            {
                continue;
            }

            switch (operation)
            {
                case AuditOperation.Insert:
                    values.Add(new PendingAuditValue
                    {
                        ColumnName = GetMappedColumnName(property),
                        ClrType    = property.Metadata.ClrType,
                        Value      = property.CurrentValue,
                    });
                    break;

                case AuditOperation.Update:
                    if (property.IsModified)
                    {
                        values.Add(new PendingAuditValue
                        {
                            ColumnName = GetMappedColumnName(property),
                            ClrType    = property.Metadata.ClrType,
                            Value      = property.CurrentValue,
                        });
                    }
                    break;

                case AuditOperation.Delete:
                    values.Add(new PendingAuditValue
                    {
                        ColumnName = GetMappedColumnName(property),
                        ClrType    = property.Metadata.ClrType,
                        Value      = property.OriginalValue,
                    });
                    break;
            }
        }

        return values;
    }



    /// <summary>
    /// Returns the database column name EF Core mapped the property to, honouring
    /// <c>[Column]</c> / <c>HasColumnName(...)</c> overrides. Falls back to the
    /// CLR property name for providers / shapes where no column mapping exists
    /// (e.g. owned-entity edge cases), matching pre-fix behaviour.
    /// </summary>
    private static string GetMappedColumnName(PropertyEntry property)
    {
        // Use the StoreObjectIdentifier overload (available net6+ and not
        // obsolete) — the parameterless GetColumnName() was marked obsolete
        // on EF Core 7+ because it didn't disambiguate inheritance / table-
        // sharing scenarios. The StoreObjectIdentifier created here resolves
        // to the property's primary table mapping. EF Core 8+ moved the
        // declaring-type accessor from DeclaringEntityType (obsolete) to
        // DeclaringType (returns ITypeBase) which the conditional below picks
        // depending on TFM.
#if NET8_0_OR_GREATER
        if (property.Metadata.DeclaringType is IEntityType entityType)
#else
        var entityType = property.Metadata.DeclaringEntityType;
        if (entityType is not null)
#endif
        {
            var storeObject = StoreObjectIdentifier.Create(entityType, StoreObjectType.Table);
            if (storeObject is not null)
            {
                var name = property.Metadata.GetColumnName(storeObject.Value);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
        }
        return property.Metadata.Name;
    }



    private static IReadOnlyList<object?> ResolvePostSaveKey(PendingAuditEntry entry)
    {
        var keyProperties = entry.Entry.Metadata.FindPrimaryKey()?.Properties;
        if (keyProperties is null)
        {
            return entry.KeyValuesBeforeSave;
        }

        return keyProperties
            .Select(p => entry.Entry.Property(p.Name).CurrentValue)
            .ToList();
    }
}
