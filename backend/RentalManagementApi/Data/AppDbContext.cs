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
    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
    public DbSet<ReminderSetting> ReminderSettings => Set<ReminderSetting>();
    public DbSet<ReminderLog> ReminderLogs => Set<ReminderLog>();
    public DbSet<ProvisionTemplate> ProvisionTemplates => Set<ProvisionTemplate>();
    public DbSet<LeaseProvision> LeaseProvisions => Set<LeaseProvision>();
    public DbSet<AppLog> AppLogs => Set<AppLog>();

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

        modelBuilder.Entity<ReminderSetting>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.LeaseId).IsUnique();
            e.HasOne(r => r.Lease)
             .WithOne()
             .HasForeignKey<ReminderSetting>(r => r.LeaseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReminderLog>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => new { r.LeaseId, r.DueDate, r.ReminderType, r.SentDate }).IsUnique();
            e.Property(r => r.ReminderType).IsRequired().HasMaxLength(10);
            e.HasOne(r => r.Lease)
             .WithMany()
             .HasForeignKey(r => r.LeaseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MaintenanceRequest>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Title).IsRequired().HasMaxLength(200);
            e.Property(m => m.Description).IsRequired().HasMaxLength(2000);
            e.Property(m => m.Priority).HasConversion<string>();
            e.Property(m => m.Status).HasConversion<string>();
            e.HasIndex(m => m.PropertyId);
            e.HasIndex(m => m.TenantId);
            e.HasIndex(m => m.Status);
            e.HasOne(m => m.Property)
             .WithMany()
             .HasForeignKey(m => m.PropertyId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Tenant)
             .WithMany()
             .HasForeignKey(m => m.TenantId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProvisionTemplate>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Title).IsRequired().HasMaxLength(200);
            e.Property(t => t.Body).IsRequired().HasMaxLength(4000);
            e.HasIndex(t => t.LandlordId);
            e.HasOne(t => t.Landlord)
             .WithMany()
             .HasForeignKey(t => t.LandlordId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<LeaseProvision>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Title).IsRequired().HasMaxLength(200);
            e.Property(p => p.Body).IsRequired().HasMaxLength(4000);
            e.HasIndex(p => p.LeaseId);
            e.HasOne(p => p.Lease)
             .WithMany(l => l.Provisions)
             .HasForeignKey(p => p.LeaseId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Action).IsRequired().HasMaxLength(100);
            e.Property(a => a.EntityType).IsRequired().HasMaxLength(50);
            e.Property(a => a.Description).IsRequired().HasMaxLength(500);
            e.HasIndex(a => new { a.UserId, a.CreatedAt });
            e.HasOne(a => a.User)
             .WithMany()
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
