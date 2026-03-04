using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Infrastructure.Configurations;
using HospitalSurgical.Infrastructure.Interceptors;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Infrastructure.Data;

/// <summary>
/// Main DbContext for the Surgical Management System.
///
/// Features integrated:
/// - Step 1: TPH inheritance for Staff
/// - Step 3: Owned Types for ContactInfo/Address
/// - Step 4: Global Query Filters for soft delete
/// - Step 5: Shadow Properties via AuditInterceptor
/// - Step 6: Concurrency tokens on Surgery and Patient
/// </summary>
public class SurgicalDbContext : DbContext
{
    public SurgicalDbContext(DbContextOptions<SurgicalDbContext> options)
        : base(options)
    {
    }

    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Surgeon> Surgeons => Set<Surgeon>();
    public DbSet<Nurse> Nurses => Set<Nurse>();
    public DbSet<AdministrativeStaff> AdministrativeStaff => Set<AdministrativeStaff>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<OperatingRoom> OperatingRooms => Set<OperatingRoom>();
    public DbSet<Surgery> Surgeries => Set<Surgery>();
    public DbSet<SurgeryNurse> SurgeryNurses => Set<SurgeryNurse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new StaffConfiguration());
        modelBuilder.ApplyConfiguration(new SurgeonConfiguration());
        modelBuilder.ApplyConfiguration(new NurseConfiguration());
        modelBuilder.ApplyConfiguration(new SurgeryConfiguration());
        modelBuilder.ApplyConfiguration(new SurgeryNurseConfiguration());
        modelBuilder.ApplyConfiguration(new PatientConfiguration());
        modelBuilder.ApplyConfiguration(new OperatingRoomConfiguration());
    }

    /// <summary>
    /// Step 6: Automatically refreshes ConcurrencyStamp on every update.
    /// Step 5: AuditInterceptor handles shadow property updates (CreatedAt, etc.).
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Refresh concurrency stamps on modified entities
        foreach (var entry in ChangeTracker.Entries<Surgery>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.ConcurrencyStamp = Guid.NewGuid().ToString();
        }

        foreach (var entry in ChangeTracker.Entries<Patient>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.ConcurrencyStamp = Guid.NewGuid().ToString();
        }

        foreach (var entry in ChangeTracker.Entries<Staff>()
            .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.ConcurrencyStamp = Guid.NewGuid().ToString();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}