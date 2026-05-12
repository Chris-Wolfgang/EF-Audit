namespace Wolfgang.Audit;

/// <summary>
/// Categorizes how an <see cref="AuditValueColumn"/> is stored in the database.
/// </summary>
/// <remarks>
/// The schema installer translates these to provider-specific column types:
/// <list type="bullet">
///   <item><description><see cref="Text"/> maps to <c>nvarchar(max)</c> on SQL Server, <c>text</c> on PostgreSQL, <c>TEXT</c> on SQLite, <c>longtext</c> on MySQL.</description></item>
///   <item><description><see cref="Binary"/> maps to <c>varbinary(max)</c> on SQL Server, <c>bytea</c> on PostgreSQL, <c>BLOB</c> on SQLite, <c>longblob</c> on MySQL.</description></item>
/// </list>
/// </remarks>
public enum AuditValueStorageKind
{
    /// <summary>Unbounded text storage.</summary>
    Text,

    /// <summary>Unbounded binary storage.</summary>
    Binary,
}
