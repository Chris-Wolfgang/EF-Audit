using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit;
using Wolfgang.Audit.Entities;
using Wolfgang.Audit.Example.WebApi;
using Wolfgang.Audit.Serializers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(new AuditOptions
{
    ValueSerializer = new StringAuditValueSerializer(),
    EntityKeySerializer = new PipeDelimitedEntityKeySerializer(),
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IAuditUserProvider, HttpContextAuditUserProvider>();

builder.Services.AddScoped<AuditSaveChangesInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    options
        .UseSqlite("DataSource=audit-webapi-example.db")
        .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await ctx.Database.EnsureCreatedAsync();
}

// Add a product. The acting human's identity (HTTP header X-User) is captured as
// OnBehalfOfUserId; the running service account ("svc-orders") is captured as UserId.
app.MapPost("/products", async (Product product, AppDbContext ctx) =>
{
    ctx.Products.Add(product);
    await ctx.SaveChangesAsync();
    return Results.Created($"/products/{product.ProductId}", product);
});

// Read the full audit history.
app.MapGet("/audit", async (AppDbContext ctx) =>
    await ctx.Set<AuditHeader>()
        .Include(h => h.Details)
        .OrderBy(h => h.AuditedAtUtc)
        .ToListAsync());

await app.RunAsync();
