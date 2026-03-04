using System.ComponentModel.DataAnnotations;

namespace HospitalSurgical.Domain.Entities;

/// <summary>
/// Abstract base class for all hospital staff using TPH inheritance.
///
/// TPH STRATEGY: All staff types stored in a single "Staff" table.
/// EF Core adds a "StaffType" discriminator column automatically.
///
/// WHY TPH over TPT/TPC?
/// - TPH: 1 table, fastest reads, nullable type-specific columns
/// - TPT: 1 table per type + JOINs on every query = slower
/// - TPC: 1 table per concrete type, no JOINs but duplicates base columns
///
/// For this model: staff queries are frequent, types are few (3),
/// nullable overhead is acceptable → TPH is the optimal choice.
///
/// SHADOW PROPERTIES (Step 5):
/// CreatedAt, UpdatedAt, CreatedBy, UpdatedBy are NOT defined here
/// as C# properties — they live only in EF Core metadata (shadow properties).
/// This keeps the domain model clean of infrastructure concerns.
///
/// SOFT DELETE (Step 4):
/// IsDeleted is implemented here so the global query filter applies
/// to ALL staff types via the base class.
/// </summary>
public abstract class Staff
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public DateTime HireDate { get; set; }

    public decimal Salary { get; set; }

    public bool IsActive { get; set; } = true;

    // Step 4: Soft Delete
    public bool IsDeleted { get; set; } = false;

    // Step 6: Optimistic concurrency token
    [MaxLength(36)]
    public string ConcurrencyStamp { get; set; } = Guid.NewGuid().ToString();

    // FullName computed property — domain logic lives in the entity
    public string FullName => $"{FirstName} {LastName}";
}