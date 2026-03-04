using System.ComponentModel.DataAnnotations;

namespace HospitalSurgical.Application.DTOs;

public class SurgeryDto
{
    public int Id { get; set; }
    public DateTime PlannedDate { get; set; }
    public DateTime PlannedEndTime { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ProcedureName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int SurgeonId { get; set; }
    public string SurgeonName { get; set; } = string.Empty;
    public int OperatingRoomId { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public List<NurseAssignmentDto> Nurses { get; set; } = new();

    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class NurseAssignmentDto
{
    public int NurseId { get; set; }
    public string NurseName { get; set; } = string.Empty;
    public string RoleDuringSurgery { get; set; } = string.Empty;
    public bool IsScrubNurse { get; set; }
}

public class CreateSurgeryDto
{
    [Required]
    public DateTime PlannedDate { get; set; }

    [Required]
    [Range(15, 600, ErrorMessage = "Duration must be between 15 and 600 minutes.")]
    public int EstimatedDurationMinutes { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProcedureName { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    [Required]
    public int PatientId { get; set; }

    [Required]
    public int SurgeonId { get; set; }

    [Required]
    public int OperatingRoomId { get; set; }

    /// <summary>Nurses to assign at creation time.</summary>
    public List<AssignNurseDto> Nurses { get; set; } = new();

    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class AssignNurseDto
{
    [Required]
    public int NurseId { get; set; }

    [MaxLength(100)]
    public string RoleDuringSurgery { get; set; } = string.Empty;

    public bool IsScrubNurse { get; set; } = false;
}

public class UpdateSurgeryStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Notes { get; set; } = string.Empty;

    /// <summary>
    /// Step 6: The client must send back the ConcurrencyStamp it received
    /// so we can detect if another user modified the surgery in the meantime.
    /// </summary>
    [Required]
    public string ConcurrencyStamp { get; set; } = string.Empty;
}

public class RescheduleSurgeryDto
{
    [Required]
    public DateTime NewPlannedDate { get; set; }

    public int? NewOperatingRoomId { get; set; }

    [MaxLength(500)]
    public string RescheduleReason { get; set; } = string.Empty;

    /// <summary>Step 6: Required for optimistic concurrency.</summary>
    [Required]
    public string ConcurrencyStamp { get; set; } = string.Empty;
}