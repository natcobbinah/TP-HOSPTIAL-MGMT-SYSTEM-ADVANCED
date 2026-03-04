using HospitalSurgical.Application.DTOs;
using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSurgical.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OperatingRoomsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public OperatingRoomsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var rooms = await _unitOfWork.OperatingRooms.GetAllAsync();
        return Ok(rooms.Select(MapToDto));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var room = await _unitOfWork.OperatingRooms.GetByIdAsync(id);
        return room is null ? NotFound() : Ok(MapToDto(room));
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailable(
        [FromQuery] DateTime date, [FromQuery] int durationMinutes)
    {
        var rooms = await _unitOfWork.OperatingRooms.GetAvailableAsync(date, durationMinutes);
        return Ok(rooms.Select(MapToDto));
    }

    [HttpGet("{id:int}/schedule")]
    public async Task<IActionResult> GetSchedule(int id, [FromQuery] DateTime date)
    {
        var room = await _unitOfWork.OperatingRooms.GetWithSurgeriesAsync(id, date);
        if (room is null) return NotFound();

        return Ok(new OperatingRoomAvailabilityDto
        {
            Id = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor,
            Equipment = room.Equipment
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOperatingRoomDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var room = new OperatingRoom
        {
            RoomNumber = dto.RoomNumber,
            Floor = dto.Floor,
            Equipment = dto.Equipment
        };

        await _unitOfWork.OperatingRooms.AddAsync(room);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, MapToDto(room));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var room = await _unitOfWork.OperatingRooms.GetByIdAsync(id);
        if (room is null) return NotFound();
        _unitOfWork.OperatingRooms.SoftDelete(room);
        await _unitOfWork.SaveChangesAsync();
        return NoContent();
    }

    private static OperatingRoomDto MapToDto(OperatingRoom r) => new()
    {
        Id = r.Id, RoomNumber = r.RoomNumber,
        Floor = r.Floor, Equipment = r.Equipment,
        Status = r.Status.ToString()
    };
}