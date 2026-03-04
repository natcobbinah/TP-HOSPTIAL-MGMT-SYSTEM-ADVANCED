using HospitalSurgical.Domain.Entities;

namespace HospitalSurgical.Domain.Interfaces;

public interface IStaffRepository : IRepository<Staff>
{
    Task<IEnumerable<Surgeon>> GetAllSurgeonsAsync();
    Task<IEnumerable<Nurse>> GetAvailableNursesAsync(DateTime date);
    Task<Surgeon?> GetSurgeonWithPlanningAsync(int id);
}