using System.ComponentModel.DataAnnotations;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Domain.ValueObjects;

namespace HospitalSurgical.Domain.Entities;

/// <summary>
/// Represents a surgical intervention.
/// Links: Patient (M:1), Surgeon (M:1), OperatingRoom (M:1)
/// Many-to-Many with Nurse via SurgeryNurse join entity.
///
/// CONCURRENCY (Step 6):
/// ConcurrencyStamp protects against simultaneous schedule edits
/// by two secretaries — throws DbUpdateConcurrencyException on conflict.
/// </summary>
public class Surgery
{
    public int Id { get; set; }

    public DateTime PlannedDate { get; set; }

    /// <summary>Estimated duration in minutes.</summary>
    public int EstimatedDurationMinutes { get; set; }

    public SurgeryStatus Status { get; set; } = SurgeryStatus.Planned;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ProcedureName { get; set; } = string.Empty;

    // Step 4: Soft delete
    public bool IsDeleted { get; set; } = false;

    // Step 6: Optimistic concurrency
    [MaxLength(36)]
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    // Foreign keys
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public int SurgeonId { get; set; }
    public Surgeon Surgeon { get; set; } = null!;

    public int OperatingRoomId { get; set; }
    public OperatingRoom OperatingRoom { get; set; } = null!;

    // Step 2: Many-to-Many nurses via explicit join entity
    public ICollection<SurgeryNurse> NurseAssignments { get; set; } = new List<SurgeryNurse>();

    /// <summary>
    /// Domain method: calculates end time based on planned date + duration.
    /// Business logic belongs in the entity, not in services.
    /// </summary>
    public DateTime PlannedEndTime => PlannedDate.AddMinutes(EstimatedDurationMinutes);

    /// <summary>
    /// Domain method: checks if this surgery overlaps with another time range.
    /// Used for conflict detection.
    /// </summary>
    public bool OverlapsWith(DateTime start, DateTime end)
    {
        return PlannedDate < end && PlannedEndTime > start;
    }
}