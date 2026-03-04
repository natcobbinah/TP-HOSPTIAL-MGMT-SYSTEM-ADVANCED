namespace HospitalSurgical.Domain.Interfaces;

/// <summary>
/// Generic repository interface.
/// All CRUD operations return entities that respect the global soft delete filter.
/// </summary>
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    void Update(T entity);
    /// <summary>Soft delete — sets IsDeleted = true.</summary>
    void SoftDelete(T entity);
    /// <summary>Hard delete — physically removes from DB. Admin only.</summary>
    void HardDelete(T entity);
}