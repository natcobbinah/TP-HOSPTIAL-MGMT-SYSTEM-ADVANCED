# ANALYSE — Advanced Surgical Management System

## 1. Quand préférer TPH vs TPT vs TPC?

### Decision Matrix

| Criterion              | TPH                             | TPT                                  | TPC                                      |
|------------------------|---------------------------------|--------------------------------------|------------------------------------------|
| Query Performance      | Fastest (no JOINs)              | Slower (JOIN per inheritance level)  | Fast for specific types, slow polymorphic|
| Storage Efficiency     | Nullable columns waste space    | No null waste                        | Duplicates base columns in every table   |
| Schema Complexity      | Single table, simple            | Multiple tables, complex             | Multiple tables, no relationships        |
| Polymorphic Queries    | Excellent (`_context.Staff.ToList()`) | Requires JOINs                        | Requires UNION ALL                        |
| Type-Specific Queries  | Good (`OfType<Surgeon>()`)      | Excellent (direct table)             | Excellent (direct table)                 |
| Adding New Types       | Adds nullable columns           | New table only                        | New table only                            |
| Discriminator Column   | Required                        | Optional                              | Not used                                  |


## 2. Comment implémenter un système d'audit complet (qui a modifié quoi, quand) ?

A full audit system tracks the creation, modification, and deletion of entities along with the user who performed the action and timestamp.

### Approaches

| Method | Description | Pros | Cons |
|--------|-------------|------|------|
| **Shadow Properties** | Use EF Core shadow properties like `CreatedBy`, `CreatedAt`, `ModifiedBy`, `ModifiedAt` | No changes to entity classes | Less type-safe, requires careful configuration |
| **Base Entity Class** | Define a `BaseEntity` with audit fields, inherit in all entities | Strongly-typed, easy to query | May require schema migration |
| **Interceptors / SaveChanges Override** | Override `DbContext.SaveChangesAsync` to populate audit fields automatically | Centralized, automatic | Slightly more complex, extra logic in DbContext |
| **Event Logging / Audit Tables** | Use separate audit tables or event sourcing | Full history, can store old values | More storage and complexity |

### Implementation Example (SaveChanges Override)

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.Entity is BaseEntity && 
                   (e.State == EntityState.Added || e.State == EntityState.Modified));

    foreach (var entry in entries)
    {
        var entity = (BaseEntity)entry.Entity;
        var now = DateTime.UtcNow;
        var user = _currentUserService.UserId; // inject user service

        if (entry.State == EntityState.Added)
        {
            entity.CreatedAt = now;
            entity.CreatedBy = user;
        }

        entity.ModifiedAt = now;
        entity.ModifiedBy = user;
    }

    return await base.SaveChangesAsync(cancellationToken);
}
```

## 3. Quelle stratégie pour gérer les pics de charge (1000+ interventions/jour) ?

Handling high-load scenarios in .NET and EF Core requires application-level and database-level strategies.

### Key Strategies

| Layer | Approach | Explanation |
|-------|---------|-------------|
| Database | Indexing & Partitioning | Ensure queries on scheduled dates, surgeon IDs, or room IDs are indexed. Consider table partitioning for very large historical data. |
| Caching | MemoryCache / Redis | Frequently accessed reference data (surgeons, rooms) should be cached to reduce DB queries. |
| Batch Inserts/Updates | Bulk Extensions | Use libraries like `EFCore.BulkExtensions` for mass inserts/updates to reduce EF Core overhead. |
| Async Processing / Queue | Background jobs via Hangfire or Azure Functions | Handle less time-critical tasks like sending notifications or audit logging asynchronously. |
| Connection Pooling | EF Core connection pooling | Proper configuration prevents DB connection exhaustion under load. |
| Horizontal Scaling | Multiple API instances behind load balancer | Ensures concurrent requests are distributed efficiently. |


## How to Test Transactions and Concurrency

Testing transactions and concurrency is crucial in systems like hospital scheduling where multiple users may access or modify the same data simultaneously. The goal is to ensure **data integrity** and proper **conflict handling**.

---

### 4 Transaction Testing

Transactions ensure that a set of operations either **all succeed** or **all fail** (atomicity). To test:

| Aspect | Approach | Example |
|--------|---------|---------|
| Unit of Work / Transaction | Wrap critical operations in a transaction and verify rollback | Use `BeginTransactionAsync()` in tests and assert DB state after exception |
| Integration Testing | Use a test database or in-memory database to simulate real workflows | EF Core `Sqlite In-Memory` or `Testcontainers` for realistic DB |


```csharp
using var transaction = await context.Database.BeginTransactionAsync();

try
{
    // Perform operations
    context.Patients.Add(new Patient { FirstName = "Test", LastName = "Patient" });
    await context.SaveChangesAsync();

    // Force an error
    throw new InvalidOperationException("Simulate failure");

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();

    // Assert the database state was not modified
    var count = await context.Patients.CountAsync();
    Assert.Equal(0, count);
}

