using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Wolfgang.AuditTrail.Schema;



/// <summary>
/// Reads and writes the single row in <c>__AuditSchemaVersion</c> that records
/// the installed audit-schema version. Implemented via EF Core LINQ rather than
/// raw SQL so the queries stay provider-agnostic.
/// </summary>
internal static class AuditSchemaVersionStore
{
    /// <summary>
    /// Returns the installed schema version, or <c>0</c> if the version table
    /// does not exist (fresh database) or has no row.
    /// </summary>
    public static async Task<int> ReadInstalledVersionAsync
    (
        AuditMigrationsDbContext context,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            var row = await context.Set<AuditSchemaVersion>()
                .AsNoTracking()
                .OrderByDescending(v => v.Version)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            return row?.Version ?? 0;
        }
        catch (DbException ex) when (TableNotFoundIndicators.IsTableNotFound(ex))
        {
            // Version table doesn't exist yet — treat as a fresh install. We
            // only swallow the narrow "table not found" case; permission
            // denied, network errors, or malformed SQL still propagate so a
            // misconfigured environment fails loudly instead of pretending the
            // schema is missing.
            return 0;
        }
    }



    /// <summary>
    /// Upserts the version row to <paramref name="version"/>. Called from
    /// <c>AuditSchemaMigrator</c> inside the migration transaction.
    /// </summary>
    public static async Task UpsertInstalledVersionAsync
    (
        AuditMigrationsDbContext context,
        int version,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        var existing = await context.Set<AuditSchemaVersion>()
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            // VSTHRD103: DbSet.Add is the recommended API. EF Core's AddAsync exists
            // only to support special value generators (HiLo / sequences) and is not
            // recommended for normal use — Add does not block. Suppress the false positive.
#pragma warning disable VSTHRD103
            context.Set<AuditSchemaVersion>().Add(new AuditSchemaVersion
            {
                Id      = 1,
                Version = version,
            });
#pragma warning restore VSTHRD103
        }
        else
        {
            existing.Version = version;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }
}
