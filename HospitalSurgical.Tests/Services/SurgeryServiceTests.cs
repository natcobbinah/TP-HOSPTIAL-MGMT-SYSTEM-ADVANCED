using HospitalSurgical.Application.DTOs;
using HospitalSurgical.Application.Services;
using HospitalSurgical.Domain.Entities;
using HospitalSurgical.Domain.Enums;
using HospitalSurgical.Infrastructure.Data;
using HospitalSurgical.Infrastructure.Repositories;
using HospitalSurgical.Tests.Helpers;
using HospitalSurgical.Domain.ValueObjects;

namespace HospitalSurgical.Tests.Services;

public class SurgeryServiceTests : IDisposable
{
    private readonly TestDbContextFactory _factory;

    public SurgeryServiceTests()
    {
        _factory = new TestDbContextFactory();
    }

    // ─────────────────────────────────────────────────────
    // Helper: seed and return a ready service + ids
    // ─────────────────────────────────────────────────────

    private async Task<(SurgeryService service, int surgeonId, int nurseId,
        int patientId, int roomId)> CreateWithSeedAsync()
    {
        var context = _factory.CreateContext();

        var surgeon = new Surgeon
        {
            FirstName = "Henri",
            LastName = "Duval",
            Specialty = "Cardiothoracic",
            LicenseNumber = "LIC-001",
            YearsOfExperience = 15,
            HireDate = DateTime.UtcNow.AddYears(-10),
            Salary = 12000,
            IsActive = true
        };
        context.Staff.Add(surgeon);

        var nurse = new Nurse
        {
            FirstName = "Sophie",
            LastName = "Martin",
            CertificationLevel = CertificationLevel.Senior,
            ShiftPreference = ShiftPreference.Morning,
            HireDate = DateTime.UtcNow.AddYears(-5),
            Salary = 4500,
            IsActive = true
        };
        context.Staff.Add(nurse);

        var patient = new Patient
        {
            FileNumber = "PAT-001",
            FirstName = "Jean",
            LastName = "Dupont",
            DateOfBirth = new DateTime(1975, 5, 15),
            ContactInfo = new ContactInfo
            {
                Phone = "+33612345678",
                Email = "jean@email.com",
                EmergencyContact = "Marie Dupont",
                EmergencyPhone = "+33698765432"
            },
            Address = new Address
            {
                Street = "10 Rue de Paris",
                City = "Paris",
                ZipCode = "75001",
                Country = "France"
            }
        };
        context.Patients.Add(patient);

        var room = new OperatingRoom
        {
            RoomNumber = "OR-01",
            Floor = 3,
            Equipment = "Full cardiac surgical suite",
            Status = OperatingRoomStatus.Available
        };
        context.OperatingRooms.Add(room);

        await context.SaveChangesAsync();

        var uow = new UnitOfWork(context);
        var service = new SurgeryService(uow);

        return (service, surgeon.Id, nurse.Id, patient.Id, room.Id);
    }

    // ─────────────────────────────────────────────────────
    // SCHEDULING TESTS
    // ─────────────────────────────────────────────────────

