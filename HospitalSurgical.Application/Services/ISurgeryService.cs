using HospitalSurgical.Application.DTOs;

namespace HospitalSurgical.Application.Services;

public interface ISurgeryService
{
    Task<SurgeryDto> ScheduleAsync(CreateSurgeryDto dto);
    Task<SurgeryDto> UpdateStatusAsync(int id, UpdateSurgeryStatusDto dto);
    Task<SurgeryDto> RescheduleAsync(int id, RescheduleSurgeryDto dto);
    Task CancelAsync(int id, string reason);
    Task<SurgeryDto?> GetByIdAsync(int id);
    Task<IEnumerable<SurgeryDto>> GetByDateAsync(DateTime date);
    Task<IEnumerable<SurgeryDto>> GetBySurgeonAsync(int surgeonId);
    Task<IEnumerable<SurgeryDto>> GetByOperatingRoomAsync(int roomId);
    Task<SurgeryDto> AssignNurseAsync(int surgeryId, AssignNurseDto dto);
    Task RemoveNurseAsync(int surgeryId, int nurseId);
}