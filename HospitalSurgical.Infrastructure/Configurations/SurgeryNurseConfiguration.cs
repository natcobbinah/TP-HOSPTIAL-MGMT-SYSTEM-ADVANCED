using HospitalSurgical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalSurgical.Infrastructure.Configurations;

public class SurgeryNurseConfiguration : IEntityTypeConfiguration<SurgeryNurse>
{
    public void Configure(EntityTypeBuilder<SurgeryNurse> builder)
    {
        builder.ToTable("SurgeryNurses");

        // Composite primary key — natural key for this join table
        builder.HasKey(sn => new { sn.SurgeryId, sn.NurseId });

        builder.Property(sn => sn.RoleDuringSurgery).HasMaxLength(100);

        builder.HasOne(sn => sn.Surgery)
            .WithMany(s => s.NurseAssignments)
            .HasForeignKey(sn => sn.SurgeryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sn => sn.Nurse)
            .WithMany(n => n.SurgeryAssignments)
            .HasForeignKey(sn => sn.NurseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}