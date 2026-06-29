using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolfgang.AuditTrail;
using Wolfgang.AuditTrail.Entities;
using Wolfgang.AuditTrail.Example.WebApi;
using Wolfgang.AuditTrail.Serializers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new AuditOptions
{
    ValueSerializer = new StringAuditValueSerializer(),
    EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditUserProvider, HttpContextAuditUserProvider>();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite("DataSource=audit-webapi-example.db");
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await ctx.Database.EnsureCreatedAsync();
}

// Add a product. The acting human's identity (HTTP header X-User) is captured as
// OnBehalfOfUserId; the running service account ("svc-orders") is captured as UserId.
// AppDbContext derives from AuditingDbContext so plain SaveChangesAsync writes the
// audit rows atomically in the same transaction.
app.MapPost("/products", async (Product product, AppDbContext ctx) =>
{
    ctx.Products.Add(product);
    await ctx.SaveChangesAsync();
    return Results.Created($"/products/{product.ProductId}", product);
});

// Read the full audit history. Project to an anonymous shape (no Header back-reference
// on details) so System.Text.Json doesn't hit the header<->detail reference cycle that
// EF's relationship fixup creates on tracked queries. AsNoTracking() also skips the
// change-tracker overhead since this endpoint is read-only.
app.MapGet("/audit", async (AppDbContext ctx) =>
    await ctx.Set<AuditHeader>()
        .AsNoTracking()
        .OrderBy(h => h.AuditedAtUtc)
        .Select(h => new
        {
            h.HeaderId,
            h.TransactionId,
            h.AuditedAtUtc,
            h.UserId,
            h.OnBehalfOfUserId,
            h.EntityType,
            h.EntityTable,
            h.EntityKey,
            h.Operation,
            Details = h.Details.Select(d => new
            {
                d.DetailId,
                d.ColumnName,
                d.ValueText,
                d.ValueType,
            }).ToList(),
        })
        .ToListAsync());

await app.RunAsync();
