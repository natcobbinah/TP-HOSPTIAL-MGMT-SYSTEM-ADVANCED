using HospitalSurgical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalSurgical.Infrastructure.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the Nurse entity.
///
/// IMPORTANT: Nurse uses TPH inheritance — it shares the "Staff" table.
/// This configuration ONLY adds Nurse-specific column constraints
/// on top of the base StaffConfiguration.
///
/// The discriminator value "Nurse" is set in StaffConfiguration.
/// Nurse-specific columns (Service, Grade, CertificationLevel, ShiftPreference)
/// are nullable in the DB because they don't apply to Surgeon or Administrative rows.
///
/// Step 4: Soft delete Global Query Filter is inherited from StaffConfiguration.
/// Step 5: Shadow properties (CreatedAt, etc.) are inherited from StaffConfiguration.
/// </summary>
public class NurseConfiguration : IEntityTypeConfiguration<Nurse>
{
    public void Configure(EntityTypeBuilder<Nurse> builder)
    {
        // CertificationLevel stored as string for readability in SQLite
        // Generated SQL: CertificationLevel TEXT NULL CHECK(CertificationLevel IN ('Junior',...))
        builder.Property(n => n.CertificationLevel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("CertificationLevel");

        // ShiftPreference stored as string for readability
        builder.Property(n => n.ShiftPreference)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasColumnName("ShiftPreference");

        // DepartmentId is nullable — a nurse may not be assigned to a specific department
        // (they are assigned to surgeries directly via SurgeryNurse)
        builder.Property(n => n.DepartmentId)
            .IsRequired(false)
            .HasColumnName("DepartmentId");

        // Index: find all nurses in a department efficiently
        builder.HasIndex(n => n.DepartmentId)
            .HasDatabaseName("IX_Nurse_DepartmentId")
            .HasFilter("[DepartmentId] IS NOT NULL"); // Partial index — skip nulls

        // Index: find available nurses by shift preference for scheduling
        builder.HasIndex(n => new { n.ShiftPreference, n.IsActive })
            .HasDatabaseName("IX_Nurse_Shift_Active");

        // Index: find nurses by certification level for surgery requirements
        builder.HasIndex(n => n.CertificationLevel)
            .HasDatabaseName("IX_Nurse_CertificationLevel");
    }
}