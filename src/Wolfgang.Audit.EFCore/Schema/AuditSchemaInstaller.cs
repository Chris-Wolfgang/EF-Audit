using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Schema;

/// <summary>
/// Creates and drops the audit tables in the consumer's database.
/// </summary>
/// <remarks>
/// <para>
/// v1 uses EF Core's own model facilities — <c>Database.EnsureCreatedAsync</c>
/// applied to a context that has the audit entities configured will produce the right
/// DDL for any supported provider. For consumers using EF Core Migrations, an
/// alternative is to let migrations own the audit tables; this helper is for the
/// non-migration path.
/// </para>
/// </remarks>
public sealed class AuditSchemaInstaller
{
    private readonly AuditOptions _options;

    /// <summary>Constructs an installer bound to the given options.</summary>
    public AuditSchemaInstaller(AuditOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options;
    }

    /// <summary>
    /// Ensures the audit header and detail tables exist on the database backing the
    /// supplied context. Safe to call repeatedly.
    /// </summary>
    public Task CreateTablesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Database.EnsureCreatedAsync(cancellationToken);
    }

    /// <summary>
    /// Drops the audit tables, if present. Intended for tests; use with care in
    /// production.
    /// </summary>
    public async Task DropTablesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var schemaPrefix = string.IsNullOrWhiteSpace(_options.Schema)
            ? string.Empty
            : $"{_options.Schema}.";

        await context.Database
            .ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {schemaPrefix}{_options.DetailTableName}", cancellationToken)
            .ConfigureAwait(false);

        await context.Database
            .ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {schemaPrefix}{_options.HeaderTableName}", cancellationToken)
            .ConfigureAwait(false);
    }
}
