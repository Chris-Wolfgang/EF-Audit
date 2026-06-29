namespace Wolfgang.Audit.Schema;



/// <summary>
/// Compile-time constants shared across the schema-migration components.
/// </summary>
public static class AuditSchemaConstants
{
    /// <summary>
    /// Name of the version tracking table. Underscore-prefixed to make its
    /// "internal bookkeeping" intent obvious next to the consumer's own tables,
    /// and distinct from EF Core's <c>__EFMigrationsHistory</c>.
    /// </summary>
    public const string VersionTableName = "__AuditSchemaVersion";

    /// <summary>
    /// Schema version this build of the library installs / upgrades to. Bump
    /// when a new <c>AuditSchemaSnapshotV{N}</c> is shipped alongside a model
    /// change.
    /// </summary>
    public const int CurrentSchemaVersion = 1;
}
