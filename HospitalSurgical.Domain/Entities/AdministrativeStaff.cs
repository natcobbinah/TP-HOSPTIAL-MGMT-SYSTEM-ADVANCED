using System.ComponentModel.DataAnnotations;

namespace HospitalSurgical.Domain.Entities;

/// <summary>
/// Administrative staff entity — inherits from Staff via TPH.
/// Discriminator value: "Administrative"
/// </summary>
public class AdministrativeStaff : Staff
{
    [MaxLength(100)]
    public string Function { get; set; } = string.Empty;

    [MaxLength(20)]
    public string OfficeNumber { get; set; } = string.Empty;
}