using HospitalSurgical.Domain.Entities;

namespace HospitalSurgical.Domain.Interfaces;

public interface IPatientRepository : IRepository<Patient>
{
    Task<Patient?> GetByFileNumberAsync(string fileNumber);
    Task<IEnumerable<Patient>> SearchByNameAsync(string name);
    Task<Patient?> GetWithSurgeriesAsync(int id);
}