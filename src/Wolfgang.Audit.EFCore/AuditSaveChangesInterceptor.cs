using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Internal;

namespace Wolfgang.Audit;

/// <summary>
/// EF Core save-changes interceptor that captures Insert / Update / Delete operations
/// on the consumer's <see cref="DbContext"/> and persists header + detail rows to the
/// audit tables inside the same transaction as the user's save.
/// </summary>
/// <remarks>
/// <para>
/// Capture is two-phase to handle database-generated primary keys:
/// </para>
/// <list type="number">
///   <item><description>In <c>SavingChangesAsync</c>, snapshot every <see cref="EntityEntry"/> the change tracker reports.</description></item>
///   <item><description>In <c>SavedChangesAsync</c> — after EF Core has applied the INSERT/UPDATE/DELETE statements and populated generated keys, but still inside the same transaction — materialize header + detail rows, attach them to the context, and call <c>SaveChangesAsync</c> a second time. A re-entry guard prevents the audit save from auditing itself.</description></item>
/// </list>
/// </remarks>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IAuditUserProvider _userProvider;
    private readonly AuditOptions _options;
    private readonly AsyncLocal<AuditSaveState?> _state = new();

    /// <summary>Constructs the interceptor with its dependencies.</summary>
    public AuditSaveChangesInterceptor(IAuditUserProvider userProvider, AuditOptions options)
    {
        ArgumentNullException.ThrowIfNull(userProvider);
        ArgumentNullException.ThrowIfNull(options);

        if (options.ValueSerializer is null)
        {
            throw new ArgumentException("AuditOptions.ValueSerializer must be set before the interceptor is constructed.", nameof(options));
        }

        if (options.EntityKeySerializer is null)
        {
            throw new ArgumentException("AuditOptions.EntityKeySerializer must be set before the interceptor is constructed.", nameof(options));
        }

        _userProvider = userProvider;
        _options = options;
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (_state.Value?.IsAuditSave == true)
        {
            return ValueTask.FromResult(result);
        }

        var context = eventData.Context;
        if (context is null)
        {
            return ValueTask.FromResult(result);
        }

        var pending = CapturePending(context);
        _state.Value = new AuditSaveState
        {
            TransactionId = Guid.NewGuid(),
            Pending = pending,
        };

        return ValueTask.FromResult(result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        var state = _state.Value;
        if (state is null || state.IsAuditSave)
        {
            _state.Value = null;
            return result;
        }

        var context = eventData.Context;
        if (context is null || state.Pending.Count == 0)
        {
            _state.Value = null;
            return result;
        }

        try
        {
            state.IsAuditSave = true;
            await PersistAuditAsync(context, state, cancellationToken).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // re-state the cleanup regardless of failure type
        finally
#pragma warning restore CA1031
        {
            _state.Value = null;
        }

        return result;
    }

    private IReadOnlyList<PendingAuditEntry> CapturePending(DbContext context)
    {
        var entries = context.ChangeTracker.Entries().ToList();
        if (entries.Count == 0)
        {
            return Array.Empty<PendingAuditEntry>();
        }

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
                // Entire entity is not logged.
                continue;
            }

            var operation = entry.State switch
            {
                EntityState.Added => (AuditOperation?)AuditOperation.Insert,
                EntityState.Modified => AuditOperation.Update,
                EntityState.Deleted => AuditOperation.Delete,
                _ => null,
            };

            if (operation is null)
            {
                continue;
            }

            pending.Add(new PendingAuditEntry
            {
                Entry = entry,
                Operation = operation.Value,
                EntityType = clrType.FullName ?? clrType.Name,
                EntityTable = entry.Metadata.GetSchemaQualifiedTableName() ?? entry.Metadata.GetTableName() ?? clrType.Name,
                ChangedValues = CaptureValues(entry, operation.Value),
            });
        }

        return pending;
    }

    private IReadOnlyList<PendingAuditValue> CaptureValues(EntityEntry entry, AuditOperation operation)
    {
        if (operation == AuditOperation.Delete && !_options.CaptureDeletedValues)
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
                        ColumnName = property.Metadata.Name,
                        ClrType = property.Metadata.ClrType,
                        Value = property.CurrentValue,
                    });
                    break;

                case AuditOperation.Update:
                    if (property.IsModified)
                    {
                        values.Add(new PendingAuditValue
                        {
                            ColumnName = property.Metadata.Name,
                            ClrType = property.Metadata.ClrType,
                            Value = property.CurrentValue,
                        });
                    }
                    break;

                case AuditOperation.Delete:
                    values.Add(new PendingAuditValue
                    {
                        ColumnName = property.Metadata.Name,
                        ClrType = property.Metadata.ClrType,
                        Value = property.OriginalValue,
                    });
                    break;
            }
        }

        return values;
    }

    private Task PersistAuditAsync(DbContext context, AuditSaveState state, CancellationToken cancellationToken)
    {
        var user = _userProvider.GetCurrentUser();
        var auditedAt = DateTime.UtcNow;
        var keySerializer = _options.EntityKeySerializer!;
        var valueSerializer = _options.ValueSerializer!;

        foreach (var pending in state.Pending)
        {
            var keyValues = pending.Entry.Metadata
                .FindPrimaryKey()?
                .Properties
                .Select(p => pending.Entry.Property(p.Name).CurrentValue)
                .ToList() ?? new List<object?>();

            var header = new AuditHeader
            {
                HeaderId = Guid.NewGuid(),
                TransactionId = state.TransactionId,
                AuditedAtUtc = auditedAt,
                UserId = user.UserId,
                OnBehalfOfUserId = user.OnBehalfOfUserId,
                EntityType = pending.EntityType,
                EntityTable = pending.EntityTable,
                EntityKey = keySerializer.Serialize(keyValues),
                Operation = pending.Operation,
            };

            foreach (var changed in pending.ChangedValues)
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

        return context.SaveChangesAsync(cancellationToken);
    }
}
