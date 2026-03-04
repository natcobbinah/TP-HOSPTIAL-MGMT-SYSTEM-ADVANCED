using HospitalSurgical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalSurgical.Infrastructure.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.FileNumber).IsUnique()
            .HasDatabaseName("IX_Patient_FileNumber");

        builder.Property(p => p.ConcurrencyStamp)
            .IsConcurrencyToken().HasMaxLength(36);

        // Step 4: Soft Delete filter
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Step 5: Shadow Properties
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<DateTime>("UpdatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<string>("CreatedBy").HasMaxLength(100).HasDefaultValue("System");
        builder.Property<string>("UpdatedBy").HasMaxLength(100).HasDefaultValue("System");

        // Step 3: Owned Types — mapped to columns in Patients table
        builder.OwnsOne(p => p.ContactInfo, ci =>
        {
            ci.Property(c => c.Phone).HasMaxLength(20).HasColumnName("Phone");
            ci.Property(c => c.Email).HasMaxLength(150).HasColumnName("Email");
            ci.Property(c => c.EmergencyContact).HasMaxLength(200).HasColumnName("EmergencyContact");
            ci.Property(c => c.EmergencyPhone).HasMaxLength(20).HasColumnName("EmergencyPhone");
        });

        builder.OwnsOne(p => p.Address, a =>
        {
            a.Property(addr => addr.Street).HasMaxLength(200).HasColumnName("Address_Street");
            a.Property(addr => addr.City).HasMaxLength(100).HasColumnName("Address_City");
            a.Property(addr => addr.ZipCode).HasMaxLength(20).HasColumnName("Address_ZipCode");
            a.Property(addr => addr.Country).HasMaxLength(100).HasColumnName("Address_Country");
        });
    }
}