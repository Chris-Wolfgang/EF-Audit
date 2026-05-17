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
/// non-migration path (prototypes, self-contained desktop apps, tests).
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
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong> this delegates to
    /// <see cref="Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade.EnsureCreatedAsync"/>,
    /// which creates <em>every</em> table in the supplied context's model — not just the
    /// audit tables. That's the right behavior for a dedicated audit context, a fresh
    /// in-memory test database, or a self-contained desktop app. For shared contexts
    /// that already manage their own schema via EF Core Migrations, do NOT call this —
    /// let the consumer's migration pipeline handle the audit tables alongside its own.
    /// </para>
    /// </remarks>
    public Task CreateTablesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.Database.EnsureCreatedAsync(cancellationToken);
    }

    /// <summary>
    /// Drops the audit tables, if present. Intended for tests; use with care in
    /// production.
    /// </summary>
    /// <exception cref="ArgumentNullException">If <paramref name="context"/> is null.</exception>
    /// <exception cref="ArgumentException">If the configured schema or table names are null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">If the configured schema or table names contain characters that would be unsafe to interpolate into raw SQL.</exception>
    public async Task DropTablesAsync(DbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var schemaPrefix = string.IsNullOrWhiteSpace(_options.Schema)
            ? string.Empty
            : $"{EnsureSafeIdentifier(_options.Schema, nameof(_options.Schema))}.";

        var detailTable = EnsureSafeIdentifier(_options.DetailTableName, nameof(_options.DetailTableName));
        var headerTable = EnsureSafeIdentifier(_options.HeaderTableName, nameof(_options.HeaderTableName));

#pragma warning disable EF1002 // Schema and table names are validated above; values come from AuditOptions, not user input.
        await context.Database
            .ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {schemaPrefix}{detailTable}", cancellationToken)
            .ConfigureAwait(false);

        await context.Database
            .ExecuteSqlRawAsync($"DROP TABLE IF EXISTS {schemaPrefix}{headerTable}", cancellationToken)
            .ConfigureAwait(false);
#pragma warning restore EF1002
    }

    /// <summary>
    /// Validates that an identifier consists only of letters, digits, and underscores,
    /// and does not start with a digit. This is the conservative intersection of SQL
    /// Server / PostgreSQL / MySQL / SQLite identifier syntax — anything more permissive
    /// would require provider-specific quoting (brackets, double-quotes, backticks).
    /// Throws if the identifier fails the check, so a malicious or misconfigured value
    /// cannot end up interpolated into a <c>DROP TABLE</c> statement.
    /// </summary>
    /// <exception cref="ArgumentException">If <paramref name="identifier"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">If <paramref name="identifier"/> contains characters outside [A-Za-z0-9_] or starts with a digit.</exception>
    private static string EnsureSafeIdentifier(string identifier, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException(
                "Identifier cannot be null or whitespace.",
                parameterName);
        }

        if (char.IsDigit(identifier[0]))
        {
            throw new InvalidOperationException(
                $"Invalid SQL identifier '{identifier}' supplied via {parameterName}: identifiers cannot start with a digit.");
        }

        foreach (var ch in identifier)
        {
            if (!char.IsLetterOrDigit(ch) && ch != '_')
            {
                throw new InvalidOperationException(
                    $"Invalid SQL identifier '{identifier}' supplied via {parameterName}: identifiers may contain only letters, digits, and underscores.");
            }
        }

        return identifier;
    }
}
