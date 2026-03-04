using System.ComponentModel.DataAnnotations;

namespace HospitalSurgical.Domain.ValueObjects;

/// <summary>
/// Value Object for contact information — reused across Patient, Staff, OperatingRoom.
///
/// DIFFERENCE BETWEEN OWNED TYPE AND VALUE OBJECT:
/// - Value Object (DDD concept): immutable object with no identity,
///   equality based on its property values. Address("Paris") == Address("Paris").
/// - Owned Type (EF Core concept): how EF Core maps a Value Object to the database.
///   The columns are stored inline in the owner's table (no separate table, no FK).
///
/// VALIDATION of Owned Types:
/// - Data annotations on properties work normally ([Required], [MaxLength])
/// - Custom validation: implement IValidatableObject on the owning entity
/// - Or validate in the Application layer before mapping to entity
/// </summary>
public class ContactInfo
{
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    [MaxLength(150)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200)]
    public string EmergencyContact { get; set; } = string.Empty;

    [MaxLength(20)]
    public string EmergencyPhone { get; set; } = string.Empty;
}