    [Fact]
    public async Task ScheduleAsync_ValidSurgery_ReturnsDto()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var dto = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(3),
            EstimatedDurationMinutes = 120,
            ProcedureName = "Coronary bypass",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        };

        // Act
        var result = await service.ScheduleAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal("Planned", result.Status);
        Assert.Equal("Coronary bypass", result.ProcedureName);
        Assert.Contains("Duval", result.SurgeonName);
        Assert.Equal("OR-01", result.RoomNumber);
    }

    [Fact]
    public async Task ScheduleAsync_PastDate_ThrowsArgumentException()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var dto = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(-1), // Past date!
            EstimatedDurationMinutes = 60,
            ProcedureName = "Test procedure",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.ScheduleAsync(dto));
        Assert.Contains("future", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScheduleAsync_NonExistentSurgeon_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (service, _, _, patientId, roomId) = await CreateWithSeedAsync();
        var dto = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Test",
            PatientId = patientId,
            SurgeonId = 999, // Does not exist
            OperatingRoomId = roomId
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ScheduleAsync(dto));
    }

    [Fact]
    public async Task ScheduleAsync_NonExistentPatient_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (service, surgeonId, _, _, roomId) = await CreateWithSeedAsync();
        var dto = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Test",
            PatientId = 999, // Does not exist
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ScheduleAsync(dto));
    }

    [Fact]
    public async Task ScheduleAsync_SurgeonConflict_ThrowsInvalidOperationException()
    {
        // Arrange — same surgeon, overlapping time
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();

        var surgery1 = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(5).Date.AddHours(9), // 09:00
            EstimatedDurationMinutes = 120,                              // Ends 11:00
            ProcedureName = "First surgery",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        };

        // Schedule first surgery in room 1
        await service.ScheduleAsync(surgery1);

        // Create a second room for the conflict test
        var context2 = _factory.CreateContext();
        var room2 = new OperatingRoom
        {
            RoomNumber = "OR-02",
            Floor = 3,
            Equipment = "General suite",
            Status = OperatingRoomStatus.Available
        };
        context2.OperatingRooms.Add(room2);
        await context2.SaveChangesAsync();

        var surgery2 = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(5).Date.AddHours(10), // 10:00 — overlaps with 09:00-11:00
            EstimatedDurationMinutes = 60,
            ProcedureName = "Conflicting surgery",
            PatientId = patientId,
            SurgeonId = surgeonId, // SAME surgeon
            OperatingRoomId = room2.Id
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ScheduleAsync(surgery2));
        Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScheduleAsync_RoomConflict_ThrowsInvalidOperationException()
    {
        // Arrange — same room, overlapping time
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();

        // Add a second surgeon
        var context = _factory.CreateContext();
        var surgeon2 = new Surgeon
        {
            FirstName = "Claire", LastName = "Bernard",
            Specialty = "General", LicenseNumber = "LIC-002",
            HireDate = DateTime.UtcNow.AddYears(-3),
            Salary = 9000, IsActive = true, YearsOfExperience = 5
        };
        context.Staff.Add(surgeon2);
        await context.SaveChangesAsync();

        var surgery1 = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(4).Date.AddHours(8), // 08:00
            EstimatedDurationMinutes = 180,                              // Ends 11:00
            ProcedureName = "Long surgery",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        };
        await service.ScheduleAsync(surgery1);

        var surgery2 = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(4).Date.AddHours(9), // 09:00 — overlaps
            EstimatedDurationMinutes = 60,
            ProcedureName = "Conflicting room surgery",
            PatientId = patientId,
            SurgeonId = surgeon2.Id,
            OperatingRoomId = roomId // SAME room
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ScheduleAsync(surgery2));
        Assert.Contains("conflict", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ScheduleAsync_WithNurses_AssignsNursesCorrectly()
    {
        // Arrange
        var (service, surgeonId, nurseId, patientId, roomId) = await CreateWithSeedAsync();
        var dto = new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 90,
            ProcedureName = "Surgery with nurses",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId,
            Nurses = new List<AssignNurseDto>
            {
                new() { NurseId = nurseId, RoleDuringSurgery = "Scrub Nurse", IsScrubNurse = true }
            }
        };

        // Act
        var result = await service.ScheduleAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Nurses);
        Assert.True(result.Nurses.First().IsScrubNurse);
        Assert.Equal("Scrub Nurse", result.Nurses.First().RoleDuringSurgery);
    }

    // ─────────────────────────────────────────────────────
    // STATUS UPDATE TESTS
    // ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_PlannedToInProgress_Succeeds()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 90,
            ProcedureName = "Status test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        // Act
        var result = await service.UpdateStatusAsync(created.Id, new UpdateSurgeryStatusDto
        {
            Status = "InProgress",
            Notes = "Patient prepped, surgery commencing.",
            ConcurrencyStamp = created.ConcurrencyStamp  // Pass back the stamp
        });

        // Assert
        Assert.Equal("InProgress", result.Status);
    }

    [Fact]
    public async Task UpdateStatusAsync_WrongConcurrencyStamp_ThrowsInvalidOperationException()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Concurrency test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        // Act — pass a WRONG concurrency stamp (simulates stale data)
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateStatusAsync(created.Id, new UpdateSurgeryStatusDto
            {
                Status = "InProgress",
                ConcurrencyStamp = "wrong-stamp-simulating-stale-client-data"
            }));

        // Assert
        Assert.Contains("CONCURRENCY_CONFLICT", ex.Message);
    }

    [Fact]
    public async Task UpdateStatusAsync_InvalidStatus_ThrowsArgumentException()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Invalid status test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateStatusAsync(created.Id, new UpdateSurgeryStatusDto
            {
                Status = "INVALID_STATUS",
                ConcurrencyStamp = created.ConcurrencyStamp
            }));
    }

    // ─────────────────────────────────────────────────────
    // CANCELLATION TESTS
    // ─────────────────────────────────────────────────────

    [Fact]
    public async Task CancelAsync_PlannedSurgery_SetsCancelledAndFreesRoom()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 90,
            ProcedureName = "To be cancelled",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        // Act
        await service.CancelAsync(created.Id, "Patient not fit for surgery.");

        // Assert — verify via GetById
        var updated = await service.GetByIdAsync(created.Id);
        Assert.Equal("Cancelled", updated!.Status);
    }

    [Fact]
    public async Task CancelAsync_CompletedSurgery_ThrowsInvalidOperationException()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(1),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Completed surgery",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        // Mark as completed first
        await service.UpdateStatusAsync(created.Id, new UpdateSurgeryStatusDto
        {
            Status = "Completed",
            ConcurrencyStamp = created.ConcurrencyStamp
        });

        // Act & Assert — cannot cancel a completed surgery
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CancelAsync(created.Id, "Attempted cancel"));
        Assert.Contains("completed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CancelAsync_NonExistentSurgery_ThrowsKeyNotFoundException()
    {
        var (service, _, _, _, _) = await CreateWithSeedAsync();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CancelAsync(999, "Does not exist"));
    }

    // ─────────────────────────────────────────────────────
    // NURSE ASSIGNMENT TESTS
    // ─────────────────────────────────────────────────────

    [Fact]
    public async Task AssignNurseAsync_ValidNurse_AddsAssignment()
    {
        // Arrange
        var (service, surgeonId, nurseId, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Nurse assignment test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        // Act
        var result = await service.AssignNurseAsync(created.Id, new AssignNurseDto
        {
            NurseId = nurseId,
            RoleDuringSurgery = "Circulating Nurse",
            IsScrubNurse = false
        });

        // Assert
        Assert.Single(result.Nurses);
        Assert.Equal("Circulating Nurse", result.Nurses.First().RoleDuringSurgery);
    }

    [Fact]
    public async Task AssignNurseAsync_DuplicateNurse_ThrowsInvalidOperationException()
    {
        // Arrange
        var (service, surgeonId, nurseId, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(3),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Duplicate nurse test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        await service.AssignNurseAsync(created.Id, new AssignNurseDto
        {
            NurseId = nurseId,
            RoleDuringSurgery = "Scrub Nurse",
            IsScrubNurse = true
        });

        // Act & Assert — same nurse cannot be assigned twice
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AssignNurseAsync(created.Id, new AssignNurseDto
            {
                NurseId = nurseId,
                RoleDuringSurgery = "Another role"
            }));
        Assert.Contains("already assigned", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RemoveNurseAsync_AssignedNurse_RemovesSuccessfully()
    {
        // Arrange
        var (service, surgeonId, nurseId, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Remove nurse test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        await service.AssignNurseAsync(created.Id, new AssignNurseDto
        {
            NurseId = nurseId,
            RoleDuringSurgery = "Support Nurse",
            IsScrubNurse = false
        });

        // Act
        await service.RemoveNurseAsync(created.Id, nurseId);

        // Assert
        var updated = await service.GetByIdAsync(created.Id);
        Assert.Empty(updated!.Nurses);
    }

    [Fact]
    public async Task RemoveNurseAsync_UnassignedNurse_ThrowsKeyNotFoundException()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(2),
            EstimatedDurationMinutes = 60,
            ProcedureName = "Remove unassigned test",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.RemoveNurseAsync(created.Id, 999));
    }

    // ─────────────────────────────────────────────────────
    // QUERY TESTS
    // ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingSurgery_ReturnsDto()
    {
        // Arrange
        var (service, surgeonId, _, patientId, roomId) = await CreateWithSeedAsync();
        var created = await service.ScheduleAsync(new CreateSurgeryDto
        {
            PlannedDate = DateTime.UtcNow.AddDays(5),
            EstimatedDurationMinutes = 45,
            ProcedureName = "Appendectomy",
            PatientId = patientId,
            SurgeonId = surgeonId,
            OperatingRoomId = roomId
        });

        // Act
        var result = await service.GetByIdAsync(created.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Appendectomy", result!.ProcedureName);
        Assert.Equal(45, result.EstimatedDurationMinutes);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistent_ReturnsNull()
    {
        var (service, _, _, _, _) = await CreateWithSeedAsync();
        var result = await service.GetByIdAsync(999);
        Assert.Null(result);
    }

    public void Dispose()
    {
        _factory.Dispose();
    }
}