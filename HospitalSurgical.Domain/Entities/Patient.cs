using System.ComponentModel.DataAnnotations;
using HospitalSurgical.Domain.ValueObjects;

namespace HospitalSurgical.Domain.Entities;

public class Patient
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string FileNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime DateOfBirth { get; set; }

    // Step 3: Owned Types — stored inline in Patients table
    public ContactInfo ContactInfo { get; set; } = new ContactInfo();
    public Address Address { get; set; } = new Address();

    // Step 4: Soft Delete
    public bool IsDeleted { get; set; } = false;

    [MaxLength(36)]
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    public ICollection<Surgery> Surgeries { get; set; } = new List<Surgery>();
}