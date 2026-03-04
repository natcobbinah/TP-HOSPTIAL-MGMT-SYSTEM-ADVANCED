using System.ComponentModel.DataAnnotations;

namespace HospitalSurgical.Domain.Entities;

/// <summary>
/// Explicit Many-to-Many join entity between Surgery and Nurse.
///
/// WHY NOT IMPLICIT MANY-TO-MANY?
/// EF Core's implicit M2M only handles the FK relationship.
/// We need extra data on the join: RoleDuringSurgery and IsScrubNurse.
/// An explicit join entity also allows querying assignments directly:
///   _context.SurgeryNurses.Where(sn => sn.IsScrubNurse).ToListAsync()
///
/// HOW TO LOAD nurses of a surgery:
///   surgery.NurseAssignments          → loads SurgeryNurse join entities
///   surgery.NurseAssignments.Select(sn => sn.Nurse)  → the actual nurses
///   OR use Include:
///   _context.Surgeries
///       .Include(s => s.NurseAssignments)
///           .ThenInclude(sn => sn.Nurse)
///       .FirstOrDefaultAsync(s => s.Id == id)
/// </summary>
public class SurgeryNurse
{
    // Composite primary key configured in SurgeryNurseConfiguration
    public int SurgeryId { get; set; }
    public Surgery Surgery { get; set; } = null!;

    public int NurseId { get; set; }
    public Nurse Nurse { get; set; } = null!;

    [MaxLength(100)]
    public string RoleDuringSurgery { get; set; } = string.Empty;

    /// <summary>
    /// True if this nurse is the scrub nurse (directly assisting the surgeon).
    /// Only one scrub nurse per surgery is recommended.
    /// </summary>
    public bool IsScrubNurse { get; set; } = false;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}