using HospitalSurgical.Application.DTOs;
using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Interfaces;
using HospitalSurgical.Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSurgical.API.Controllers;

/// <summary>
/// Manages patient records in the surgical system.
/// Includes soft delete support (Step 4) — patients are never permanently
/// removed unless explicitly requested by admin via HardDelete endpoint.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PatientsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public PatientsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ─────────────────────────────────────────────────────
    // READ ENDPOINTS
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// GET /api/patients
    /// Returns all active (non-deleted) patients, ordered alphabetically.
    /// Global Query Filter automatically excludes IsDeleted = true records.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var patients = await _unitOfWork.Patients.GetAllAsync();
        return Ok(patients.Select(MapToDto));
    }

    /// <summary>
    /// GET /api/patients/5
    /// Returns a single patient by ID.
    /// Returns 404 if patient is soft-deleted (filtered by global query filter).
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var patient = await _unitOfWork.Patients.GetByIdAsync(id);
        return patient is null ? NotFound() : Ok(MapToDto(patient));
    }

    /// <summary>
    /// GET /api/patients/file/PAT-001
    /// Looks up a patient by their unique file number.
    /// </summary>
    [HttpGet("file/{fileNumber}")]
    public async Task<IActionResult> GetByFileNumber(string fileNumber)
    {
        var patient = await _unitOfWork.Patients.GetByFileNumberAsync(fileNumber);
        return patient is null
            ? NotFound(new { error = $"Patient with file number '{fileNumber}' not found." })
            : Ok(MapToDto(patient));
    }

    /// <summary>
    /// GET /api/patients/5/surgeries
    /// Returns patient detail with their full surgical history.
    /// Uses Eager Loading (Include + ThenInclude) — Step 5 from base project.
    /// </summary>
    [HttpGet("{id:int}/surgeries")]
    public async Task<IActionResult> GetWithSurgeries(int id)
    {
        var patient = await _unitOfWork.Patients.GetWithSurgeriesAsync(id);

        if (patient is null)
            return NotFound(new { error = $"Patient with ID {id} not found." });

        return Ok(new
        {
            patient.Id,
            patient.FileNumber,
            FullName = $"{patient.FirstName} {patient.LastName}",
            patient.DateOfBirth,
            Age = CalculateAge(patient.DateOfBirth),
            Contact = new
            {
                patient.ContactInfo.Phone,
                patient.ContactInfo.Email,
                patient.ContactInfo.EmergencyContact,
                patient.ContactInfo.EmergencyPhone
            },
            Address = new
            {
                patient.Address.Street,
                patient.Address.City,
                patient.Address.ZipCode,
                patient.Address.Country
            },
            Surgeries = patient.Surgeries.Select(s => new
            {
                s.Id,
                s.ProcedureName,
                s.PlannedDate,
                PlannedEndTime = s.PlannedEndTime,
                s.EstimatedDurationMinutes,
                Status = s.Status.ToString(),
                Surgeon = s.Surgeon is not null
                    ? $"Dr. {s.Surgeon.FirstName} {s.Surgeon.LastName}"
                    : "Unknown",
                Room = s.OperatingRoom?.RoomNumber ?? "Unknown"
            }).OrderByDescending(s => s.PlannedDate)
        });
    }

    /// <summary>
    /// GET /api/patients/search?name=Dupont
    /// Full-text search across first and last name fields.
    /// Leverages the IX_Patient_LastName index for performance.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return BadRequest(new { error = "Search term cannot be empty." });

        var patients = await _unitOfWork.Patients.SearchByNameAsync(name);
        return Ok(patients.Select(MapToDto));
    }

    // ─────────────────────────────────────────────────────
    // WRITE ENDPOINTS
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// POST /api/patients
    /// Creates a new patient record.
    /// Validates date of birth is in the past.
    /// ContactInfo and Address are mapped as Owned Types (Step 3).
    /// Shadow properties (CreatedAt, CreatedBy) are set automatically
    /// by the AuditInterceptor (Step 5).
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Validate date of birth
        if (dto.DateOfBirth >= DateTime.UtcNow)
            return BadRequest(new { error = "Date of birth must be in the past." });

        // Check file number uniqueness
        var existing = await _unitOfWork.Patients.GetByFileNumberAsync(dto.FileNumber);
        if (existing is not null)
            return Conflict(new { error = $"File number '{dto.FileNumber}' is already in use." });

        var patient = new Patient
        {
            FileNumber = dto.FileNumber,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DateOfBirth = dto.DateOfBirth,
            // Step 3: Owned Type — ContactInfo and Address mapped inline
            ContactInfo = new ContactInfo
            {
                Phone = dto.Phone,
                Email = dto.Email,
                EmergencyContact = dto.EmergencyContact,
                EmergencyPhone = dto.EmergencyPhone
            },
            Address = new Address
            {
                Street = dto.Street ?? string.Empty,
                City = dto.City ?? string.Empty,
                ZipCode = dto.ZipCode ?? string.Empty,
                Country = dto.Country ?? string.Empty
            }
        };

        await _unitOfWork.Patients.AddAsync(patient);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, MapToDto(patient));
    }

    /// <summary>
    /// PUT /api/patients/5
    /// Updates patient contact information.
    /// FileNumber is immutable — cannot be changed after creation.
    /// </summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var patient = await _unitOfWork.Patients.GetByIdAsync(id);
        if (patient is null)
            return NotFound(new { error = $"Patient with ID {id} not found." });

        // Update mutable fields only — FileNumber stays immutable
        patient.FirstName = dto.FirstName;
        patient.LastName = dto.LastName;
        patient.ContactInfo = new ContactInfo
        {
            Phone = dto.Phone,
            Email = dto.Email,
            EmergencyContact = dto.EmergencyContact,
            EmergencyPhone = dto.EmergencyPhone
        };
        patient.Address = new Address
        {
            Street = dto.Street ?? string.Empty,
            City = dto.City ?? string.Empty,
            ZipCode = dto.ZipCode ?? string.Empty,
            Country = dto.Country ?? string.Empty
        };

        _unitOfWork.Patients.Update(patient);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex) when (ex.Message.Contains("UNIQUE"))
        {
            return Conflict(new { error = "A constraint was violated during update." });
        }

        return Ok(MapToDto(patient));
    }

    // ─────────────────────────────────────────────────────
    // DELETE ENDPOINTS (Step 4: Soft Delete)
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// DELETE /api/patients/5
    /// SOFT DELETE — sets IsDeleted = true. Patient is hidden from all
    /// normal queries via the Global Query Filter but remains in the DB.
    /// Medical records must never be permanently deleted without legal authorization.
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var patient = await _unitOfWork.Patients.GetByIdAsync(id);
        if (patient is null)
            return NotFound(new { error = $"Patient with ID {id} not found." });

        // Prevent deleting a patient with upcoming planned surgeries
        var patientWithSurgeries = await _unitOfWork.Patients.GetWithSurgeriesAsync(id);
        var hasUpcomingSurgeries = patientWithSurgeries?.Surgeries
            .Any(s => s.PlannedDate > DateTime.UtcNow
                && s.Status == Domain.Enums.SurgeryStatus.Planned) ?? false;

        if (hasUpcomingSurgeries)
            return Conflict(new
            {
                error = "Cannot delete patient with upcoming planned surgeries. " +
                        "Cancel all planned surgeries first."
            });

        _unitOfWork.Patients.SoftDelete(patient);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// DELETE /api/patients/5/hard
    /// HARD DELETE — physically removes the patient from the database.
    /// Admin-only operation. Should be used only for test data or legal erasure requests.
    /// </summary>
    [HttpDelete("{id:int}/hard")]
    public async Task<IActionResult> HardDelete(int id)
    {
        // In production: add [Authorize(Roles = "Admin")] here
        // IgnoreQueryFilters() needed to find soft-deleted patients
        var patient = await _unitOfWork.Patients.GetByIdAsync(id);
        if (patient is null)
            return NotFound(new { error = $"Patient with ID {id} not found." });

        _unitOfWork.Patients.HardDelete(patient);
        await _unitOfWork.SaveChangesAsync();

        return NoContent();
    }

    // ─────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────

    private static PatientDto MapToDto(Patient patient) => new()
    {
        Id = patient.Id,
        FileNumber = patient.FileNumber,
        FirstName = patient.FirstName,
        LastName = patient.LastName,
        DateOfBirth = patient.DateOfBirth,
        Phone = patient.ContactInfo.Phone,
        Email = patient.ContactInfo.Email,
        EmergencyContact = patient.ContactInfo.EmergencyContact,
        EmergencyPhone = patient.ContactInfo.EmergencyPhone
    };

    private static int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}