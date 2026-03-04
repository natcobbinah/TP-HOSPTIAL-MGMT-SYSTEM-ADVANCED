using HospitalSurgical.Domain.Interfaces;
using HospitalSurgical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Infrastructure.Repositories;

/// <summary>
/// Generic base repository implementing common CRUD operations.
///
/// DESIGN DECISIONS:
/// 1. GetByIdAsync uses FindAsync — checks the EF Core identity cache first,
///    then falls back to a DB query. Fastest for single-PK lookups.
///
/// 2. GetAllAsync uses AsNoTracking — read-only queries don't need change
///    tracking. Reduces memory allocation by ~30-40% for large result sets.
///
/// 3. SoftDelete vs HardDelete:
///    - SoftDelete: Sets IsDeleted = true. Global Query Filter hides it.
///      Used for all normal deletions (Step 4).
///    - HardDelete: Physically removes the row. Admin-only, irreversible.
///
/// 4. Update does NOT call SaveChangesAsync — this is intentional.
///    The Unit of Work pattern requires SaveChangesAsync to be called once
///    at the end of the operation (Step 8). This ensures all operations
///    in a business transaction are committed atomically.
///
/// WHY GENERIC BASE REPOSITORY?
/// - Eliminates duplicate GetByIdAsync/AddAsync/Update/SoftDelete
///   across PatientRepository, SurgeryRepository, StaffRepository, etc.
/// - Concrete repositories extend this with domain-specific queries.
/// - The IRepository&lt;T&gt; interface in Domain defines the contract;
///   this class provides the shared implementation.
/// </summary>
/// <typeparam name="T">The entity type this repository manages.</typeparam>
public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly SurgicalDbContext Context;
    protected readonly DbSet<T> DbSet;

    protected BaseRepository(SurgicalDbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    // ─────────────────────────────────────────────────────
    // READ
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Gets a single entity by primary key.
    ///
    /// Uses FindAsync which checks the EF Core identity map (in-memory cache)
    /// before hitting the database. Fastest approach for PK lookups.
    ///
    /// NOTE: Respects Global Query Filters (e.g., IsDeleted = false).
    /// To bypass: use Context.Set&lt;T&gt;().IgnoreQueryFilters().FindAsync(id)
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(int id)
        => await DbSet.FindAsync(id);

    /// <summary>
    /// Gets all entities with AsNoTracking for read-only scenarios.
    ///
    /// AsNoTracking:
    /// - No ChangeTracker overhead (entities are not tracked)
    /// - ~30% less memory for large result sets
    /// - Slightly faster query execution
    /// - Appropriate for any list/read endpoint where we won't update the entities
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
        => await DbSet.AsNoTracking().ToListAsync();

    // ─────────────────────────────────────────────────────
    // WRITE
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds a new entity to the DbContext change tracker.
    /// The entity is NOT saved to the database until SaveChangesAsync is called
    /// (by the Unit of Work — Step 8).
    /// </summary>
    public virtual async Task AddAsync(T entity)
        => await DbSet.AddAsync(entity);

    /// <summary>
    /// Marks an entity as Modified in the change tracker.
    /// All properties are marked for update (full entity update).
    /// For partial updates, use Context.Entry(entity).Property(x => x.Prop).IsModified = true.
    /// </summary>
    public virtual void Update(T entity)
        => DbSet.Update(entity);

    // ─────────────────────────────────────────────────────
    // DELETE
    // ─────────────────────────────────────────────────────

    /// <summary>
    /// Step 4: Soft Delete implementation.
    ///
    /// Sets IsDeleted = true via reflection-based property access.
    /// The Global Query Filter (HasQueryFilter(e => !e.IsDeleted))
    /// then automatically excludes this entity from all queries.
    ///
    /// REFLECTION APPROACH: Used because the base class doesn't know the
    /// concrete type's IsDeleted property at compile time.
    /// The concrete repositories can override this with a strongly-typed version.
    ///
    /// HOW THE GLOBAL QUERY FILTER WORKS:
    /// EF Core appends "WHERE IsDeleted = 0" to every generated SQL query.
    /// To bypass it: DbSet.IgnoreQueryFilters().Where(...)
    ///
    /// HOW TO OVERRIDE THE FILTER FOR ADMINS:
    /// Context.Set&lt;T&gt;().IgnoreQueryFilters().Where(e => EF.Property&lt;bool&gt;(e, "IsDeleted")).ToListAsync()
    /// </summary>
    public virtual void SoftDelete(T entity)
    {
        var isDeletedProperty = typeof(T).GetProperty("IsDeleted");
        if (isDeletedProperty is null)
            throw new InvalidOperationException(
                $"Entity type '{typeof(T).Name}' does not have an IsDeleted property. " +
                "Ensure the entity implements soft delete.");

        isDeletedProperty.SetValue(entity, true);
        DbSet.Update(entity);
    }

    /// <summary>
    /// Hard Delete — physically removes the record from the database.
    ///
    /// WHEN TO USE:
    /// - Test data cleanup
    /// - GDPR/legal erasure requests (right to be forgotten)
    /// - Duplicate records created by error
    ///
    /// NEVER USE for medical records in production without legal authorization.
    /// Always prefer SoftDelete for routine operations.
    /// </summary>
    public virtual void HardDelete(T entity)
        => DbSet.Remove(entity);
}