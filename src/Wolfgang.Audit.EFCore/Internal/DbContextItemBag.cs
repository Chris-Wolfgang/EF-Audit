using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Internal;



/// <summary>
/// Per-<see cref="DbContext"/> key/value bag used by
/// <see cref="AuditSaveChangesInterceptor"/> to thread state between the
/// <c>SavingChanges</c> and <c>SavedChanges</c> hooks. Backed by a
/// <see cref="ConditionalWeakTable{TKey, TValue}"/> so entries are reclaimed when
/// the context is GC'd — no cross-request leak in long-lived apps.
/// </summary>
internal static class DbContextItemBag
{
    private static readonly ConditionalWeakTable<DbContext, Dictionary<string, object>> _state = new();



    public static void SetItem(this DbContext context, string key, object? value)
    {
        if (value is null)
        {
            Get(context).Remove(key);
        }
        else
        {
            Get(context)[key] = value;
        }
    }



    public static T? GetItem<T>(this DbContext context, string key)
    {
        if (_state.TryGetValue(context, out var state) && state!.TryGetValue(key, out var value))
        {
            return (T?)value;
        }
        return default;
    }



    public static void RemoveItem(this DbContext context, string key)
    {
        if (_state.TryGetValue(context, out var state))
        {
            state!.Remove(key);
        }
    }



    private static Dictionary<string, object> Get(DbContext context)
    {
        return _state.GetValue(context, _ => new Dictionary<string, object>(StringComparer.Ordinal));
    }
}
