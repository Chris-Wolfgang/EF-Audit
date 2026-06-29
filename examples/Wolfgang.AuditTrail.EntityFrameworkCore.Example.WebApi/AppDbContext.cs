using Microsoft.EntityFrameworkCore;

namespace Wolfgang.AuditTrail.Example.WebApi;

public class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
}

public class AppDbContext : AuditingDbContext
{
    public AppDbContext
    (
        DbContextOptions<AppDbContext> options,
        IAuditUserProvider userProvider,
        AuditOptions auditOptions
    )
        : base(options, userProvider, auditOptions)
    {
    }



    public DbSet<Product> Products => Set<Product>();
}
