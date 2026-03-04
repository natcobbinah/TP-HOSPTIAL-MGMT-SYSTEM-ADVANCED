using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Domain.Interfaces;
using HospitalSurgical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Infrastructure.Repositories;

public class StaffRepository : BaseRepository<Staff>, IStaffRepository
{
    private readonly SurgicalDbContext _context;

    public StaffRepository(SurgicalDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Step 1: Query a specific type using OfType<T>()
    /// Generated SQL: SELECT ... FROM Staff WHERE StaffType = 'Surgeon'
    /// </summary>
    public async Task<IEnumerable<Surgeon>> GetAllSurgeonsAsync()
        => await _context.Staff.OfType<Surgeon>()
            .AsNoTracking()
            .OrderBy(s => s.LastName)
            .ToListAsync();

    public async Task<IEnumerable<Nurse>> GetAvailableNursesAsync(DateTime date)
        => await _context.Staff.OfType<Nurse>()
            .AsNoTracking()
            .Where(n => n.IsActive)
            .Where(n => !n.SurgeryAssignments.Any(sn =>
                sn.Surgery.PlannedDate.Date == date.Date &&
                sn.Surgery.Status != SurgeryStatus.Cancelled))
            .OrderBy(n => n.LastName)
            .ToListAsync();

    public async Task<Surgeon?> GetSurgeonWithPlanningAsync(int id)
        => await _context.Staff.OfType<Surgeon>()
            .Include(s => s.Surgeries
                .Where(sur => sur.PlannedDate >= DateTime.UtcNow
                    && sur.Status != SurgeryStatus.Cancelled)
                .OrderBy(sur => sur.PlannedDate))
                .ThenInclude(sur => sur.OperatingRoom)
            .FirstOrDefaultAsync(s => s.Id == id);

    // public async Task<Staff?> GetByIdAsync(int id)
    //     => await _context.Staff.FindAsync(id);

    // public async Task<IEnumerable<Staff>> GetAllAsync()
    //     => await _context.Staff.AsNoTracking()
    //         .OrderBy(s => s.LastName)
    //         .ToListAsync();

    // public async Task AddAsync(Staff staff)
    //     => await _context.Staff.AddAsync(staff);

    // public void Update(Staff staff)
    //     => _context.Staff.Update(staff);

    // public void SoftDelete(Staff staff)
    // {
    //     staff.IsDeleted = true;
    //     _context.Staff.Update(staff);
    // }

    // public void HardDelete(Staff staff)
    //     => _context.Staff.Remove(staff);
}