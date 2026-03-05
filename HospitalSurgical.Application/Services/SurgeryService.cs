using HospitalSurgical.Application.DTOs;
using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Application.Services;

/// <summary>
/// Step 8: Surgery scheduling uses explicit transactions to ensure atomicity.
/// Creating a surgery requires:
///   1. Validate patient exists
///   2. Validate surgeon exists and has no conflict
///   3. Validate operating room is available
///   4. Create surgery entity
///   5. Assign nurses (optional)
///   6. Update operating room status
/// If any step fails → rollback ALL changes.
/// </summary>
public class SurgeryService : ISurgeryService
{
    private readonly IUnitOfWork _unitOfWork;

    public SurgeryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SurgeryDto> ScheduleAsync(CreateSurgeryDto dto)
    {
        // Validate date is in the future
        if (dto.PlannedDate <= DateTime.UtcNow)
            throw new ArgumentException("Surgery must be scheduled in the future.");

        var endTime = dto.PlannedDate.AddMinutes(dto.EstimatedDurationMinutes);

        // Step 8: Begin explicit transaction
        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            // 1. Validate patient
            var patient = await _unitOfWork.Patients.GetByIdAsync(dto.PatientId)
                ?? throw new KeyNotFoundException($"Patient {dto.PatientId} not found.");

            // 2. Validate surgeon and check for scheduling conflict
            var surgeon = await _unitOfWork.Staff.GetSurgeonWithPlanningAsync(dto.SurgeonId)
                ?? throw new KeyNotFoundException($"Surgeon {dto.SurgeonId} not found.");

            if (await _unitOfWork.Surgeries.HasConflictAsync(dto.SurgeonId, dto.PlannedDate, endTime))
                throw new InvalidOperationException(
                    $"Scheduling conflict: {surgeon.FullName} already has a surgery during this time slot.");

            // 3. Validate operating room
            var room = await _unitOfWork.OperatingRooms.GetByIdAsync(dto.OperatingRoomId)
                ?? throw new KeyNotFoundException($"Operating room {dto.OperatingRoomId} not found.");

            if (room.Status == OperatingRoomStatus.UnderMaintenance)
                throw new InvalidOperationException(
                    $"Scheduling conflict: Operating room {room.RoomNumber} already has a surgery during this time slot.");

            if (await _unitOfWork.Surgeries.RoomHasConflictAsync(dto.OperatingRoomId, dto.PlannedDate, endTime))
                throw new InvalidOperationException(
                    $"Scheduling conflict: Operating room {room.RoomNumber} already has a surgery during this time slot.");

            // 4. Create surgery
            var surgery = new Surgery
            {
                PlannedDate = dto.PlannedDate,
                EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                ProcedureName = dto.ProcedureName,
                Notes = dto.Notes,
                Status = SurgeryStatus.Planned,
                PatientId = dto.PatientId,
                SurgeonId = dto.SurgeonId,
                OperatingRoomId = dto.OperatingRoomId,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            await _unitOfWork.Surgeries.AddAsync(surgery);
            await _unitOfWork.SaveChangesAsync(); // Get generated ID

            // 5. Assign nurses
            foreach (var nurseDto in dto.Nurses)
            {
                var nurse = await _unitOfWork.Staff.GetByIdAsync(nurseDto.NurseId) as Domain.Entities.Nurse
                    ?? throw new KeyNotFoundException($"Nurse {nurseDto.NurseId} not found.");

                surgery.NurseAssignments.Add(new SurgeryNurse
                {
                    SurgeryId = surgery.Id,
                    NurseId = nurseDto.NurseId,
                    RoleDuringSurgery = nurseDto.RoleDuringSurgery,
                    IsScrubNurse = nurseDto.IsScrubNurse,
                    AssignedAt = DateTime.UtcNow
                });
            }

            // 6. Update room status
            room.Status = OperatingRoomStatus.Occupied;
            _unitOfWork.OperatingRooms.Update(room);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();

            return MapToDto(surgery, patient, surgeon, room);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Step 6: Optimistic concurrency — detect if another user modified the surgery.
    /// The client must pass back the ConcurrencyStamp it last received.
    /// </summary>
    public async Task<SurgeryDto> UpdateStatusAsync(int id, UpdateSurgeryStatusDto dto)
    {
        var surgery = await _unitOfWork.Surgeries.GetWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Surgery {id} not found.");

        // 1️⃣ Validate status input first
        if (!Enum.TryParse<SurgeryStatus>(dto.Status, ignoreCase: true, out var newStatus))
            throw new ArgumentException($"Invalid status '{dto.Status}'.");

        // 2️⃣ Business rule validation
        if (surgery.Status == SurgeryStatus.Completed)
            throw new InvalidOperationException("Cannot modify a completed surgery.");

        // 3️⃣ Optimistic concurrency check
        if (!string.Equals(surgery.ConcurrencyStamp, dto.ConcurrencyStamp, StringComparison.Ordinal))
            throw new InvalidOperationException(
                "CONCURRENCY_CONFLICT: This surgery was modified by another user.");

        // 4️⃣ Apply changes
        surgery.Status = newStatus;

        if (!string.IsNullOrWhiteSpace(dto.Notes))
            surgery.Notes = dto.Notes;

        // 5️⃣ Generate new concurrency stamp for every update
        var newStamp = Guid.NewGuid().ToString();
        surgery.ConcurrencyStamp = newStamp;

        // 6️⃣ Free room if surgery finished
        if (newStatus is SurgeryStatus.Completed or SurgeryStatus.Cancelled)
        {
            var room = await _unitOfWork.OperatingRooms.GetByIdAsync(surgery.OperatingRoomId);
            if (room != null)
            {
                room.Status = OperatingRoomStatus.Available;
                _unitOfWork.OperatingRooms.Update(room);
            }
        }

        _unitOfWork.Surgeries.Update(surgery);

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException(
                "CONCURRENCY_CONFLICT: Surgery was updated simultaneously.");
        }

        return MapToDto(surgery, surgery.Patient, surgery.Surgeon as Surgeon, surgery.OperatingRoom);
    }

    public async Task<SurgeryDto> RescheduleAsync(int id, RescheduleSurgeryDto dto)
    {
        var surgery = await _unitOfWork.Surgeries.GetWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Surgery {id} not found.");

        if (!string.IsNullOrWhiteSpace(dto.ConcurrencyStamp) &&
                surgery.ConcurrencyStamp != dto.ConcurrencyStamp)
            throw new InvalidOperationException(
                "CONCURRENCY_CONFLICT: Surgery was modified. Please reload.");

        if (surgery.Status != SurgeryStatus.Planned)
            throw new InvalidOperationException("Only planned surgeries can be rescheduled.");

        if (dto.NewPlannedDate <= DateTime.UtcNow)
            throw new ArgumentException("New date must be in the future.");

        var newEndTime = dto.NewPlannedDate.AddMinutes(surgery.EstimatedDurationMinutes);
        var roomId = dto.NewOperatingRoomId ?? surgery.OperatingRoomId;

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // Check surgeon availability for new time
            if (await _unitOfWork.Surgeries.HasConflictAsync(
                surgery.SurgeonId, dto.NewPlannedDate, newEndTime, id))
                throw new InvalidOperationException("Surgeon has a conflict at the new time.");

            // Check room availability
            if (await _unitOfWork.Surgeries.RoomHasConflictAsync(
                roomId, dto.NewPlannedDate, newEndTime, id))
                throw new InvalidOperationException("Operating room has a conflict at the new time.");

            // Free old room if changing rooms
            if (dto.NewOperatingRoomId.HasValue && dto.NewOperatingRoomId.Value != surgery.OperatingRoomId)
            {
                var oldRoom = await _unitOfWork.OperatingRooms.GetByIdAsync(surgery.OperatingRoomId);
                if (oldRoom != null)
                {
                    oldRoom.Status = OperatingRoomStatus.Available;
                    _unitOfWork.OperatingRooms.Update(oldRoom);
                }

                var newRoom = await _unitOfWork.OperatingRooms.GetByIdAsync(dto.NewOperatingRoomId.Value);
                if (newRoom != null)
                {
                    newRoom.Status = OperatingRoomStatus.Occupied;
                    _unitOfWork.OperatingRooms.Update(newRoom);
                }

                surgery.OperatingRoomId = dto.NewOperatingRoomId.Value;
            }

            surgery.PlannedDate = dto.NewPlannedDate;
            if (!string.IsNullOrWhiteSpace(dto.RescheduleReason))
                surgery.Notes = dto.RescheduleReason;

            _unitOfWork.Surgeries.Update(surgery);
            await _unitOfWork.CommitAsync();

            return MapToDto(surgery, surgery.Patient, surgery.Surgeon as Surgeon, surgery.OperatingRoom);
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task CancelAsync(int id, string reason)
    {
        var surgery = await _unitOfWork.Surgeries.GetWithDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Surgery {id} not found.");

        if (surgery.Status == SurgeryStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed surgery.");

        if (surgery.Status == SurgeryStatus.Cancelled)
            throw new InvalidOperationException("Surgery is already cancelled.");

        await using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            surgery.Status = SurgeryStatus.Cancelled;
            surgery.Notes = reason;
            _unitOfWork.Surgeries.Update(surgery);

            // Free the operating room
            var room = await _unitOfWork.OperatingRooms.GetByIdAsync(surgery.OperatingRoomId);
            if (room != null)
            {
                room.Status = OperatingRoomStatus.Available;
                _unitOfWork.OperatingRooms.Update(room);
            }

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<SurgeryDto?> GetByIdAsync(int id)
    {
        var surgery = await _unitOfWork.Surgeries.GetWithDetailsAsync(id);
        if (surgery is null) return null;
        return MapToDto(surgery, surgery.Patient, surgery.Surgeon as Surgeon, surgery.OperatingRoom);
    }

    public async Task<IEnumerable<SurgeryDto>> GetByDateAsync(DateTime date)
    {
        var surgeries = await _unitOfWork.Surgeries.GetByDateAsync(date);
        return surgeries.Select(s => MapToDto(s, s.Patient, s.Surgeon as Surgeon, s.OperatingRoom));
    }

    public async Task<IEnumerable<SurgeryDto>> GetBySurgeonAsync(int surgeonId)
    {
        var surgeries = await _unitOfWork.Surgeries.GetBySurgeonAsync(surgeonId);
        return surgeries.Select(s => MapToDto(s, s.Patient, s.Surgeon as Surgeon, s.OperatingRoom));
    }

    public async Task<IEnumerable<SurgeryDto>> GetByOperatingRoomAsync(int roomId)
    {
        var surgeries = await _unitOfWork.Surgeries.GetByOperatingRoomAsync(roomId);
        return surgeries.Select(s => MapToDto(s, s.Patient, s.Surgeon as Surgeon, s.OperatingRoom));
    }

    public async Task<SurgeryDto> AssignNurseAsync(int surgeryId, AssignNurseDto dto)
    {
        var surgery = await _unitOfWork.Surgeries.GetWithDetailsAsync(surgeryId)
            ?? throw new KeyNotFoundException($"Surgery {surgeryId} not found.");

        if (surgery.NurseAssignments.Any(sn => sn.NurseId == dto.NurseId))
            throw new InvalidOperationException("This nurse is already assigned to this surgery.");

        surgery.NurseAssignments.Add(new SurgeryNurse
        {
            SurgeryId = surgeryId,
            NurseId = dto.NurseId,
            RoleDuringSurgery = dto.RoleDuringSurgery,
            IsScrubNurse = dto.IsScrubNurse,
            AssignedAt = DateTime.UtcNow
        });

        _unitOfWork.Surgeries.Update(surgery);
        await _unitOfWork.SaveChangesAsync();

        return MapToDto(surgery, surgery.Patient, surgery.Surgeon as Surgeon, surgery.OperatingRoom);
    }

    public async Task RemoveNurseAsync(int surgeryId, int nurseId)
    {
        var surgery = await _unitOfWork.Surgeries.GetWithDetailsAsync(surgeryId)
            ?? throw new KeyNotFoundException($"Surgery {surgeryId} not found.");

        var assignment = surgery.NurseAssignments.FirstOrDefault(sn => sn.NurseId == nurseId)
            ?? throw new KeyNotFoundException($"Nurse {nurseId} is not assigned to surgery {surgeryId}.");

        surgery.NurseAssignments.Remove(assignment);
        _unitOfWork.Surgeries.Update(surgery);
        await _unitOfWork.SaveChangesAsync();
    }

    // ──────────────────────────────────────────────
    // Private helper: Entity → DTO mapping
    // ──────────────────────────────────────────────
    private static SurgeryDto MapToDto(
        Surgery surgery, Patient? patient, Surgeon? surgeon, OperatingRoom? room)
    {
        return new SurgeryDto
        {
            Id = surgery.Id,
            PlannedDate = surgery.PlannedDate,
            PlannedEndTime = surgery.PlannedEndTime,
            EstimatedDurationMinutes = surgery.EstimatedDurationMinutes,
            Status = surgery.Status.ToString(),
            ProcedureName = surgery.ProcedureName,
            Notes = surgery.Notes,
            ConcurrencyStamp = surgery.ConcurrencyStamp,
            PatientId = surgery.PatientId,
            PatientName = patient is not null
                ? $"{patient.FirstName} {patient.LastName}" : "Unknown",
            SurgeonId = surgery.SurgeonId,
            SurgeonName = surgeon is not null
                ? $"Dr. {surgeon.FirstName} {surgeon.LastName}" : "Unknown",
            OperatingRoomId = surgery.OperatingRoomId,
            RoomNumber = room?.RoomNumber ?? "Unknown",
            Nurses = surgery.NurseAssignments.Select(sn => new NurseAssignmentDto
            {
                NurseId = sn.NurseId,
                NurseName = sn.Nurse is not null
                    ? $"{sn.Nurse.FirstName} {sn.Nurse.LastName}" : "Unknown",
                RoleDuringSurgery = sn.RoleDuringSurgery,
                IsScrubNurse = sn.IsScrubNurse
            }).ToList()
        };
    }
}