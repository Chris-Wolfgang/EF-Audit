using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Wolfgang.AuditTrail.Entities;

namespace Wolfgang.AuditTrail.Internal;



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
            var captured = TryCaptureEntry(entry, options);
            if (captured is not null)
            {
                pending.Add(captured);
            }
        }

        return pending;
    }



    private static PendingAuditEntry? TryCaptureEntry(EntityEntry entry, AuditOptions options)
    {
        var clrType = entry.Metadata.ClrType;
        if (clrType == typeof(AuditHeader) || clrType == typeof(AuditDetail))
        {
            return null;
        }

        if (clrType.GetCustomAttribute<NotAuditedAttribute>(inherit: false) is not null)
        {
            return null;
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
            return null;
        }

        var changedValues = CaptureValues(entry, operation.Value, options);

        // Skip Updates whose only modified properties are [NotAudited] —
        // CaptureValues filters them out and returns an empty list. A header
        // with zero detail rows isn't informative ("something on this row
        // changed but we can't tell you what") and pollutes the audit table.
        // Inserts and Deletes still produce a header (record-of-creation /
        // record-of-deletion is meaningful even without column-level changes).
        if (operation.Value == AuditOperation.Update && changedValues.Count == 0)
        {
            return null;
        }

        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        var keyValuesBeforeSave = keyProperties is null
            ? (IReadOnlyList<object?>)Array.Empty<object?>()
            : keyProperties.Select(p => entry.Property(p.Name).CurrentValue).ToList();

        return new PendingAuditEntry
        {
            Entry = entry,
            Operation = operation.Value,
            EntityType = clrType.FullName ?? clrType.Name,
            EntityTable = entry.Metadata.GetSchemaQualifiedTableName() ?? entry.Metadata.GetTableName() ?? clrType.Name,
            ChangedValues = changedValues,
            KeyValuesBeforeSave = keyValuesBeforeSave,
        };
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

                var detailValue = ResolveDetailValue(entry, changed);

                var writer = new ColumnValueWriter(detail);
                detail.ValueType = valueSerializer.Encode(detailValue, changed.ClrType, writer);
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

            var captured = CapturePropertyValue(property, operation);
            if (captured is not null)
            {
                values.Add(captured);
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



    /// <summary>
    /// Builds the per-operation <see cref="PendingAuditValue"/> for one property.
    /// Inserts/Updates record <c>CurrentValue</c> (re-read post-save), Deletes
    /// record <c>OriginalValue</c> (the only authoritative source after detach),
    /// and Updates with <c>!IsModified</c> are skipped.
    /// </summary>
    private static PendingAuditValue? CapturePropertyValue(PropertyEntry property, AuditOperation operation)
    {
        switch (operation)
        {
            case AuditOperation.Insert:
                return new PendingAuditValue
                {
                    ColumnName   = GetMappedColumnName(property),
                    ClrType      = property.Metadata.ClrType,
                    PropertyName = property.Metadata.Name,
                    Value        = property.CurrentValue,
                };

            case AuditOperation.Update:
                return property.IsModified
                    ? new PendingAuditValue
                    {
                        ColumnName   = GetMappedColumnName(property),
                        ClrType      = property.Metadata.ClrType,
                        PropertyName = property.Metadata.Name,
                        Value        = property.CurrentValue,
                    }
                    : null;

            case AuditOperation.Delete:
                return new PendingAuditValue
                {
                    ColumnName   = GetMappedColumnName(property),
                    ClrType      = property.Metadata.ClrType,
                    PropertyName = property.Metadata.Name,
                    Value        = property.OriginalValue,
                };

            default:
                return null;
        }
    }



    /// <summary>
    /// For Inserts and Updates, re-reads <c>CurrentValue</c> from the still-tracked
    /// <see cref="EntityEntry"/> after the user's <c>SaveChanges</c> has run.
    /// This is the only way to capture database-generated values (defaults,
    /// computed columns, identity columns, server-side <c>ValueConverter</c>
    /// output) — the pre-save snapshot only has the CLR default.
    /// For Deletes the snapshot is authoritative (the entry has detached).
    /// </summary>
    private static object? ResolveDetailValue(PendingAuditEntry entry, PendingAuditValue changed)
    {
        if (entry.Operation == AuditOperation.Delete)
        {
            return changed.Value;
        }

        try
        {
            return entry.Entry.Property(changed.PropertyName).CurrentValue;
        }
        catch (InvalidOperationException)
        {
            // The entry is no longer tracking this property (rare — e.g. owned
            // entity re-shaping mid-save). Fall back to the pre-save snapshot
            // so the audit row is still emitted, just with the pre-save value.
            return changed.Value;
        }
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
