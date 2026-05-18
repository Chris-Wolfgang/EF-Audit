using Wolfgang.Audit.Cli.Model;

namespace Wolfgang.Audit.Cli.Service;



/// <summary>
/// Default <see cref="IMigrateRunnerFactory"/>. SQL Server resolves to the real
/// <see cref="SqlServerMigrateRunner"/>; PostgreSQL / MySQL / SQLite fall back to
/// <see cref="StubMigrateRunner"/> until their migrations packages ship.
/// </summary>
internal sealed class MigrateRunnerFactory : IMigrateRunnerFactory
{
    private readonly SqlServerMigrateRunner _sqlServer;
    private readonly StubMigrateRunner _stub;



    public MigrateRunnerFactory(SqlServerMigrateRunner sqlServer, StubMigrateRunner stub)
    {
        _sqlServer = sqlServer ?? throw new ArgumentNullException(nameof(sqlServer));
        _stub      = stub      ?? throw new ArgumentNullException(nameof(stub));
    }



    public IMigrateRunner Create(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => _sqlServer,
        _                          => _stub,
    };
}
