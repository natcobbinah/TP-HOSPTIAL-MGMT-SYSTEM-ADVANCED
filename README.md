# Hospital Surgical Management System (HSMS)

[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/en-us/)
[![License](https://img.shields.io/badge/License-MIT-green)](LICENSE)

## Table of Contents

- [Project Overview](#project-overview)  
- [Objectives](#objectives)  
- [Architecture](#architecture)  
- [Key Features](#key-features)  
- [Domain Model](#domain-model)  
- [Data Access & Persistence](#data-access--persistence)  
- [Concurrency & Transactions](#concurrency--transactions)  
- [Testing Strategy](#testing-strategy)  
- [Performance & Scalability](#performance--scalability)  
- [Setup & Installation](#setup--installation)  
- [References](#references)  

---

## Project Overview

The **Hospital Surgical Management System (HSMS)** is an enterprise-grade application designed to manage **surgical interventions**, **operating rooms**, **staff scheduling**, and **patient records**.  

The system implements **Domain-Driven Design (DDD)**, **Entity Framework Core**, and **advanced concurrency control** to ensure consistency in multi-user environments such as hospital operation theaters.  

---

## Objectives

1. Efficiently manage surgical interventions, including scheduling, rescheduling, and cancellation.  
2. Ensure **data consistency** using **optimistic concurrency** and transaction management.  
3. Provide a **full audit trail** for regulatory compliance.  
4. Support high-load scenarios (1000+ interventions/day) with scalable architecture.  
5. Enable **testing of concurrency and transactions** for robust system validation.  

---

## Architecture

The project follows a **layered architecture**:

- **Domain Layer:** Entities, Value Objects, Enums, and Domain Services.  
- **Application Layer:** Service interfaces and DTOs.  
- **Infrastructure Layer:** EF Core DbContext, Repositories, Transactions.  
- **Presentation Layer:** API (optional, can be extended).  
- **Tests Layer:** Unit tests, integration tests, concurrency tests.  

**Design Patterns Used:**

- Repository Pattern  
- Unit of Work  
- Factory Pattern (for test data seeding)  
- Optimistic Concurrency via `ConcurrencyStamp`  
- Owned Types for Value Object persistence  

---

## Key Features

| Feature | Description |
|---------|-------------|
| Scheduling | Schedule, reschedule, and cancel surgeries with conflict detection |
| Staff Management | Manage surgeons, nurses, and operating room assignments |
| Concurrency Control | Detect conflicts with `ConcurrencyStamp` and `RowVersion` |
| Transactions | Atomic operations for critical workflows |
| Audit | Track modifications (who, what, when) |
| High Load Handling | Scalable design for >1000 interventions/day |

---

## Domain Model

- **Entities:** `Surgery`, `Surgeon`, `Patient`, `OperatingRoom`, `Nurse`  
- **Value Objects:** `Address`, `ContactInfo`, `ScheduleWindow`  
- **Enumerations:** `SurgeryStatus`, `OperatingRoomStatus`  

**Example Entity Relationships:**

- Surgery ↔ Patient (1:N)  
- Surgery ↔ Surgeon (1:N)  
- Surgery ↔ OperatingRoom (1:1)  
- Surgery ↔ Nurses (Many-to-Many via join entity)  

---

## Data Access & Persistence

- **EF Core 10.0**  
- **TPH strategy** for staff inheritance (`Surgeon`, `Nurse`)  
- **Owned Types** for `Address` and `ContactInfo`  
- **Shadow Properties** for audit fields (`CreatedAt`, `ModifiedAt`)  
- **Global Query Filters** to implement soft delete / active staff filtering  

---

## Concurrency & Transactions

**Optimistic Concurrency:**

- Each `Surgery` has a `ConcurrencyStamp` updated on every change.  
- Conflicts trigger `InvalidOperationException` with descriptive messages.  

**Transactions:**

- Critical workflows use **explicit transactions** for atomic operations.  
- Supports rollback on exceptions, ensuring database integrity.  

**Locking Strategy Comparison:**

| Aspect | Optimistic | Pessimistic |
|--------|-----------|------------|
| Philosophy | Detect conflicts at save | Prevent conflicts upfront |
| Concurrency | High, no blocking | Low, blocking occurs |
| Best for | Low contention | High contention critical updates |
| Implementation | `ConcurrencyStamp` / `RowVersion` | `SELECT ... FOR UPDATE` |

---

## Testing Strategy

- **Unit Tests:** Core services and domain logic  
- **Integration Tests:** EF Core in-memory and SQL databases  
- **Concurrency Tests:** Simulate multiple users updating the same surgery  
- **Transaction Tests:** Ensure rollback on errors  

**Tools Used:**

- xUnit  
- EF Core In-Memory  
- TestDbContextFactory for seeding consistent test data  

---

## Performance & Scalability

- **Compiled Queries** for frequent read-heavy operations  
- **Caching** strategies for reference data (rooms, staff)  
- **Load testing scenarios:** >1000 surgeries/day simulated via concurrent service calls  
- **Database optimization:** Indexing on frequently queried fields (PatientId, SurgeonId, PlannedDate)  

---

## Setup & Installation

```bash
# Clone repository
git clone https://github.com/username/hospital-surgical.git
cd hospital-surgical

# Build the solution
dotnet build

# Run all tests
dotnet test

# Interact with API endpoints
dotnet run --project HospitalSurgical.API
```

## Sample application Snapshots
<img width="1918" height="1038" alt="hospital_surgical_apis" src="https://github.com/user-attachments/assets/9f128c30-4db4-4548-8b2c-dd02b2b1714b" />
