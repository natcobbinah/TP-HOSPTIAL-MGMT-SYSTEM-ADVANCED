using HospitalSurgical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalSurgical.Infrastructure.Configurations;

/// <summary>
/// EF Core Fluent API configuration for the Surgeon entity.
///
/// IMPORTANT: Surgeon uses TPH inheritance — shares the "Staff" table.
/// This configuration adds Surgeon-specific column constraints only.
///
/// KEY CONSTRAINT: LicenseNumber must be globally unique across all surgeons.
/// This is enforced at the database level via a unique index, not just in
/// application code — even if validation is bypassed, the DB rejects duplicates.
///
/// Step 7: Indexes are critical here — GetSurgeonWithPlanningAsync is a
/// compiled query used in conflict detection (hot path). The index on
/// LicenseNumber and Specialty supports frequent lookup patterns.
/// </summary>
public class SurgeonConfiguration : IEntityTypeConfiguration<Surgeon>
{
    public void Configure(EntityTypeBuilder<Surgeon> builder)
    {
        // LicenseNumber: required, unique — medical license must be unique per surgeon
        builder.Property(s => s.LicenseNumber)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("LicenseNumber");

        // Unique index on LicenseNumber — database-level enforcement
        // If two surgeons attempt to register with the same license,
        // DbUpdateException is thrown and caught in StaffService.CreateSurgeonAsync
        builder.HasIndex(s => s.LicenseNumber)
            .IsUnique()
            .HasDatabaseName("IX_Surgeon_LicenseNumber");

        // Specialty: required, stored as string for readability
        builder.Property(s => s.Specialty)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("Specialty");

        // YearsOfExperience: non-negative integer
        builder.Property(s => s.YearsOfExperience)
            .HasDefaultValue(0)
            .HasColumnName("YearsOfExperience");

        // Index: find surgeons by specialty for surgery matching
        // e.g., "Find all available Cardiothoracic surgeons"
        builder.HasIndex(s => s.Specialty)
            .HasDatabaseName("IX_Surgeon_Specialty");

        // Composite index: active surgeons by specialty — most common query pattern
        // "Show me all active Cardiothoracic surgeons"
        builder.HasIndex(s => new { s.Specialty, s.IsActive })
            .HasDatabaseName("IX_Surgeon_Specialty_Active");

        // Index: experience level for advanced surgeon matching rules
        builder.HasIndex(s => s.YearsOfExperience)
            .HasDatabaseName("IX_Surgeon_YearsOfExperience");
    }
}