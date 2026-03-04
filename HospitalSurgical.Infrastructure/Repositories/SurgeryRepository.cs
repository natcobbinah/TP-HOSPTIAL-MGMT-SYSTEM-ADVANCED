using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Domain.Interfaces;
using HospitalSurgical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Infrastructure.Repositories;

/// <summary>
/// Step 7: Compiled Queries are pre-compiled LINQ queries stored as static delegates.
///
/// WHEN TO USE compiled queries?
/// - Queries executed MANY times per second (hot paths)
/// - Query translation overhead is measurable (~5-10ms per query)
/// - Parameters change but query structure never changes
///
/// MEMORY IMPACT:
/// - Each compiled query is cached in memory for the lifetime of the app
/// - They are evaluated once and reused — no LINQ translation on each call
/// - For 10 compiled queries: ~few KB of overhead, worth it for hot paths
/// </summary>
public class SurgeryRepository : BaseRepository<Surgery>, ISurgeryRepository
{
    private readonly SurgicalDbContext _context;

    // ─────────────────────────────────────────────────────────────────────
    // Step 7: Compiled Queries — pre-translated at startup, fast on every call
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Find surgeries for a surgeon on a specific date — COMPILED.</summary>
    private static readonly Func<SurgicalDbContext, int, DateTime, DateTime, IQueryable<Surgery>>
    GetSurgeonSurgeriesCompiled =
    EF.CompileQuery(
        (SurgicalDbContext ctx, int surgeonId, DateTime start, DateTime end) =>
            ctx.Surgeries
                .Where(s => s.SurgeonId == surgeonId
                    && s.PlannedDate >= start
                    && s.PlannedDate < end
                    && s.Status != SurgeryStatus.Cancelled)
                .OrderBy(s => s.PlannedDate));

    /// <summary>Check for scheduling conflict — COMPILED.</summary>
    private static readonly Func<SurgicalDbContext, int, DateTime, DateTime, int, Task<bool>>
        CheckSurgeonConflictCompiled = EF.CompileAsyncQuery(
            (SurgicalDbContext ctx, int surgeonId, DateTime start, DateTime end, int excludeId) =>
                ctx.Surgeries.Any(s =>
                    s.SurgeonId == surgeonId
                    && s.Id != excludeId
                    && s.Status != SurgeryStatus.Cancelled
                    && s.PlannedDate < end
                    && s.PlannedDate.AddMinutes(s.EstimatedDurationMinutes) > start));

    /// <summary>Find surgeries by operating room for a date — COMPILED.</summary>
    private static readonly Func<SurgicalDbContext, int, DateTime, DateTime, IQueryable<Surgery>>
    GetRoomSurgeriesCompiled =
        EF.CompileQuery(
            (SurgicalDbContext ctx, int roomId, DateTime start, DateTime end) =>
                ctx.Surgeries
                    .Where(s => s.OperatingRoomId == roomId
                        && s.PlannedDate >= start
                        && s.PlannedDate < end)
                    .OrderBy(s => s.PlannedDate));

    public SurgeryRepository(SurgicalDbContext context) : base(context)
    {
        _context = context;
    }

    // public async Task<Surgery?> GetByIdAsync(int id)
    //     => await _context.Surgeries.FindAsync(id);

    public async Task<Surgery?> GetWithDetailsAsync(int id)
        => await _context.Surgeries
            .Include(s => s.Patient)
            .Include(s => s.Surgeon)
            .Include(s => s.OperatingRoom)
            .Include(s => s.NurseAssignments)
                .ThenInclude(sn => sn.Nurse)
            .AsSplitQuery()
            .FirstOrDefaultAsync(s => s.Id == id);

    // public async Task<IEnumerable<Surgery>> GetAllAsync()
    //     => await _context.Surgeries
    //         .AsNoTracking()
    //         .Include(s => s.Patient)
    //         .Include(s => s.Surgeon)
    //         .Include(s => s.OperatingRoom)
    //         .OrderBy(s => s.PlannedDate)
    //         .ToListAsync();

    public async Task<IEnumerable<Surgery>> GetByDateAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1);
        var result = new List<Surgery>();

        var surgeries = await GetRoomSurgeriesCompiled(_context, 0, start, end)
         .ToListAsync();

        result.AddRange(surgeries);

        // Fallback to standard LINQ when room not specified
        return await _context.Surgeries
            .AsNoTracking()
            .Where(s => s.PlannedDate.Date == date.Date)
            .Include(s => s.Surgeon)
            .Include(s => s.OperatingRoom)
            .OrderBy(s => s.PlannedDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Surgery>> GetBySurgeonAsync(int surgeonId)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(30); // Next 30 days
        var result = new List<Surgery>();

        return await GetSurgeonSurgeriesCompiled(_context, surgeonId, start, end)
    .ToListAsync();
    }

    public async Task<IEnumerable<Surgery>> GetByOperatingRoomAsync(int roomId)
    {
        var start = DateTime.UtcNow.Date;
        var end = start.AddDays(1);
        var result = new List<Surgery>();

        var surgeries = await GetRoomSurgeriesCompiled(_context, roomId, start, end)
            .ToListAsync();

        result.AddRange(surgeries);

        return result;
    }

    /// <summary>
    /// Step 7: Uses compiled query for O(1) conflict detection.
    /// Called on every surgery scheduling — must be as fast as possible.
    /// </summary>
    public async Task<bool> HasConflictAsync(
        int surgeonId, DateTime start, DateTime end, int? excludeSurgeryId = null)
        => await CheckSurgeonConflictCompiled(
            _context, surgeonId, start, end, excludeSurgeryId ?? 0);

    public async Task<bool> RoomHasConflictAsync(
        int roomId, DateTime start, DateTime end, int? excludeSurgeryId = null)
        => await _context.Surgeries.AnyAsync(s =>
            s.OperatingRoomId == roomId
            && s.Id != (excludeSurgeryId ?? 0)
            && s.Status != SurgeryStatus.Cancelled
            && s.PlannedDate < end
            && s.PlannedDate.AddMinutes(s.EstimatedDurationMinutes) > start);

    // public async Task AddAsync(Surgery surgery)
    //     => await _context.Surgeries.AddAsync(surgery);

    // public void Update(Surgery surgery)
    //     => _context.Surgeries.Update(surgery);

    /// <summary>Step 4: Soft delete — marks as deleted, keeps in DB.</summary>
    // public void SoftDelete(Surgery surgery)
    // {
    //     surgery.IsDeleted = true;
    //     _context.Surgeries.Update(surgery);
    // }

    /// <summary>Step 4: Hard delete — physical removal. Admin only.</summary>
    // public void HardDelete(Surgery surgery)
    //     => _context.Surgeries.Remove(surgery);

    /// <summary>
    /// Step 4: Retrieves soft-deleted surgeries.
    /// IgnoreQueryFilters() bypasses the global soft-delete filter.
    /// Only accessible to admin roles.
    /// </summary>
    public async Task<IEnumerable<Surgery>> GetDeletedAsync()
        => await _context.Surgeries
            .IgnoreQueryFilters()        // ← Bypass the global filter
            .Where(s => s.IsDeleted)    // ← Only show deleted ones
            .AsNoTracking()
            .ToListAsync();
}