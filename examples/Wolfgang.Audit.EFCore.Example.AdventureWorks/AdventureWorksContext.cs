using Microsoft.EntityFrameworkCore;
using Wolfgang.Audit;

namespace Wolfgang.Audit.Example.AdventureWorks;



/// <summary>
/// A trimmed slice of the AdventureWorks schema — just enough to demonstrate
/// audit capture against realistic data. We map only the columns we care to
/// audit; the AdventureWorks DB has many more columns the consumer's app could
/// expose, but for this demo the smaller surface keeps the audit output
/// readable.
/// </summary>
public class AdventureWorksContext : AuditingDbContext
{
    public AdventureWorksContext
    (
        DbContextOptions<AdventureWorksContext> options,
        IAuditUserProvider userProvider,
        AuditOptions auditOptions
    )
        : base(options, userProvider, auditOptions)
    {
    }



    public DbSet<Person> People => Set<Person>();

    public DbSet<EmailAddress> EmailAddresses => Set<EmailAddress>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<Person>(b =>
        {
            b.ToTable("Person", "Person");
            b.HasKey(p => p.BusinessEntityID);
            b.Property(p => p.PersonType).HasMaxLength(2).IsRequired();
            b.Property(p => p.FirstName).HasMaxLength(50).IsRequired();
            b.Property(p => p.MiddleName).HasMaxLength(50);
            b.Property(p => p.LastName).HasMaxLength(50).IsRequired();
            b.Ignore(p => p.AdditionalContactInfo); // XML column — out of scope for this demo
        });

        modelBuilder.Entity<EmailAddress>(b =>
        {
            b.ToTable("EmailAddress", "Person");
            b.HasKey(e => new { e.BusinessEntityID, e.EmailAddressID });
            b.Property(e => e.EmailAddress1)
                .HasColumnName("EmailAddress")
                .HasMaxLength(50);
        });

        base.OnModelCreating(modelBuilder); // applies AuditHeader / AuditDetail mappings
    }
}



public class Person
{
    public int BusinessEntityID { get; set; }

    public string PersonType { get; set; } = "EM";

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string? AdditionalContactInfo { get; set; }
}



public class EmailAddress
{
    public int BusinessEntityID { get; set; }

    public int EmailAddressID { get; set; }

    public string? EmailAddress1 { get; set; }
}
