using HospitalSurgical.Application.DTOs;
using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSurgical.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StaffController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public StaffController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("surgeons")]
    public async Task<IActionResult> GetSurgeons()
    {
        var surgeons = await _unitOfWork.Staff.GetAllSurgeonsAsync();
        return Ok(surgeons.Select(s => new SurgeonDto
        {
            Id = s.Id, FirstName = s.FirstName, LastName = s.LastName,
            FullName = s.FullName, StaffType = "Surgeon",
            HireDate = s.HireDate, Salary = s.Salary, IsActive = s.IsActive,
            Specialty = s.Specialty, LicenseNumber = s.LicenseNumber,
            YearsOfExperience = s.YearsOfExperience
        }));
    }

    [HttpGet("nurses/available")]
    public async Task<IActionResult> GetAvailableNurses([FromQuery] DateTime date)
    {
        var nurses = await _unitOfWork.Staff.GetAvailableNursesAsync(date);
        return Ok(nurses.Select(n => new NurseDto
        {
            Id = n.Id, FirstName = n.FirstName, LastName = n.LastName,
            FullName = n.FullName, StaffType = "Nurse",
            HireDate = n.HireDate, Salary = n.Salary, IsActive = n.IsActive,
            CertificationLevel = n.CertificationLevel.ToString(),
            ShiftPreference = n.ShiftPreference.ToString(),
            DepartmentId = n.DepartmentId
        }));
    }

    [HttpGet("surgeons/{id:int}/planning")]
    public async Task<IActionResult> GetSurgeonPlanning(int id)
    {
        var surgeon = await _unitOfWork.Staff.GetSurgeonWithPlanningAsync(id);
        if (surgeon is null) return NotFound();
        return Ok(new
        {
            surgeon.Id,
            surgeon.FullName,
            surgeon.Specialty,
            UpcomingSurgeries = surgeon.Surgeries.Select(s => new
            {
                s.Id, s.PlannedDate, s.PlannedEndTime,
                s.ProcedureName, s.Status,
                Room = s.OperatingRoom?.RoomNumber
            })
        });
    }

    [HttpPost("surgeons")]
    public async Task<IActionResult> CreateSurgeon([FromBody] CreateSurgeonDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var surgeon = new Surgeon
        {
            FirstName = dto.FirstName, LastName = dto.LastName,
            HireDate = dto.HireDate, Salary = dto.Salary,
            Specialty = dto.Specialty, LicenseNumber = dto.LicenseNumber,
            YearsOfExperience = dto.YearsOfExperience, IsActive = true
        };
        await _unitOfWork.Staff.AddAsync(surgeon);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSurgeons), new { id = surgeon.Id }, surgeon);
    }

    [HttpPost("nurses")]
    public async Task<IActionResult> CreateNurse([FromBody] CreateNurseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (!Enum.TryParse<CertificationLevel>(dto.CertificationLevel, true, out var cert))
            return BadRequest(new { error = $"Invalid certification level: {dto.CertificationLevel}" });

        if (!Enum.TryParse<ShiftPreference>(dto.ShiftPreference, true, out var shift))
            return BadRequest(new { error = $"Invalid shift preference: {dto.ShiftPreference}" });

        var nurse = new Nurse
        {
            FirstName = dto.FirstName, LastName = dto.LastName,
            HireDate = dto.HireDate, Salary = dto.Salary,
            CertificationLevel = cert, ShiftPreference = shift,
            DepartmentId = dto.DepartmentId, IsActive = true
        };
        await _unitOfWork.Staff.AddAsync(nurse);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetSurgeons), new { id = nurse.Id }, nurse);
    }

    /// <summary>
    /// Step 4: Soft delete — staff member is hidden but not removed.
    /// DELETE /api/staff/5
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var staff = await _unitOfWork.Staff.GetByIdAsync(id);
        if (staff is null) return NotFound();
        _unitOfWork.Staff.SoftDelete(staff);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Step 4: Recover all soft-deleted staff (admin only).
    /// GET /api/staff/deleted
    /// </summary>
    [HttpGet("deleted")]
    public async Task<IActionResult> GetDeleted()
    {
        // IgnoreQueryFilters is applied inside the repository implementation
        var deleted = await _unitOfWork.Staff.GetAllAsync();
        return Ok(deleted.Where(s => s.IsDeleted));
    }
}