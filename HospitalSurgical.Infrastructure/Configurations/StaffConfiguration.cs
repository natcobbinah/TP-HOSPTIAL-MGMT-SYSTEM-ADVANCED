using HospitalSurgical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalSurgical.Infrastructure.Configurations;

/// <summary>
/// Configures TPH inheritance for all Staff types.
/// All staff stored in a single "Staff" table with discriminator column.
/// Shadow properties for audit trail configured here.
/// Global Query Filter for soft delete applied here.
/// </summary>
public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("Staff");
        builder.HasKey(s => s.Id);

        // Step 1: TPH discriminator
        builder.HasDiscriminator<string>("StaffType")
            .HasValue<Surgeon>("Surgeon")
            .HasValue<Nurse>("Nurse")
            .HasValue<AdministrativeStaff>("Administrative");

        builder.Property(s => s.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.LastName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.Salary).HasColumnType("decimal(18,2)");

        builder.Property(s => s.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(36);

        // Step 5: Shadow Properties for audit trail
        // These columns exist in the DB but NOT as C# properties on the entity
        builder.Property<DateTime>("CreatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<DateTime>("UpdatedAt")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<string>("CreatedBy")
            .HasMaxLength(100)
            .HasDefaultValue("System");
        builder.Property<string>("UpdatedBy")
            .HasMaxLength(100)
            .HasDefaultValue("System");

        // Step 4: Global Query Filter — automatically excludes deleted staff
        // from ALL queries unless explicitly overridden with IgnoreQueryFilters()
        builder.HasQueryFilter(s => !s.IsDeleted);

        // Index for active staff lookup
        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_Staff_IsActive");
    }
}

// public class SurgeonConfiguration : IEntityTypeConfiguration<Surgeon>
// {
//     public void Configure(EntityTypeBuilder<Surgeon> builder)
//     {
//         builder.Property(s => s.LicenseNumber).IsRequired().HasMaxLength(50);
//         builder.Property(s => s.Specialty).IsRequired().HasMaxLength(100);

//         builder.HasIndex(s => s.LicenseNumber)
//             .IsUnique()
//             .HasDatabaseName("IX_Surgeon_LicenseNumber");
//     }
// }

// public class NurseConfiguration : IEntityTypeConfiguration<Nurse>
// {
//     public void Configure(EntityTypeBuilder<Nurse> builder)
//     {
//         builder.Property(n => n.CertificationLevel)
//             .HasConversion<string>()
//             .HasMaxLength(20);

//         builder.Property(n => n.ShiftPreference)
//             .HasConversion<string>()
//             .HasMaxLength(20);
//     }
// }