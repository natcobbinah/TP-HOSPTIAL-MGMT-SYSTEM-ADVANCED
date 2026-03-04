using System.ComponentModel.DataAnnotations;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Domain.ValueObjects;

namespace HospitalSurgical.Domain.Entities;

public class OperatingRoom
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    public int Floor { get; set; }

    [MaxLength(500)]
    public string Equipment { get; set; } = string.Empty;

    public OperatingRoomStatus Status { get; set; } = OperatingRoomStatus.Available;

    // Step 3: Owned Type
    public ContactInfo ContactInfo { get; set; } = new ContactInfo();

    // Step 4: Soft Delete
    public bool IsDeleted { get; set; } = false;

    public ICollection<Surgery> Surgeries { get; set; } = new List<Surgery>();
}