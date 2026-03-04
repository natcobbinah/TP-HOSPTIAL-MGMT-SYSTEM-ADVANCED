using HospitalSurgical.Domain.Entities;

namespace HospitalSurgical.Domain.Interfaces;

public interface IOperatingRoomRepository : IRepository<OperatingRoom>
{
    Task<IEnumerable<OperatingRoom>> GetAvailableAsync(DateTime date, int durationMinutes);
    Task<OperatingRoom?> GetWithSurgeriesAsync(int id, DateTime date);
}