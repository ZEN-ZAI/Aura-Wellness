using AuraWellness.Domain.Entities;
using AuraWellness.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuraWellness.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<BusinessUnit> BusinessUnits => Set<BusinessUnit>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<BuStaffProfile> BuStaffProfiles => Set<BuStaffProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Company>(e =>
        {
            e.ToTable("companies");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            e.Property(c => c.Address).HasColumnName("address").IsRequired();
            e.Property(c => c.ContactNumber).HasColumnName("contact_number").HasMaxLength(50).IsRequired();
            e.Property(c => c.CreatedAt).HasColumnName("created_at");
            e.HasMany(c => c.BusinessUnits).WithOne(b => b.Company).HasForeignKey(b => b.CompanyId).OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Persons).WithOne(p => p.Company).HasForeignKey(p => p.CompanyId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BusinessUnit>(e =>
        {
            e.ToTable("business_units");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id");
            e.Property(b => b.CompanyId).HasColumnName("company_id");
            e.Property(b => b.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            e.Property(b => b.CreatedAt).HasColumnName("created_at");
            e.HasIndex(b => b.CompanyId);
            e.HasMany(b => b.StaffProfiles).WithOne(p => p.BusinessUnit).HasForeignKey(p => p.BuId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Person>(e =>
        {
            e.ToTable("persons");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.CompanyId).HasColumnName("company_id");
            e.Property(p => p.FirstName).HasColumnName("first_name").HasMaxLength(100).IsRequired();
            e.Property(p => p.LastName).HasColumnName("last_name").HasMaxLength(100).IsRequired();
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.HasIndex(p => p.CompanyId);
            e.HasMany(p => p.BuProfiles).WithOne(pr => pr.Person).HasForeignKey(pr => pr.PersonId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BuStaffProfile>(e =>
        {
            e.ToTable("bu_staff_profiles");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.PersonId).HasColumnName("person_id");
            e.Property(p => p.BuId).HasColumnName("bu_id");
            e.Property(p => p.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.Property(p => p.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(p => p.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
            e.Property(p => p.CreatedAt).HasColumnName("created_at");
            e.HasIndex(p => new { p.BuId, p.Email }).IsUnique();
            e.HasIndex(p => p.BuId);
            e.HasIndex(p => p.Email);
        });
    }
}
