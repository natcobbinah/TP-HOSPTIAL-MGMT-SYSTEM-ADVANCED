using HospitalSurgical.Application.DTOs;
using HospitalSurgical.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace HospitalSurgical.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SurgeriesController : ControllerBase
{
    private readonly ISurgeryService _surgeryService;

    public SurgeriesController(ISurgeryService surgeryService)
    {
        _surgeryService = surgeryService;
    }

    /// <summary>GET /api/surgeries/date/2026-03-10</summary>
    [HttpGet("date/{date:datetime}")]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var surgeries = await _surgeryService.GetByDateAsync(date);
        return Ok(surgeries);
    }

    /// <summary>GET /api/surgeries/surgeon/5</summary>
    [HttpGet("surgeon/{surgeonId:int}")]
    public async Task<IActionResult> GetBySurgeon(int surgeonId)
    {
        var surgeries = await _surgeryService.GetBySurgeonAsync(surgeonId);
        return Ok(surgeries);
    }

    /// <summary>GET /api/surgeries/room/3</summary>
    [HttpGet("room/{roomId:int}")]
    public async Task<IActionResult> GetByRoom(int roomId)
    {
        var surgeries = await _surgeryService.GetByOperatingRoomAsync(roomId);
        return Ok(surgeries);
    }

    /// <summary>GET /api/surgeries/5</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var surgery = await _surgeryService.GetByIdAsync(id);
        return surgery is null ? NotFound() : Ok(surgery);
    }

    /// <summary>POST /api/surgeries</summary>
    [HttpPost]
    public async Task<IActionResult> Schedule([FromBody] CreateSurgeryDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var surgery = await _surgeryService.ScheduleAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = surgery.Id }, surgery);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    /// <summary>PUT /api/surgeries/5/status</summary>
    [HttpPut("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSurgeryStatusDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var surgery = await _surgeryService.UpdateStatusAsync(id, dto);
            return Ok(surgery);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex)
        {
            // Step 6: Return 409 Conflict for concurrency errors
            if (ex.Message.StartsWith("CONCURRENCY_CONFLICT"))
                return Conflict(new { error = ex.Message, type = "ConcurrencyConflict" });
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>PUT /api/surgeries/5/reschedule</summary>
    [HttpPut("{id:int}/reschedule")]
    public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleSurgeryDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var surgery = await _surgeryService.RescheduleAsync(id, dto);
            return Ok(surgery);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex)
        {
            if (ex.Message.StartsWith("CONCURRENCY_CONFLICT"))
                return Conflict(new { error = ex.Message, type = "ConcurrencyConflict" });
            return Conflict(new { error = ex.Message });
        }
    }

    /// <summary>PUT /api/surgeries/5/cancel</summary>
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, [FromBody] string reason)
    {
        try
        {
            await _surgeryService.CancelAsync(id, reason);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    /// <summary>POST /api/surgeries/5/nurses</summary>
    [HttpPost("{surgeryId:int}/nurses")]
    public async Task<IActionResult> AssignNurse(int surgeryId, [FromBody] AssignNurseDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var surgery = await _surgeryService.AssignNurseAsync(surgeryId, dto);
            return Ok(surgery);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    /// <summary>DELETE /api/surgeries/5/nurses/3</summary>
    [HttpDelete("{surgeryId:int}/nurses/{nurseId:int}")]
    public async Task<IActionResult> RemoveNurse(int surgeryId, int nurseId)
    {
        try
        {
            await _surgeryService.RemoveNurseAsync(surgeryId, nurseId);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
    }
}