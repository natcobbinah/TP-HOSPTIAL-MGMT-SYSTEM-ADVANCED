using System.ComponentModel.DataAnnotations;
using HospitalSurgical.Domain.Enums;

namespace HospitalSurgical.Domain.Entities;

/// <summary>
/// Nurse entity — inherits from Staff via TPH.
/// Discriminator value: "Nurse"
/// </summary>
public class Nurse : Staff
{
    public int? DepartmentId { get; set; }

    public CertificationLevel CertificationLevel { get; set; }

    public ShiftPreference ShiftPreference { get; set; }

    // Step 2: Many-to-Many with Surgery via SurgeryNurse join entity
    public ICollection<SurgeryNurse> SurgeryAssignments { get; set; } = new List<SurgeryNurse>();
}