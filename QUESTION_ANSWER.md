## Quelle stratégie TPH utilisez-vous ? Pourquoi ?
Answer:
```
> We use Table-Per-Hierarchy (TPH) for the Staff entity hierarchy (Surgeon, Nurse, AdministrativeStaff).
```
```
Why TPH?
| Criterion           | TPH                               | TPT                          | TPC                              |
| ------------------- | --------------------------------- | ---------------------------- | -------------------------------- |
| Query Performance   | Fastest (no JOINs)                | Slower (JOIN per level)      | Fast per type, slow polymorphic  |
| Storage Efficiency  | Nullable columns                  | No null waste                | Duplicates base columns          |
| Schema Complexity   | Single table                       | Multiple tables              | Multiple tables                  |
| Best For            | Few types, frequent polymorphic queries | Many types, rarely queried together | Always query by specific type   |
```

## Comment gérez-vous les propriétés communes ?
Answer:
```
Common properties are defined once in the abstract base class (Staff) and inherited by all derived types.
```
```
Benefits:
> DRY principle: Common properties defined once
> Consistent validation: All staff types share the same constraints
> Polymorphic queries: Can query Staff and get all types
> Shared behavior: FullName computed property works for all types
```

## Comment requête-t-on un type spécifique ?
Answer:
```
Use OfType<T>() to filter by discriminator value or use Direct DbSet access (if configured)
```

## Pourquoi pas de Many-to-Many directe ?
Answer:
```
We cannot use EF Core's implicit Many-to-Many because we need extra data on the relationship (RoleDuringSurgery, IsScrubNurse, AssignedAt).
```

