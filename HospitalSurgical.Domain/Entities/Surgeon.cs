using System.ComponentModel.DataAnnotations;

namespace HospitalSurgical.Domain.Entities;

/// <summary>
/// Surgeon entity — inherits from Staff via TPH.
/// Discriminator value: "Surgeon"
///
/// QUERYING a specific type:
///   // Get only surgeons:
///   _context.Staff.OfType<Surgeon>().ToListAsync()
///   // Generated SQL: SELECT ... FROM Staff WHERE StaffType = 'Surgeon'
/// </summary>
public class Surgeon : Staff
{
    [Required]
    [MaxLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    public int YearsOfExperience { get; set; }

    // Navigation: a surgeon performs many surgeries
    public ICollection<Surgery> Surgeries { get; set; } = new List<Surgery>();
}