using HospitalSurgical.Application.DTOs;
using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Application.Services;

/// <summary>
/// Manages all staff operations: creation, querying, activation, soft delete.
///
/// TPH QUERY PATTERN:
/// - _context.Staff.OfType&lt;Surgeon&gt;()  → adds WHERE StaffType = 'Surgeon'
/// - _context.Staff.ToList()             → returns ALL types (polymorphic)
///
/// Step 4: SoftDelete sets IsDeleted = true.
///         Global Query Filter ensures deleted staff never appear in normal queries.
///         Use IgnoreQueryFilters() in repo to fetch deleted records for admins.
/// </summary>
public class StaffService : IStaffService
{
    private readonly IUnitOfWork _unitOfWork;

    public StaffService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ─────────────────────────────────────────────────────
    // SURGEON METHODS
    // ─────────────────────────────────────────────────────

    public async Task<SurgeonDto> CreateSurgeonAsync(CreateSurgeonDto dto)
    {
        if (dto.HireDate > DateTime.UtcNow)
            throw new ArgumentException("Hire date cannot be in the future.");

        if (dto.YearsOfExperience < 0)
            throw new ArgumentException("Years of experience cannot be negative.");

        var surgeon = new Surgeon
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            HireDate = dto.HireDate,
            Salary = dto.Salary,
            Specialty = dto.Specialty,
            LicenseNumber = dto.LicenseNumber,
            YearsOfExperience = dto.YearsOfExperience,
            IsActive = true
        };

        await _unitOfWork.Staff.AddAsync(surgeon);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (
            ex.InnerException?.Message.Contains("UNIQUE") == true)
        {
            throw new InvalidOperationException(
                $"License number '{dto.LicenseNumber}' is already registered.");
        }

