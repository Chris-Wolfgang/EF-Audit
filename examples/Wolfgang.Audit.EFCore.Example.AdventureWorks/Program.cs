using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Wolfgang.Audit;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Example.AdventureWorks;
using Wolfgang.Audit.Serializers;

// AdventureWorks demo
// -------------------
// Spins up a SQL Server container, restores the canonical AdventureWorks2022
// sample database into it, then mutates a few realistic rows (rename a
// salesperson, update an email, insert + delete a secondary email). Prints
// the resulting audit history so you can see exactly what the library captures
// against a real production-style schema.
//
// Requires Docker. Run from the repo root:
//   dotnet run --project examples/Wolfgang.Audit.EFCore.Example.AdventureWorks
//
// First run takes ~60-90 sec because it pulls and restores the AdventureWorks
// .bak. Subsequent runs reuse the cached image so they're much faster.

Console.WriteLine("📦 Starting SQL Server container + restoring AdventureWorks2022...");

await using var container = new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
    .Build();
await container.StartAsync();
await RestoreAdventureWorksAsync(container);

// Switch InitialCatalog to AdventureWorks2022 via SqlConnectionStringBuilder
// rather than string.Replace — safer (no risk of replacing substrings inside
// the password or other values) and clearer about intent.
var connStr = new SqlConnectionStringBuilder(container.GetConnectionString())
{
    InitialCatalog = "AdventureWorks2022",
}.ConnectionString;

var auditOptions = new AuditOptions
{
    Schema              = "Audit",          // keeps audit tables out of AdventureWorks's own schemas
    ValueSerializer     = new StringAuditValueSerializer(),
    EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
};
IAuditUserProvider userProvider = new StaticAuditUserProvider("hr-admin@adventure-works.com");

var dbOptions = new DbContextOptionsBuilder<AdventureWorksContext>()
    .UseSqlServer(connStr)
    .Options;

// One-time: create the audit tables (and only the audit tables).
await CreateAuditTablesAsync(connStr, auditOptions);

// 1. Update — a salesperson gets married and changes her last name.
Console.WriteLine();
Console.WriteLine("✏️  Renaming an employee...");
await using (var ctx = new AdventureWorksContext(dbOptions, userProvider, auditOptions))
{
    var ken = await ctx.People.SingleAsync(p => p.BusinessEntityID == 1);
    Console.WriteLine($"  Before: {ken.FirstName} {ken.MiddleName} {ken.LastName} (type={ken.PersonType})");
    ken.LastName = "Sánchez-Smith";
    await ctx.SaveChangesAsync();
    Console.WriteLine($"  After:  {ken.FirstName} {ken.MiddleName} {ken.LastName}");
}

// 2. Update — change the primary email for that same person.
Console.WriteLine();
Console.WriteLine("📧  Updating their primary email...");
await using (var ctx = new AdventureWorksContext(dbOptions, userProvider, auditOptions))
{
    var email = await ctx.EmailAddresses.FirstAsync(e => e.BusinessEntityID == 1);
    Console.WriteLine($"  Before: {email.EmailAddress1}");
    email.EmailAddress1 = "ken.sanchez-smith@adventure-works.com";
    await ctx.SaveChangesAsync();
    Console.WriteLine($"  After:  {email.EmailAddress1}");
}

// 3. Insert + immediate delete — add then remove a secondary email for that
// same person. We use EmailAddress rather than Person because EmailAddress's
// only FK is to an existing BusinessEntity (Person 1 already exists), so the
// insert won't trip AdventureWorks's FK constraints. EmailAddressID is an
// identity column in the real schema (configured in OnModelCreating), so SQL
// Server assigns the key on insert.
Console.WriteLine();
Console.WriteLine("➕  Adding then immediately removing a secondary email...");
await using (var ctx = new AdventureWorksContext(dbOptions, userProvider, auditOptions))
{
    var secondary = new EmailAddress
    {
        BusinessEntityID = 1,
        EmailAddress1    = "ken.alt@adventure-works.com",
    };
    ctx.EmailAddresses.Add(secondary);
    await ctx.SaveChangesAsync();
    Console.WriteLine($"  Added secondary email id={secondary.EmailAddressID}");

    ctx.EmailAddresses.Remove(secondary);
    await ctx.SaveChangesAsync();
    Console.WriteLine($"  Removed secondary email id={secondary.EmailAddressID}");
}

