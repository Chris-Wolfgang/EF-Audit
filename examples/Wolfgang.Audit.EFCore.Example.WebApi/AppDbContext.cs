using Microsoft.EntityFrameworkCore;

namespace Wolfgang.Audit.Example.WebApi;

public class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public decimal Price { get; set; }
}

public class AppDbContext : DbContext
{
    private readonly AuditOptions _auditOptions;

    public AppDbContext(DbContextOptions<AppDbContext> options, AuditOptions auditOptions)
        : base(options)
    {
        _auditOptions = auditOptions;
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyAuditing(_auditOptions);
    }
}
