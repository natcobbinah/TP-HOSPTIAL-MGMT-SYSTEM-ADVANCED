using HospitalSurgical.Domain.Entities;

namespace HospitalSurgical.Domain.Interfaces;

public interface ISurgeryRepository : IRepository<Surgery>
{
    Task<Surgery?> GetWithDetailsAsync(int id);
    Task<IEnumerable<Surgery>> GetByDateAsync(DateTime date);
    Task<IEnumerable<Surgery>> GetBySurgeonAsync(int surgeonId);
    Task<IEnumerable<Surgery>> GetByOperatingRoomAsync(int roomId);
    Task<bool> HasConflictAsync(int surgeonId, DateTime start, DateTime end, int? excludeSurgeryId = null);
    Task<bool> RoomHasConflictAsync(int roomId, DateTime start, DateTime end, int? excludeSurgeryId = null);
    // Step 4: Recover soft-deleted surgeries (admin only)
    Task<IEnumerable<Surgery>> GetDeletedAsync();
}