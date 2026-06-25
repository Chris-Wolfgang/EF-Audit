namespace Wolfgang.Audit.Cli.Model;



/// <summary>
/// Database engine families the CLI knows how to target. One <c>Wolfgang.Audit.EFCore.Migrations.*</c>
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
