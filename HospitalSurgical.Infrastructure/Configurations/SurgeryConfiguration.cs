using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalSurgical.Infrastructure.Configurations;

public class SurgeryConfiguration : IEntityTypeConfiguration<Surgery>
{
    public void Configure(EntityTypeBuilder<Surgery> builder)
    {
        builder.ToTable("Surgeries");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.ProcedureName).HasMaxLength(200);
        builder.Property(s => s.Notes).HasMaxLength(500);

        builder.Property(s => s.ConcurrencyStamp)
            .IsConcurrencyToken()
            .HasMaxLength(36);

        // Step 4: Global Query Filter for soft delete
        builder.HasQueryFilter(s => !s.IsDeleted);

        // Step 5: Shadow Properties
        builder.Property<DateTime>("CreatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<DateTime>("UpdatedAt").HasDefaultValueSql("CURRENT_TIMESTAMP");
        builder.Property<string>("CreatedBy").HasMaxLength(100).HasDefaultValue("System");
        builder.Property<string>("UpdatedBy").HasMaxLength(100).HasDefaultValue("System");

        // Relationships
        builder.HasOne(s => s.Patient)
            .WithMany(p => p.Surgeries)
            .HasForeignKey(s => s.PatientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Surgeon)
            .WithMany(sur => sur.Surgeries)
            .HasForeignKey(s => s.SurgeonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.OperatingRoom)
            .WithMany(r => r.Surgeries)
            .HasForeignKey(s => s.OperatingRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        // Step 7: Indexes for compiled queries
        builder.HasIndex(s => new { s.SurgeonId, s.PlannedDate })
            .HasDatabaseName("IX_Surgery_Surgeon_Date");

        builder.HasIndex(s => new { s.OperatingRoomId, s.PlannedDate })
            .HasDatabaseName("IX_Surgery_Room_Date");

        builder.HasIndex(s => s.PlannedDate)
            .HasDatabaseName("IX_Surgery_PlannedDate");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("IX_Surgery_Status");
    }
}