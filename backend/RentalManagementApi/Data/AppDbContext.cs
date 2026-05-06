using Microsoft.EntityFrameworkCore;
using RentalManagementApi.Entities;

namespace RentalManagementApi.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<TenantProfile> TenantProfiles => Set<TenantProfile>();
    public DbSet<TenantInvite> TenantInvites => Set<TenantInvite>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyInventory> PropertyInventory => Set<PropertyInventory>();
    public DbSet<Lease> Leases => Set<Lease>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).ValueGeneratedNever();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired().HasMaxLength(320);
            e.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
            e.Property(u => u.LastName).IsRequired().HasMaxLength(100);
            e.Property(u => u.Phone).HasMaxLength(30);
            e.Property(u => u.Role).IsRequired().HasMaxLength(20);
        });

        modelBuilder.Entity<TenantProfile>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasOne(p => p.User)
             .WithOne(u => u.TenantProfile)
             .HasForeignKey<TenantProfile>(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(p => p.UserId).IsUnique();
        });

        modelBuilder.Entity<TenantInvite>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => i.Token).IsUnique();
            e.HasIndex(i => i.Email);
            e.Property(i => i.Email).IsRequired().HasMaxLength(320);
            e.Property(i => i.Token).IsRequired().HasMaxLength(128);
            e.HasOne(i => i.InvitedBy)
             .WithMany()
             .HasForeignKey(i => i.InvitedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Property>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(200);
            e.Property(p => p.Address).IsRequired().HasMaxLength(500);
            e.Property(p => p.Type).HasConversion<string>();
            e.HasIndex(p => p.LandlordId);
            e.HasOne(p => p.Landlord)
             .WithMany()
             .HasForeignKey(p => p.LandlordId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PropertyInventory>(e =>
        {
            e.HasKey(i => i.Id);
            e.Property(i => i.Name).IsRequired().HasMaxLength(200);
            e.Property(i => i.Condition).HasConversion<string>();
            e.HasIndex(i => i.PropertyId);
            e.HasOne(i => i.Property)
             .WithMany(p => p.Inventory)
             .HasForeignKey(i => i.PropertyId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lease>(e =>
        {
            e.HasKey(l => l.Id);
            e.Property(l => l.MonthlyRent).HasColumnType("decimal(18,2)");
            e.Property(l => l.DepositAmount).HasColumnType("decimal(18,2)");
            e.Property(l => l.AdvanceAmount).HasColumnType("decimal(18,2)");
            e.Property(l => l.Status).HasConversion<string>();
            e.HasIndex(l => l.PropertyId);
            e.HasIndex(l => l.TenantId);
            e.HasOne(l => l.Property)
             .WithMany(p => p.Leases)
             .HasForeignKey(l => l.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(l => l.Tenant)
             .WithMany()
             .HasForeignKey(l => l.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.AmountDue).HasColumnType("decimal(18,2)");
            e.Property(p => p.AmountPaid).HasColumnType("decimal(18,2)");
            e.Property(p => p.Status).HasConversion<string>();
            e.HasIndex(p => p.LeaseId);
            e.HasIndex(p => p.DueDate);
            e.HasIndex(p => p.Status);
            e.HasOne(p => p.Lease)
             .WithMany(l => l.Payments)
             .HasForeignKey(p => p.LeaseId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
