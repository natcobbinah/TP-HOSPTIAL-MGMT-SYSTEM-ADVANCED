using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Domain.Interfaces;
using HospitalSurgical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Infrastructure.Repositories;

public class OperatingRoomRepository : BaseRepository<OperatingRoom>, IOperatingRoomRepository
{
    private readonly SurgicalDbContext _context;

    public OperatingRoomRepository(SurgicalDbContext context) : base(context)
    {
        _context = context;
    }

    // public async Task<OperatingRoom?> GetByIdAsync(int id)
    //     => await _context.OperatingRooms.FindAsync(id);

    // public async Task<IEnumerable<OperatingRoom>> GetAllAsync()
    //     => await _context.OperatingRooms.AsNoTracking()
    //         .OrderBy(r => r.Floor).ThenBy(r => r.RoomNumber)
    //         .ToListAsync();

    /// <summary>
    /// Finds rooms with no conflicting surgeries in the given time window.
    /// </summary>
    public async Task<IEnumerable<OperatingRoom>> GetAvailableAsync(DateTime date, int durationMinutes)
    {
        var endTime = date.AddMinutes(durationMinutes);

        return await _context.OperatingRooms
            .AsNoTracking()
            .Where(r => r.Status != OperatingRoomStatus.UnderMaintenance)
            .Where(r => !r.Surgeries.Any(s =>
                s.Status != SurgeryStatus.Cancelled &&
                s.PlannedDate < endTime &&
                s.PlannedDate.AddMinutes(s.EstimatedDurationMinutes) > date))
            .OrderBy(r => r.Floor)
            .ToListAsync();
    }

    public async Task<OperatingRoom?> GetWithSurgeriesAsync(int id, DateTime date)
        => await _context.OperatingRooms
            .Include(r => r.Surgeries
                .Where(s => s.PlannedDate.Date == date.Date)
                .OrderBy(s => s.PlannedDate))
                .ThenInclude(s => s.Surgeon)
            .FirstOrDefaultAsync(r => r.Id == id);

    // public async Task AddAsync(OperatingRoom room)
    //     => await _context.OperatingRooms.AddAsync(room);

    // public void Update(OperatingRoom room)
    //     => _context.OperatingRooms.Update(room);

    // public void SoftDelete(OperatingRoom room)
    // {
    //     room.IsDeleted = true;
    //     _context.OperatingRooms.Update(room);
    // }

    // public void HardDelete(OperatingRoom room)
    //     => _context.OperatingRooms.Remove(room);
}