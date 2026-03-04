using HospitalSurgical.Application.DTOs;
using HospitalSurgical.Application.Services;
using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Infrastructure.Data;
using HospitalSurgical.Infrastructure.Repositories;
using HospitalSurgical.Tests.Helpers;
using HospitalSurgical.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace HospitalSurgical.Tests.Services;

/// <summary>
/// Step 6 Tests: Optimistic Concurrency
///
/// WHAT WE TEST:
/// - Simulate two users loading the same surgery simultaneously
/// - User A updates successfully → ConcurrencyStamp changes
/// - User B tries to update with the old stamp → conflict detected
///
/// OPTIMISTIC vs PESSIMISTIC LOCKING:
/// - Optimistic: No DB lock held. Conflict detected at save time (our approach).
///   Best for: Low contention, many reads, few writes. Hospital planning fits this.
/// - Pessimistic: Row is locked from read to commit. Other users wait.
///   Best for: High contention, guaranteed no conflict. Stock systems, banking.
///
/// HOW TO INFORM THE USER:
/// - Return HTTP 409 Conflict with error type "ConcurrencyConflict"
/// - Client shows: "This record was modified. Click here to reload."
/// - Client can optionally show a diff of what changed.
/// </summary>
public class ConcurrencyTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public ConcurrencyTests()
    {
        _factory = new TestDbContextFactory();
    }

    private async Task<(int surgeonId, int patientId, int roomId)> SeedBaseDataAsync()
    {
        var context = _factory.CreateContext();

        var surgeon = new Surgeon
        {
            FirstName = "Test", LastName = "Surgeon",
            Specialty = "General", LicenseNumber = "LIC-CON-001",
            HireDate = DateTime.UtcNow.AddYears(-5),
            Salary = 8000, IsActive = true, YearsOfExperience = 5
        };
        context.Staff.Add(surgeon);

        var patient = new Patient
        {
            FileNumber = "CON-PAT-001",
            FirstName = "Concurrency", LastName = "Patient",
            DateOfBirth = new DateTime(1980, 1, 1),
            ContactInfo = new ContactInfo { Phone = "+33600000000", Email = "test@test.com" },
            Address = new Address { City = "Paris", Country = "France" }
        };
        context.Patients.Add(patient);

        var room = new OperatingRoom
        {
            RoomNumber = "CON-OR-01",
            Floor = 1,
            Equipment = "Basic",
            Status = OperatingRoomStatus.Available
        };
        context.OperatingRooms.Add(room);

        await context.SaveChangesAsync();
        return (surgeon.Id, patient.Id, room.Id);
    }

    [Fact]
    public async Task ConcurrencyStamp_DetectsStaleUpdate()
    {
        // Arrange
        var (surgeonId, patientId, roomId) = await SeedBaseDataAsync();

        // User A schedules a surgery and gets back a DTO with ConcurrencyStamp
        var contextA = _factory.CreateContext();
        var uowA = new UnitOfWork(contextA);
        var serviceA = new SurgeryService(uowA);

         var surgery = await serviceA.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(7),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Concurrency test surgery",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        var originalStamp = surgery.ConcurrencyStamp;
        var staleStamp = originalStamp; // Store for later use

        // User A successfully updates → stamp changes
        var updatedByA = await serviceA.UpdateStatusAsync(surgery.Id, new UpdateSurgeryStatusDto
        {
            Status = "InProgress",
            Notes = "User A started surgery.",
            ConcurrencyStamp = originalStamp
        });

        // Verify stamp changed after User A's update
        Assert.NotEqual(originalStamp, updatedByA.ConcurrencyStamp);

        // User B (different service/context) tries to update with the OLD stamp
        var contextB = _factory.CreateContext();
        var uowB = new UnitOfWork(contextB);
        var serviceB = new SurgeryService(uowB);

        // Act — User B sends the original (now stale) stamp
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            serviceB.UpdateStatusAsync(surgery.Id, new UpdateSurgeryStatusDto
            {
                Status = "Completed",
                Notes = "User B trying to complete.",
                ConcurrencyStamp = staleStamp // ← Stale!
            }));

        // Assert
        Assert.Contains("CONCURRENCY_CONFLICT", ex.Message);
    }

    [Fact]
    public async Task ConcurrencyStamp_ChangesOnEveryUpdate()
    {
        // Arrange
        var (surgeonId, patientId, roomId) = await SeedBaseDataAsync();

        var context = _factory.CreateContext();
        var uow = new UnitOfWork(context);
        var service = new SurgeryService(uow);

        var surgery = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(3),
            EstimatedDurationMinutes = 90,
            ProcedureName = "Stamp evolution test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        var stamp1 = surgery.ConcurrencyStamp;

        // First update
        var update1 = await service.UpdateStatusAsync(surgery.Id, new UpdateSurgeryStatusDto
        {
            Status = "InProgress",
            ConcurrencyStamp = stamp1
        });
        var stamp2 = update1.ConcurrencyStamp;

        // Reload surgery to get updated concurrency stamp
        var reloadedSurgery = await service.GetByIdAsync(surgery.Id);
        var stamp3 = reloadedSurgery.ConcurrencyStamp;

        // Assert — each update produces a new unique stamp
        Assert.NotEqual(stamp1, stamp2);
        Assert.NotEqual(stamp2, stamp3);
        Assert.NotEqual(stamp1, stamp3);
    }

    [Fact]
    public async Task RescheduleAsync_DetectsConcurrencyConflict()
    {
        // Arrange
        var (surgeonId, patientId, roomId) = await SeedBaseDataAsync();

        var context = _factory.CreateContext();
        var uow = new UnitOfWork(context);
        var service = new SurgeryService(uow);

        var surgery = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(5),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Reschedule concurrency test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        var staleStamp = surgery.ConcurrencyStamp;

        // Another update changes the stamp
        await service.UpdateStatusAsync(surgery.Id, new UpdateSurgeryStatusDto
        {
            Status = "InProgress",
            ConcurrencyStamp = staleStamp
        });

        // Act — try to reschedule with stale stamp
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RescheduleAsync(surgery.Id, new RescheduleSurgeryDto
            {
                NewPlannedDate = DateTime.UtcNow.AddDays(10),
                RescheduleReason = "Patient request",
                ConcurrencyStamp = staleStamp // Stale
            }));

        // Assert
        Assert.Contains("CONCURRENCY_CONFLICT", ex.Message);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}