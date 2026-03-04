using HospitalSurgical.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HospitalSurgical.Infrastructure.Configurations;

public class OperatingRoomConfiguration : IEntityTypeConfiguration<OperatingRoom>
{
    public void Configure(EntityTypeBuilder<OperatingRoom> builder)
    {
        builder.ToTable("OperatingRooms");
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => r.RoomNumber).IsUnique()
            .HasDatabaseName("IX_OperatingRoom_Number");

        builder.Property(r => r.Status)
            .HasConversion<string>().HasMaxLength(20);

        // Step 4: Soft delete filter
        builder.HasQueryFilter(r => !r.IsDeleted);

        // Step 3: Owned Type
        builder.OwnsOne(r => r.ContactInfo, ci =>
        {
            ci.Property(c => c.Phone).HasMaxLength(20).HasColumnName("Phone");
            ci.Property(c => c.Email).HasMaxLength(150).HasColumnName("Email");
            ci.Property(c => c.EmergencyContact).HasMaxLength(200).HasColumnName("EmergencyContact");
            ci.Property(c => c.EmergencyPhone).HasMaxLength(20).HasColumnName("EmergencyPhone");
        });
    }
}