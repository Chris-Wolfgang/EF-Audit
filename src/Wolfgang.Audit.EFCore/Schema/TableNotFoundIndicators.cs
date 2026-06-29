using System.Data.Common;

namespace Wolfgang.Audit.Schema;



/// <summary>
/// Provider-specific error indicators that mean "the table the query targeted
/// does not exist." A narrow allow-list rather than catching every
/// <see cref="DbException"/>, so real failures (permission denied, network
/// dropped, malformed SQL) propagate instead of being silently treated as a
/// fresh-install signal.
/// </summary>
internal static class TableNotFoundIndicators
{
    // SQL Server: error 208 "Invalid object name"
    // PostgreSQL: SQLSTATE 42P01 "undefined_table"
    // MySQL:      error 1146 "Table 'x.y' doesn't exist"
    // SQLite:     "no such table"
    private static readonly string[] Patterns =
    {
        "no such table",                     // SQLite
        "invalid object name",               // SQL Server
        "doesn't exist",                     // MySQL
        "does not exist",                    // PostgreSQL
        "undefined_table",                   // PostgreSQL SQLSTATE
        "42P01",                             // PostgreSQL SQLSTATE
    };

    public static bool IsTableNotFound(DbException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var message  = exception.Message ?? string.Empty;
        var sqlState = exception.SqlState ?? string.Empty;

        if (Patterns.Any(p => message.Contains(p, StringComparison.OrdinalIgnoreCase)
                              || sqlState.Contains(p, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        return false;
    }
}
