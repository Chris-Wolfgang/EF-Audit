using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Wolfgang.Audit;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Example.AdventureWorks;
using Wolfgang.Audit.Serializers;

// AdventureWorks demo
// -------------------
// Spins up a SQL Server container, restores the canonical AdventureWorks2022
// sample database into it, then mutates a few realistic rows (rename a
// salesperson, update an email, delete a contact). Prints the resulting audit
// history so you can see exactly what the library captures against a real
// production-style schema.
//
// Requires Docker. Run from the repo root:
//   dotnet run --project examples/Wolfgang.Audit.EFCore.Example.AdventureWorks
//
// First run takes ~60-90 sec because it pulls and restores the AdventureWorks
// .bak. Subsequent runs reuse the cached image so they're much faster.

Console.WriteLine("📦 Starting SQL Server container + restoring AdventureWorks2022...");
var container = new MsSqlBuilder()
    .WithImage("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04")
    .Build();
await container.StartAsync();
await RestoreAdventureWorksAsync(container);

var connStr = container.GetConnectionString().Replace("master", "AdventureWorks2022", StringComparison.Ordinal);

var auditOptions = new AuditOptions
{
    Schema = "Audit",                      // keeps audit tables out of AdventureWorks's own schemas
    ValueSerializer = new StringAuditValueSerializer(),
    EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
};
IAuditUserProvider userProvider = new StaticAuditUserProvider("hr-admin@adventure-works.com");

var dbOptions = new DbContextOptionsBuilder<AdventureWorksContext>()
    .UseSqlServer(connStr)
    .Options;

// One-time: create the audit tables.
await using (var setup = new AdventureWorksContext(dbOptions, userProvider, auditOptions))
{
    await CreateAuditTablesAsync(setup);
}

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

// 2. Update — change the email address for that same person.
Console.WriteLine();
Console.WriteLine("📧  Updating their email...");
await using (var ctx = new AdventureWorksContext(dbOptions, userProvider, auditOptions))
{
    var email = await ctx.EmailAddresses.FirstAsync(e => e.BusinessEntityID == 1);
    Console.WriteLine($"  Before: {email.EmailAddress1}");
    email.EmailAddress1 = "ken.sanchez-smith@adventure-works.com";
    await ctx.SaveChangesAsync();
    Console.WriteLine($"  After:  {email.EmailAddress1}");
}

// 3. Insert + immediate delete — onboard, then offboard, a contractor.
Console.WriteLine();
Console.WriteLine("👤  Onboarding then immediately offboarding a contractor...");
int newPersonId;
await using (var ctx = new AdventureWorksContext(dbOptions, userProvider, auditOptions))
{
    // Pick an unused BusinessEntityID. AdventureWorks's max is ~20000 — using 99999 here.
    var contractor = new Person
    {
        BusinessEntityID = 99999,
        PersonType = "GC",                         // General Contact
        FirstName = "Temp",
        LastName = "Contractor",
    };
    ctx.People.Add(contractor);
    await ctx.SaveChangesAsync();
    newPersonId = contractor.BusinessEntityID;
    Console.WriteLine($"  Onboarded BusinessEntityID={newPersonId}");

    ctx.People.Remove(contractor);
    await ctx.SaveChangesAsync();
    Console.WriteLine($"  Offboarded BusinessEntityID={newPersonId}");
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

await container.DisposeAsync();



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

    var restore = await container.ExecAsync(new[]
    {
        "/opt/mssql-tools18/bin/sqlcmd",
        "-S", "localhost",
        "-U", "sa",
        "-P", "yourStrong(!)Password",
        "-No",
        "-Q", sql,
    });
    if (restore.ExitCode != 0)
    {
        throw new InvalidOperationException($"AdventureWorks restore failed: {restore.Stderr}");
    }
}



static async Task CreateAuditTablesAsync(AdventureWorksContext context)
{
    // Use the consumer's preferred installer. For brevity we go through the
    // RelationalModelCreator EnsureCreated() path here, scoped to just the
    // audit tables. In production you'd call AuditSchemaInstaller.CreateTablesAsync.
    await context.Database.ExecuteSqlRawAsync("IF SCHEMA_ID('Audit') IS NULL EXEC('CREATE SCHEMA [Audit]')");
    var creator = (IRelationalDatabaseCreator)context.GetInfrastructure().GetRequiredService<IDatabaseCreator>();
    await creator.CreateTablesAsync();
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