EF Core 5+ Implicit Many-to-Many (what we CAN'T use):
```
// ? This only works if you DON'T need extra columns on the join
public class Surgery
{
    public ICollection<Nurse> Nurses { get; set; }  // Direct navigation
}

public class Nurse
{
    public ICollection<Surgery> Surgeries { get; set; }  // Direct navigation
}

// EF Core auto-creates a hidden join table: SurgeryNurse(SurgeryId, NurseId)
// BUT: No way to add RoleDuringSurgery or IsScrubNurse columns!
```
Our Solution: Explicit Join Entity (what we MUST use):
```
// ? Explicit join entity with payload
public class SurgeryNurse
{
    // Composite PK
    public int SurgeryId { get; set; }
    public Surgery Surgery { get; set; } = null!;
    
    public int NurseId { get; set; }
    public Nurse Nurse { get; set; } = null!;
    
    // ? Extra columns ù this is why we need an explicit entity
    public string RoleDuringSurgery { get; set; } = string.Empty;
    public bool IsScrubNurse { get; set; }
    public DateTime AssignedAt { get; set; }
}

// Surgery navigates to SurgeryNurse, not directly to Nurse
public class Surgery
{
    public ICollection<SurgeryNurse> NurseAssignments { get; set; }
}

public class Nurse
{
    public ICollection<SurgeryNurse> SurgeryAssignments { get; set; }
}
```
```
Benefits of Explicit Join Entity:
> Can add unlimited extra columns
> Can query the join table directly: _context.SurgeryNurses.Where(sn => sn.IsScrubNurse)
> Can add indexes on join table columns
> Can add validation logic to the join entity
```

## Comment charger les infirmiers d'une intervention ?
Answer:
```
Use Eager Loading with Include + ThenInclude to load the join entity and the related nurses in a single query.
```

## Quelle différence entre Owned Type et Value Object ?
Answer:
| Concept       | Definition                                           | Scope                               |
| ------------- | --------------------------------------------------- | ---------------------------------- |
| Value Object  | DDD concept: immutable object with no identity, equality based on values | Domain modeling (conceptual)       |
| Owned Type    | EF Core concept: how to map a Value Object to the database | Data persistence (technical)       |

| Aspect       | Value Object (DDD)               | Owned Type (EF Core)             |
| ------------ | ------------------------------- | -------------------------------- |
| Purpose      | Domain modeling concept         | Database mapping technique       |
| Identity     | No identity                     | No separate table/PK             |
| Immutability | Should be immutable             | Can be mutable (EF Core doesn't enforce) |
| Storage      | N/A (conceptual)                | Columns in owner's table         |
| Equality     | Value-based                     | N/A (EF Core doesn't use it)     |

```
When to use Owned Types:
> Address, ContactInfo, Money, DateRange ù concepts with no identity
> Reused across multiple entities (Patient.Address, Department.Address)
> Always accessed through the owner (never queried independently)
> If you need to query it independently ? use a separate entity with FK
```

## Comment valider un Owned Type ?
Answer:
Method 1: Data Annotations (Simplest)
Example
```
public class ContactInfo
{
    [Required]
    [Phone]
    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    [MaxLength(150)]
    public string Email { get; set; } = string.Empty;
}

// Validation happens automatically when ModelState.IsValid is called
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreatePatientDto dto)
{
    if (!ModelState.IsValid)  // ? Validates ContactInfo propertiesreturn BadRequest(ModelState);
    
    // ...
}
```
Method 2: IValidatableObject (Custom validation)
```
public class ContactInfo : IValidatableObject
{
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string EmergencyPhone { get; set; } = string.Empty;
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Custom rule: Phone and EmergencyPhone cannot be the same
        if (Phone == EmergencyPhone && !string.IsNullOrEmpty(Phone))
        {
            yield return new ValidationResult(
                "Emergency phone must be different from primary phone.",
                new[] { nameof(EmergencyPhone) });
        }
        
        // Custom rule: At least one contact method required
        if (string.IsNullOrWhiteSpace(Phone) && string.IsNullOrWhiteSpace(Email))
        {
            yield return new ValidationResult(
                "Either phone or email must be provided.",
                new[] { nameof(Phone), nameof(Email) });
        }
    }
}
```
```
Others:
> Method 3: FluentValidation (Recommended for complex rules)
> Method 4: Domain-level validation (in the entity)
> Method 5: EF Core Value Converter with validation
```

## Comment le Global Query Filter fonctionne-t-il ?
Answer:
```
A Global Query Filter is a LINQ predicate automatically applied to every query on an entity type.
Configuration:
```

```
// StaffConfiguration.cs
builder.HasQueryFilter(s => !s.IsDeleted);
```

// This single line affects ALL queries on Staff (and derived types)
```
What it does:
// User writes:
var staff = await _context.Staff.ToListAsync();

// EF Core automatically transforms it to:
var staff = await _context.Staff
    .Where(s => !s.IsDeleted)  // ? Filter added automatically
    .ToListAsync();

// Generated SQL:
// SELECT * FROM Staff WHERE IsDeleted = 0
```

## Comment surcharger le filter pour les admins ?
Answer:
```
Use IgnoreQueryFilters() to bypass the global filter.
```

Method 1: IgnoreQueryFilters() ù Complete bypass
```
// Admin endpoint: show ALL staff including soft-deleted
[HttpGet("deleted")]
[Authorize(Roles = "Admin")]  // ? Protect this endpoint!
public async Task<IActionResult> GetDeletedStaff()
{
    var deletedStaff = await _context.Staff
        .IgnoreQueryFilters()        // ? Bypass the IsDeleted filter
        .Where(s => s.IsDeleted)     // ? Manually filter for deleted only
        .OrderBy(s => s.LastName)
        .ToListAsync();
    
    return Ok(deletedStaff);
}

// Generated SQL:
// SELECT * FROM Staff WHERE IsDeleted = 1 ORDER BY LastName
// (No automatic "IsDeleted = 0" filter)
```
Method 2: Conditional filter in repository
```
public interface IStaffRepository
{
    Task<IEnumerable<Staff>> GetAllAsync(bool includeDeleted = false);
}

public class StaffRepository : IStaffRepository
{
    public async Task<IEnumerable<Staff>> GetAllAsync(bool includeDeleted = false)
    {
        var query = _context.Staff.AsQueryable();
        
        if (includeDeleted)
            query = query.IgnoreQueryFilters();
        
        return await query.OrderBy(s => s.LastName).ToListAsync();
    }
}

// Usage:
var activeStaff = await _repo.GetAllAsync();// Filtered
var allStaff = await _repo.GetAllAsync(includeDeleted: true);  // Unfiltered
```
Others
Method 3: Separate method for admin queries
```
public class StaffRepository
{
    // Normal users ù filter applied
    public async Task<IEnumerable<Staff>> GetAllAsync()
        => await _context.Staff.ToListAsync();
    
    // Admins only ù filter bypassed
    public async Task<IEnumerable<Staff>> GetAllIncludingDeletedAsync()
        => await _context.Staff.IgnoreQueryFilters().ToListAsync();
}
```

## Qu'est-ce qu'une Shadow Property ?
Answer:
```
A Shadow Property is a property that exists in EF Core's metadata model and the database, but NOT as a C# property on the entity class.
```

```
Why use Shadow Properties?
> Keep domain entities clean of infrastructure concerns
> Audit fields (CreatedAt, UpdatedBy) don't belong in domain logic
> Discriminator columns in TPH (StaffType) are managed by EF Core
> Foreign keys can be shadow if you only need navigation properties
```

Example:
```
// Domain entity ù NO CreatedAt property in C#
public class Staff
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    // ... other domain properties
    
    // ? NO: public DateTime CreatedAt { get; set; }
    // We don't want audit fields polluting the domain model
}

// Configuration ù Shadow Property defined in EF Core metadata
builder.Property<DateTime>("CreatedAt")
    .HasDefaultValueSql("CURRENT_TIMESTAMP");

builder.Property<string>("CreatedBy")
    .HasMaxLength(100);

// Database ù columns exist in the table
// CREATE TABLE Staff (
//     Id INTEGER PRIMARY KEY,
//     FirstName TEXT,
//     CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,  -- Shadow property
//     CreatedBy TEXT,                -- Shadow property
//     ...
// )
```

## Comment définir des valeurs par défaut ?
Answer:
Method 1: HasDefaultValueSql() ù Database-level default
```
// Configuration
builder.Property<DateTime>("CreatedAt")
    .HasDefaultValueSql("CURRENT_TIMESTAMP");

// Generated SQL:
// CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP

// EF Core does NOT set this value ù the database does on INSERT
```
Method 2: HasDefaultValue() ù EF Core constant
```
builder.Property<bool>("IsActive")
    .HasDefaultValue(true);

// Generated SQL:
// IsActive INTEGER NOT NULL DEFAULT 1

// Database sets the value if EF Core doesn't provide one
```

## Quelle différence entre optimistic et pessimistic locking ?
Answer:
| Aspect               | Optimistic Locking                          | Pessimistic Locking                   |
| -------------------- | ------------------------------------------ | ------------------------------------ |
| Philosophy           | Assume no conflict, detect at save         | Assume conflict, prevent it upfront  |
| Lock timing          | No lock until save                          | Lock held from read to commit        |
| Concurrency          | High (no blocking)                          | Low (users wait for locks)           |
| Conflict detection   | At SaveChanges()                            | Never (lock prevents conflicts)      |
| Implementation       | Concurrency token (RowVersion, Timestamp)  | SELECT ... FOR UPDATE                |
| Best for             | Low contention, many reads                  | High contention, critical updates    |
| Database support     | All databases                               | PostgreSQL, MySQL, SQL Server       |
| User experience      | May need to retry                           | Guaranteed success but slower        |

## Comment informer l'utilisateur du conflit ?
Answer:
Step 1: Catch the exception in the service
```
public async Task<SurgeryDto> UpdateStatusAsync(int id, UpdateSurgeryStatusDto dto)
{
    var surgery = await _unitOfWork.Surgeries.GetByIdAsync(id)
        ?? throw new KeyNotFoundException($"Surgery {id} not found.");
    
    // Verify concurrency stamp matches
    if (surgery.ConcurrencyStamp != dto.ConcurrencyStamp)
    {
        throw new InvalidOperationException(
            "CONCURRENCY_CONFLICT: This surgery was modified by another user. " +
            "Please reload and try again.");
    }
    
    surgery.Status = Enum.Parse<SurgeryStatus>(dto.Status);
    _unitOfWork.Surgeries.Update(surgery);
    
    try
    {
        await _unitOfWork.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // Another update happened between our read and write
        throw new InvalidOperationException(
            "CONCURRENCY_CONFLICT: Surgery was updated simultaneously. " +
            "Reload and retry.", ex);
    }
    
    return MapToDto(surgery);
}

Step 2: Return HTTP 409 Conflict from controller
[HttpPut("{id:int}/status")]
public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSurgeryStatusDto dto)
{
    try
    {
        var surgery = await _surgeryService.UpdateStatusAsync(id, dto);
        return Ok(surgery);
    }
    catch (KeyNotFoundException)
    {
        return NotFound();
    }
    catch (InvalidOperationException ex) when (ex.Message.StartsWith("CONCURRENCY_CONFLICT"))
    {
        // Return 409 Conflict with structured error
        return Conflict(new
        {
            error = "ConcurrencyConflict",
            message = "This record was modified by another user.",
            detail = ex.Message,
            action = "reload"  // Tell client what to do
        });
    }
}

Step 3: Client-side handling (JavaScript example)
async function updateSurgeryStatus(surgeryId, status, concurrencyStamp) {
    const response = await fetch(`/api/surgeries/${surgeryId}/status`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ status, concurrencyStamp })
    });
    
    if (response.status === 409) {  // Conflict
        const error = await response.json();
        
        // Show user-friendly message
        showNotification({
            type: 'warning',
            title: 'Record Modified',
            message: 'Another user modified this surgery. Your changes were not saved.',
            actions: [
                {
                    label: 'Reload and Retry',
                    onClick: async () => {
                        // Reload fresh data
                        const fresh = await fetch(`/api/surgeries/${surgeryId}`).then(r => r.json());
                        
                        // Show diff (optional)
                        showDiffModal(currentData, fresh);
                        
                        // Let user decide: overwrite or cancel}
                },
                {
                    label: 'Discard My Changes',
                    onClick: () => window.location.reload()
                }
            ]
        });
        
        return null;
    }
    
    return await response.json();
}

Step 4: Advanced ù Show what changed
catch (DbUpdateConcurrencyException ex)
{
    // Get current DB values
    var entry = ex.Entries.Single();
    var databaseValues = await entry.GetDatabaseValuesAsync();
    
    if (databaseValues is null)
    {
        throw new InvalidOperationException("Record was deleted by another user.");
    }
    
    // Compare what changed
    var changes = new List<string>();
    foreach (var property in entry.Properties)
    {
        var proposedValue = property.CurrentValue;
        var databaseValue = databaseValues[property.Metadata.Name];
        
        if (!Equals(proposedValue, databaseValue))
        {
            changes.Add($"{property.Metadata.Name}: " +
                $"You tried '{proposedValue}', but current value is '{databaseValue}'");
        }
    }
    
    throw new InvalidOperationException(
        $"CONCURRENCY_CONFLICT: {string.Join("; ", changes)}");
}
Best practices:

? Always include the current ConcurrencyStamp in update DTOs
? Return HTTP 409 (not 400 or 500) for concurrency conflicts
? Provide actionable guidance: "Reload and retry"
? Log concurrency conflicts for monitoring
? Consider showing a diff of what changed
? Never silently overwrite (last-write-wins) without user consent
```

## Quand utiliser les compiled queries ?
Answer:
```
Use compiled queries when:
> Query is executed frequently (100+ times/second)
> Query structure never changes (only parameters vary)
> Query translation overhead is measurable (5-10ms per call)
> Hot path / critical performance scenario

Don't use compiled queries when:
> Query is executed rarely (< 10 times/minute)
> Query structure is dynamic (conditional WHERE clauses)
> Premature optimization (profile first!)
```

## Quel impact sur la mémoire ?
Answer:
```
Memory footprint:
Each compiled query: ~5-20 KB (expression tree + compiled delegate)
Stored in static fields lives for application lifetime
Not garbage collected until app shutdown
```

## Quand utiliser une transaction explicite ?
Answer:
EF Core's implicit transaction:
```
// Single SaveChangesAsync() = automatic transaction
patient.FirstName = "Updated";
await _context.SaveChangesAsync();  // ? Wrapped in transaction automatically
```
## Comment gérer les transactions distribuées ?
Answer:
```
Problem: Traditional distributed transactions (2PC) are not recommended in modern architectures:
> Poor performance (locks across systems)
> Not supported by many cloud services
> Tight coupling between services
```
Modern solutions:
Saga Pattern (Recommended)
Choreography-based saga with compensating transactions:
```
// Step 1: Create surgery (local transaction)
public async Task<int> CreateSurgeryAsync(CreateSurgeryDto dto)
{
    var surgery = new Surgery { ... };
    await _context.Surgeries.AddAsync(surgery);
    await _context.SaveChangesAsync();
    
    // Publish event
    await _eventBus.PublishAsync(new SurgeryCreatedEvent
    {
        SurgeryId = surgery.Id,
        PatientId = dto.PatientId,
        RoomId = dto.OperatingRoomId
    });
    
    return surgery.Id;
}

// Step 2: External service reserves room (separate transaction)
public async Task Handle(SurgeryCreatedEvent evt)
{
    try
    {
        await _roomService.ReserveRoomAsync(evt.RoomId, evt.SurgeryId);
        await _eventBus.PublishAsync(new RoomReservedEvent { ... });
    }
    catch
    {
        // Compensating transaction: cancel surgery
        await _eventBus.PublishAsync(new SurgeryCreationFailedEvent
        {
            SurgeryId = evt.SurgeryId,Reason = "Room reservation failed"
        });
    }
}

// Step 3: Compensating action if needed
public async Task Handle(SurgeryCreationFailedEvent evt)
{
    var surgery = await _context.Surgeries.FindAsync(evt.SurgeryId);
    surgery.Status = SurgeryStatus.Cancelled;
    await _context.SaveChangesAsync();
}
```
Outbox Pattern (Reliable messaging)
```
// Domain event stored in same DB transaction
public class OutboxMessage
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }
}

// Service: write to DB + outbox atomically
public async Task<Surgery> ScheduleSurgeryAsync(CreateSurgeryDto dto)
{
    await using var transaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        // 1. Business operation
        var surgery = new Surgery { ... };
        await _context.Surgeries.AddAsync(surgery);
        
        // 2. Store event in outbox (same transaction!)
        var outboxMessage = new OutboxMessage
        {
            EventType = "SurgeryCreated",
            Payload = JsonSerializer.Serialize(new { surgery.Id, surgery.PatientId }),
            CreatedAt = DateTime.UtcNow
        };
        await _context.OutboxMessages.AddAsync(outboxMessage);
        
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        
        return surgery;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}

// Background worker: process outbox
public class OutboxProcessor : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _context.OutboxMessages
                .Where(m => !m.Processed)
                .Take(100)
                .ToListAsync();
            
            foreach (var message in messages)
            {
                try
                {
                    // Publish to message bus
                    await _eventBus.PublishAsync(message.EventType, message.Payload);
                    
                    message.Processed = true;
                    await _context.SaveChangesAsync();
                }
                catch
                {
                    // Retry later
                }
            }
            
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```
