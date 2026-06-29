namespace Wolfgang.AuditTrail.Cli.Model;



/// <summary>
/// Database engine families the CLI knows how to target. One <c>Wolfgang.AuditTrail.EntityFrameworkCore.Migrations.*</c>
/// package per non-unknown value is expected.
/// </summary>
internal enum DatabaseProvider
{
    Unknown = 0,
    SqlServer,
    PostgreSql,
    MySql,
    Sqlite,
}
