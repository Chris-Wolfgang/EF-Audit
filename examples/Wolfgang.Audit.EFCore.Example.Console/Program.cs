using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Example.Console;
using Wolfgang.Audit.Serializers;

var auditOptions = new AuditOptions
{
    ValueSerializer = new StringAuditValueSerializer(),
    EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
};

var userProvider = new StaticAuditUserProvider("alice@example.com");

var contextOptions = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite("DataSource=audit-example.db")
    .Options;

await using (var setup = new AppDbContext(contextOptions, auditOptions))
{
    await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
}

await using (var ctx = new AppDbContext(contextOptions, auditOptions))
{
    ctx.Products.Add(new Product { Name = "Widget", Price = 9.99m });
    await ctx.SaveChangesWithAuditAsync(userProvider, auditOptions);

    var widget = await ctx.Products.SingleAsync();
    widget.Price = 12.49m;
    await ctx.SaveChangesWithAuditAsync(userProvider, auditOptions);
}

await using (var ctx = new AppDbContext(contextOptions, auditOptions))
{
    Console.WriteLine("Audit history:");
    var headers = await ctx.Set<AuditHeader>()
        .Include(h => h.Details)
        .OrderBy(h => h.AuditedAtUtc)
        .ToListAsync();

    foreach (var header in headers)
    {
        Console.WriteLine($"  [{header.AuditedAtUtc:u}] {header.Operation} {header.EntityType} key={header.EntityKey} by {header.UserId}");
        foreach (var detail in header.Details)
        {
            Console.WriteLine($"      {detail.ColumnName} = {detail.ValueText} ({detail.ValueType})");
        }
    }
}
