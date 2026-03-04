using Microsoft.EntityFrameworkCore.Storage;

namespace HospitalSurgical.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IPatientRepository Patients { get; }
    ISurgeryRepository Surgeries { get; }
    IOperatingRoomRepository OperatingRooms { get; }
    IStaffRepository Staff { get; }

    Task<int> SaveChangesAsync();

    // Step 8: Explicit transaction support
    ValueTask<IDbContextTransaction> BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}