        return MapSurgeonToDto(surgeon);
    }

    public async Task<SurgeonDto?> GetSurgeonByIdAsync(int id)
    {
        var surgeons = await _unitOfWork.Staff.GetAllSurgeonsAsync();
        var surgeon = surgeons.FirstOrDefault(s => s.Id == id);
        return surgeon is null ? null : MapSurgeonToDto(surgeon);
    }

    public async Task<IEnumerable<SurgeonDto>> GetAllSurgeonsAsync()
    {
        var surgeons = await _unitOfWork.Staff.GetAllSurgeonsAsync();
        return surgeons.Select(MapSurgeonToDto);
    }

    /// <summary>
    /// Returns a surgeon with their upcoming planned surgeries.
    /// Uses GetSurgeonWithPlanningAsync — Eager Loading with filtered Include.
    /// Only non-cancelled, future surgeries are loaded.
    /// </summary>
    public async Task<DoctorPlanningDto> GetSurgeonPlanningAsync(int id)
    {
        var surgeon = await _unitOfWork.Staff.GetSurgeonWithPlanningAsync(id)
            ?? throw new KeyNotFoundException($"Surgeon with ID {id} not found.");

        return new DoctorPlanningDto
        {
            Id = surgeon.Id,
            FullName = surgeon.FullName,
            Specialty = surgeon.Specialty,
            YearsOfExperience = surgeon.YearsOfExperience,
            IsActive = surgeon.IsActive,
            UpcomingSurgeries = surgeon.Surgeries.Select(s => new SurgeryDto
            {
                Id = s.Id,
                PlannedDate = s.PlannedDate,
                PlannedEndTime = s.PlannedEndTime,
                EstimatedDurationMinutes = s.EstimatedDurationMinutes,
                Status = s.Status.ToString(),
                ProcedureName = s.ProcedureName,
                Notes = s.Notes,
                SurgeonId = surgeon.Id,
                SurgeonName = surgeon.FullName,
                RoomNumber = s.OperatingRoom?.RoomNumber ?? "Unknown"
            }).ToList()
        };
    }

    public async Task<SurgeonDto> UpdateSurgeonAsync(int id, UpdateSurgeonDto dto)
    {
        var surgeons = await _unitOfWork.Staff.GetAllSurgeonsAsync();
        var surgeon = surgeons.FirstOrDefault(s => s.Id == id)
            ?? throw new KeyNotFoundException($"Surgeon with ID {id} not found.");

        // Update allowed fields — LicenseNumber is immutable
        if (!string.IsNullOrWhiteSpace(dto.FirstName))
            surgeon.FirstName = dto.FirstName;

        if (!string.IsNullOrWhiteSpace(dto.LastName))
            surgeon.LastName = dto.LastName;

        if (!string.IsNullOrWhiteSpace(dto.Specialty))
            surgeon.Specialty = dto.Specialty;

        if (dto.YearsOfExperience >= 0)
            surgeon.YearsOfExperience = dto.YearsOfExperience;

        if (dto.Salary > 0)
            surgeon.Salary = dto.Salary;

        _unitOfWork.Staff.Update(surgeon);
        await _unitOfWork.SaveChangesAsync();

        return MapSurgeonToDto(surgeon);
    }

    // ─────────────────────────────────────────────────────
    // NURSE METHODS
    // ─────────────────────────────────────────────────────

    public async Task<NurseDto> CreateNurseAsync(CreateNurseDto dto)
    {
        if (dto.HireDate > DateTime.UtcNow)
            throw new ArgumentException("Hire date cannot be in the future.");

        if (!Enum.TryParse<CertificationLevel>(dto.CertificationLevel, true, out var cert))
            throw new ArgumentException(
                $"Invalid certification level '{dto.CertificationLevel}'. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<CertificationLevel>())}");

        if (!Enum.TryParse<ShiftPreference>(dto.ShiftPreference, true, out var shift))
            throw new ArgumentException(
                $"Invalid shift preference '{dto.ShiftPreference}'. " +
                $"Valid values: {string.Join(", ", Enum.GetNames<ShiftPreference>())}");

        var nurse = new Nurse
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            HireDate = dto.HireDate,
            Salary = dto.Salary,
            CertificationLevel = cert,
            ShiftPreference = shift,
            DepartmentId = dto.DepartmentId,
            IsActive = true
        };

        await _unitOfWork.Staff.AddAsync(nurse);
        await _unitOfWork.SaveChangesAsync();

        return MapNurseToDto(nurse);
    }

    public async Task<NurseDto?> GetNurseByIdAsync(int id)
    {
        var staff = await _unitOfWork.Staff.GetByIdAsync(id);
        return staff is Nurse nurse ? MapNurseToDto(nurse) : null;
    }

    public async Task<IEnumerable<NurseDto>> GetAllNursesAsync()
    {
        var all = await _unitOfWork.Staff.GetAllAsync();
        return all.OfType<Nurse>().Select(MapNurseToDto);
    }

    public async Task<IEnumerable<NurseDto>> GetAvailableNursesAsync(DateTime date)
    {
        var nurses = await _unitOfWork.Staff.GetAvailableNursesAsync(date);
        return nurses.Select(MapNurseToDto);
    }

    // ─────────────────────────────────────────────────────
    // SHARED METHODS
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Step 4: Soft delete implementation.
    /// Sets IsDeleted = true — the Global Query Filter then excludes
    /// this staff member from ALL future queries automatically.
    ///
    /// HOW THE GLOBAL QUERY FILTER WORKS:
    /// In StaffConfiguration: builder.HasQueryFilter(s => !s.IsDeleted)
    /// EF Core automatically appends "WHERE IsDeleted = 0" to every query
    /// on the Staff table, UNLESS .IgnoreQueryFilters() is explicitly called.
    ///
    /// TO OVERRIDE THE FILTER for admins:
    /// _context.Staff.IgnoreQueryFilters().Where(s => s.IsDeleted).ToListAsync()
    /// This is used in the StaffController's GetDeleted() endpoint.
    /// </summary>
    public async Task SoftDeleteAsync(int id)
    {
        var staff = await _unitOfWork.Staff.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Staff member with ID {id} not found.");

        // Prevent deleting a surgeon with upcoming planned surgeries
        if (staff is Surgeon surgeon)
        {
            var planning = await _unitOfWork.Staff.GetSurgeonWithPlanningAsync(id);
            if (planning?.Surgeries.Any(s =>
                s.PlannedDate > DateTime.UtcNow &&
                s.Status == SurgeryStatus.Planned) == true)
            {
                throw new InvalidOperationException(
                    $"Cannot deactivate surgeon '{surgeon.FullName}'. " +
                    "They have upcoming planned surgeries. Reassign or cancel them first.");
            }
        }

        _unitOfWork.Staff.SoftDelete(staff);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task SetActiveStatusAsync(int id, bool isActive)
    {
        var staff = await _unitOfWork.Staff.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Staff member with ID {id} not found.");

        staff.IsActive = isActive;
        _unitOfWork.Staff.Update(staff);
        await _unitOfWork.SaveChangesAsync();
    }

    // ─────────────────────────────────────────────────────
    // Private mapping helpers
    // ─────────────────────────────────────────────────────

    private static SurgeonDto MapSurgeonToDto(Surgeon s) => new()
    {
        Id = s.Id,
        FirstName = s.FirstName,
        LastName = s.LastName,
        FullName = s.FullName,
        StaffType = "Surgeon",
        HireDate = s.HireDate,
        Salary = s.Salary,
        IsActive = s.IsActive,
        Specialty = s.Specialty,
        LicenseNumber = s.LicenseNumber,
        YearsOfExperience = s.YearsOfExperience
    };

    private static NurseDto MapNurseToDto(Nurse n) => new()
    {
        Id = n.Id,
        FirstName = n.FirstName,
        LastName = n.LastName,
        FullName = n.FullName,
        StaffType = "Nurse",
        HireDate = n.HireDate,
        Salary = n.Salary,
        IsActive = n.IsActive,
        CertificationLevel = n.CertificationLevel.ToString(),
        ShiftPreference = n.ShiftPreference.ToString(),
        DepartmentId = n.DepartmentId
    };
}