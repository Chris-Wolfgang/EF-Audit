namespace Wolfgang.AuditTrail.Cli.Model;



/// <summary>
/// Normalized inputs for the <c>migrate</c> subcommand, after CLI option parsing and
/// environment-variable resolution. Passed to <see cref="Service.IMigrateRunner"/>.
/// </summary>
/// <param name="ConnectionString">ADO.NET connection string; never null/empty by this point.</param>
/// <param name="Provider">Detected or user-specified provider; never <see cref="DatabaseProvider.Unknown"/>.</param>
/// <param name="Schema">
/// Audit-table schema. <c>null</c> means "use the provider default" — the migrations
/// package will substitute <c>dbo</c> / <c>public</c> / etc. as appropriate.
/// </param>
/// <param name="HeaderTableName">Override for the audit-header table name.</param>
/// <param name="DetailTableName">Override for the audit-detail table name.</param>
/// <param name="DryRun">If <c>true</c>, print SQL to stdout instead of executing.</param>
internal sealed record MigrateOptions
(
    string ConnectionString,
    DatabaseProvider Provider,
    string? Schema,
    string HeaderTableName,
    string DetailTableName,
    bool DryRun
);
