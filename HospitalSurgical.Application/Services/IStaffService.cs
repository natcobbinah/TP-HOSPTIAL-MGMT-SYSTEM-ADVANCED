using HospitalSurgical.Application.DTOs;

namespace HospitalSurgical.Application.Services;

/// <summary>
/// Service interface for managing all staff types.
/// Uses the TPH hierarchy — Surgeon, Nurse, and AdministrativeStaff
/// are all accessible through this single service using type-specific methods.
/// </summary>
public interface IStaffService
{
    // ── Surgeons ────────────────────────────────────────
    Task<SurgeonDto> CreateSurgeonAsync(CreateSurgeonDto dto);
    Task<SurgeonDto?> GetSurgeonByIdAsync(int id);
    Task<IEnumerable<SurgeonDto>> GetAllSurgeonsAsync();
    Task<DoctorPlanningDto> GetSurgeonPlanningAsync(int id);
    Task<SurgeonDto> UpdateSurgeonAsync(int id, UpdateSurgeonDto dto);

    // ── Nurses ──────────────────────────────────────────
    Task<NurseDto> CreateNurseAsync(CreateNurseDto dto);
    Task<NurseDto?> GetNurseByIdAsync(int id);
    Task<IEnumerable<NurseDto>> GetAllNursesAsync();
    Task<IEnumerable<NurseDto>> GetAvailableNursesAsync(DateTime date);

    // ── Shared ──────────────────────────────────────────

    /// <summary>
    /// Step 4: Soft delete — hides staff from all queries.
    /// Staff records are retained for audit and historical data.
    /// </summary>
    Task SoftDeleteAsync(int id);

    /// <summary>
    /// Reactivates a previously deactivated staff member.
    /// </summary>
    Task SetActiveStatusAsync(int id, bool isActive);
}

/// <summary>
/// DTO for updating surgeon details.
/// License number is immutable — use a separate process to update.
/// </summary>
public class UpdateSurgeonDto
{
    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.MaxLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0, 60)]
    public int YearsOfExperience { get; set; }

    public decimal Salary { get; set; }
}

/// <summary>
/// Doctor planning DTO — reused from Step 5 dashboard pattern.
/// Shows surgeon + department context + upcoming surgeries.
/// </summary>
public class DoctorPlanningDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public bool IsActive { get; set; }
    public List<SurgeryDto> UpcomingSurgeries { get; set; } = new();
}