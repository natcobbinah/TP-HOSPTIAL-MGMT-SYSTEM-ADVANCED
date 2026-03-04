using System.ComponentModel.DataAnnotations;

namespace HospitalSurgical.Application.DTOs;

public class StaffDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StaffType { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
}

public class SurgeonDto : StaffDto
{
    public string Specialty { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
}

public class NurseDto : StaffDto
{
    public string CertificationLevel { get; set; } = string.Empty;
    public string ShiftPreference { get; set; } = string.Empty;
    public int? DepartmentId { get; set; }
}

public class CreateSurgeonDto
{
    [Required][MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required][MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public decimal Salary { get; set; }

    [Required][MaxLength(100)]
    public string Specialty { get; set; } = string.Empty;

    [Required][MaxLength(50)]
    public string LicenseNumber { get; set; } = string.Empty;

    [Range(0, 60)]
    public int YearsOfExperience { get; set; }
}

public class CreateNurseDto
{
    [Required][MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required][MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime HireDate { get; set; } = DateTime.UtcNow;
    public decimal Salary { get; set; }

    [Required]
    public string CertificationLevel { get; set; } = string.Empty;

    [Required]
    public string ShiftPreference { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }
}