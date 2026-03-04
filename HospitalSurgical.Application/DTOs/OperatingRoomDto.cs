using System.ComponentModel.DataAnnotations;

namespace HospitalSurgical.Application.DTOs;

public class OperatingRoomDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string Equipment { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class CreateOperatingRoomDto
{
    [Required][MaxLength(20)]
    public string RoomNumber { get; set; } = string.Empty;

    [Range(0, 50)]
    public int Floor { get; set; }

    [MaxLength(500)]
    public string Equipment { get; set; } = string.Empty;
}

public class OperatingRoomAvailabilityDto
{
    public int Id { get; set; }
    public string RoomNumber { get; set; } = string.Empty;
    public int Floor { get; set; }
    public string Equipment { get; set; } = string.Empty;
    public List<SurgeryDto> ScheduledSurgeries { get; set; } = new();
}