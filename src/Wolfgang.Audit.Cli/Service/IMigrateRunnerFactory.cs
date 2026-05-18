using Wolfgang.Audit.Cli.Model;

namespace Wolfgang.Audit.Cli.Service;



/// <summary>
/// Resolves an <see cref="IMigrateRunner"/> for the requested
/// <see cref="DatabaseProvider"/>. Lets the CLI register a real runner per
/// provider (e.g. <see cref="SqlServerMigrateRunner"/>) and fall back to
/// <see cref="StubMigrateRunner"/> for providers whose migrations package
/// has not yet shipped.
/// </summary>
internal interface IMigrateRunnerFactory
{
    IMigrateRunner Create(DatabaseProvider provider);
}
