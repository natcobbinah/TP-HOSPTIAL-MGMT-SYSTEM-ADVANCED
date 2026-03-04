using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Interfaces;
using HospitalSurgical.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Infrastructure.Repositories;

public class PatientRepository : BaseRepository<Patient>, IPatientRepository
{
    private readonly SurgicalDbContext _context;

    public PatientRepository(SurgicalDbContext context) : base(context)
    {
        _context = context;
    }

    // public async Task<Patient?> GetByIdAsync(int id)
    //     => await _context.Patients.FindAsync(id);

    public async Task<Patient?> GetByFileNumberAsync(string fileNumber)
        => await _context.Patients.AsNoTracking()
            .FirstOrDefaultAsync(p => p.FileNumber == fileNumber);

    public async Task<Patient?> GetWithSurgeriesAsync(int id)
        => await _context.Patients
            .Include(p => p.Surgeries.OrderByDescending(s => s.PlannedDate))
                .ThenInclude(s => s.Surgeon)
            .Include(p => p.Surgeries)
                .ThenInclude(s => s.OperatingRoom)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);

    // public async Task<IEnumerable<Patient>> GetAllAsync()
    //     => await _context.Patients.AsNoTracking()
    //         .OrderBy(p => p.LastName).ToListAsync();

    public async Task<IEnumerable<Patient>> SearchByNameAsync(string name)
        => await _context.Patients.AsNoTracking()
            .Where(p => p.LastName.Contains(name) || p.FirstName.Contains(name))
            .OrderBy(p => p.LastName)
            .ToListAsync();

    // public async Task AddAsync(Patient patient)
    //     => await _context.Patients.AddAsync(patient);

    // public void Update(Patient patient)
    //     => _context.Patients.Update(patient);

    /// <summary>Step 4: Soft delete — marks patient as deleted.</summary>
    // public void SoftDelete(Patient patient)
    // {
    //     patient.IsDeleted = true;
    //     _context.Patients.Update(patient);
    // }

    /// <summary>Step 4: Hard delete — admin only.</summary>
    // public void HardDelete(Patient patient)
    //     => _context.Patients.Remove(patient);
}