// 4. Print the resulting audit history for those rows.
Console.WriteLine();
Console.WriteLine("📜 Audit history for affected rows:");
int auditRowCount;
await using (var ctx = new AdventureWorksContext(dbOptions, userProvider, auditOptions))
{
    var headers = await ctx.Set<AuditHeader>()
        .Include(h => h.Details)
        .OrderBy(h => h.AuditedAtUtc)
        .ThenBy(h => h.HeaderId)
        .ToListAsync();

    foreach (var h in headers)
    {
        var operationName = h.Operation switch
        {
            AuditOperation.Insert => "INSERT",
            AuditOperation.Update => "UPDATE",
            AuditOperation.Delete => "DELETE",
            _ => h.Operation.ToString(),
        };

        Console.WriteLine();
        Console.WriteLine($"  [{h.AuditedAtUtc:u}] {operationName} on {h.EntityType.Split('.')[^1]} key={h.EntityKey} by {h.UserId}");
        foreach (var d in h.Details.OrderBy(d => d.ColumnName, StringComparer.Ordinal))
        {
            Console.WriteLine($"      {d.ColumnName} = {d.ValueText ?? "<null>"}  ({d.ValueType})");
        }
        if (h.Details.Count == 0)
        {
            Console.WriteLine("      (no detail rows — CaptureDeletedValues=false)");
        }
    }

    auditRowCount = headers.Count;
}

Console.WriteLine();
Console.WriteLine($"✅  Done — {auditRowCount} audit rows captured atomically with the user data.");

// container is disposed via `await using` at top — DisposeAsync runs even on
// exception, keeping the SQL Server container from leaking.



// ----------------------------------------------------------------------------

static async Task RestoreAdventureWorksAsync(MsSqlContainer container)
{
    // AdventureWorks2022 .bak is published as a release asset on the SQL Server
    // samples repo. We fetch it inside the container, then RESTORE.
    var bakUrl = "https://github.com/Microsoft/sql-server-samples/releases/download/adventureworks/AdventureWorks2022.bak";
    var bakPath = "/var/opt/mssql/backup/AdventureWorks2022.bak";

    var prep = await container.ExecAsync(new[]
    {
        "bash",
        "-c",
        $"mkdir -p /var/opt/mssql/backup && curl -fsSL '{bakUrl}' -o '{bakPath}'",
    });
    if (prep.ExitCode != 0)
    {
        throw new InvalidOperationException($"Failed to download AdventureWorks .bak: {prep.Stderr}");
    }

    // RESTORE DATABASE ... WITH MOVE so the MDF/LDF land where SQL Server expects.
    var sql = $@"
        RESTORE DATABASE [AdventureWorks2022]
          FROM DISK = '{bakPath}'
          WITH MOVE 'AdventureWorks2022' TO '/var/opt/mssql/data/AdventureWorks2022.mdf',
               MOVE 'AdventureWorks2022_log' TO '/var/opt/mssql/data/AdventureWorks2022_log.ldf',
               REPLACE;
    ";

    // Pull the SA password out of the container's own connection string so it
    // stays in sync if Testcontainers changes its default and so no credential
    // literal sits in source for secret scanners to flag.
    var saPassword = new SqlConnectionStringBuilder(container.GetConnectionString()).Password;

    var restore = await container.ExecAsync(new[]
    {
        "/opt/mssql-tools18/bin/sqlcmd",
        "-S", "localhost",
        "-U", "sa",
        "-P", saPassword,
        "-No",
        "-Q", sql,
    });
    if (restore.ExitCode != 0)
    {
        throw new InvalidOperationException($"AdventureWorks restore failed: {restore.Stderr}");
    }
}



static async Task CreateAuditTablesAsync(string connectionString, AuditOptions options)
{
    // Use an audit-only DbContext so EnsureCreatedAsync emits CREATE TABLE for
    // *just* AuditHeader / AuditDetail. Running this against AdventureWorksContext
    // (which also has Person + EmailAddress) would try to create those tables
    // too and fail because the restored .bak already has them.
    //
    // The schema is created up-front because EnsureCreatedAsync won't create the
    // schema itself, only the tables under it. The schema name is read from the
    // supplied options.Schema so it stays in sync with whatever the consumer
    // configured rather than being hard-coded here.
    var schema   = options.Schema ?? "dbo";
    var setupOpts = new DbContextOptionsBuilder<AuditOnlyContext>()
        .UseSqlServer(connectionString)
        .Options;

    await using var setup = new AuditOnlyContext(setupOpts, options);
#pragma warning disable EF1002 // Schema name comes from in-process AuditOptions, not user input.
    await setup.Database.ExecuteSqlRawAsync(
        $"IF SCHEMA_ID('{schema}') IS NULL EXEC('CREATE SCHEMA [{schema}]')");
#pragma warning restore EF1002
    await setup.Database.EnsureCreatedAsync();
}



namespace Wolfgang.Audit.Example.AdventureWorks
{
    public sealed class StaticAuditUserProvider : IAuditUserProvider
    {
        private readonly AuditUser _user;
        public StaticAuditUserProvider(string userId) => _user = new AuditUser(userId);
        public AuditUser GetCurrentUser() => _user;
    }
}
