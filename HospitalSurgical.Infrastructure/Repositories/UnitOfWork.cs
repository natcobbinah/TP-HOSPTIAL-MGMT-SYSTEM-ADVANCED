using HospitalSurgical.Domain.Interfaces;
using HospitalSurgical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace HospitalSurgical.Infrastructure.Repositories;

/// <summary>
/// Step 8: Unit of Work implementation.
///
/// WHEN TO USE explicit transactions?
/// - Multiple repository operations that must succeed or fail together
/// - Creating a surgery involves: checking availability + creating surgery + updating room status
///   → If any step fails, ALL must roll back
///
/// DISTRIBUTED TRANSACTIONS:
/// For operations spanning multiple databases or services:
/// - Use the Outbox Pattern (store events in DB, process asynchronously)
/// - Or use Saga Pattern for long-running distributed workflows
/// - EF Core's IDbContextTransaction only covers a single DbContext/database
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly SurgicalDbContext _context;
    private IDbContextTransaction? _currentTransaction;

    private IPatientRepository? _patients;
    private ISurgeryRepository? _surgeries;
    private IOperatingRoomRepository? _operatingRooms;
    private IStaffRepository? _staff;

    public UnitOfWork(SurgicalDbContext context)
    {
        _context = context;
    }

    public IPatientRepository Patients
        => _patients ??= new PatientRepository(_context);

    public ISurgeryRepository Surgeries
        => _surgeries ??= new SurgeryRepository(_context);

    public IOperatingRoomRepository OperatingRooms
        => _operatingRooms ??= new OperatingRoomRepository(_context);

    public IStaffRepository Staff
        => _staff ??= new StaffRepository(_context);

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();

    public async ValueTask<IDbContextTransaction> BeginTransactionAsync()
    {
        _currentTransaction = await _context.Database.BeginTransactionAsync();
        return _currentTransaction;
    }

    public async Task CommitAsync()
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        try
        {
            await _context.SaveChangesAsync();
            await _currentTransaction.CommitAsync();
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync()
    {
        if (_currentTransaction is null) return;

        try
        {
            await _currentTransaction.RollbackAsync();
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public void Dispose()
    {
        _currentTransaction?.Dispose();
        _context.Dispose();
    }
}