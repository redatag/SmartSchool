# SmartSchool Project Instructions

## Project Overview
SmartSchool is a web-based smart schooling platform.

Target markets:
- Egypt
- Saudi Arabia
- Future international schools

Technology baseline:
- .NET 10
- ASP.NET Core
- C# 14
- Entity Framework Core
- SQL Server
- Redis
- xUnit

## Global Architecture
The overall system MUST use Modular Monolithic Architecture.

Each business module is a bounded context and owns its own domain model, persistence model, DbContext, migrations, schema, and contracts.

Modules:
1. Identity & Access — Clean Architecture
2. Academic — Hexagonal Architecture
3. Enrollment — Vertical Slice Architecture
4. Attendance — Onion Architecture
5. Finance — CQRS + DDD
6. Notifications — Event-Driven Architecture
7. Smart Insights — DDD + Processing/Data Pipeline

The architectural style inside each module may differ, but the whole product remains one Modular Monolith.

## DDD Rules
Use:
- Bounded Contexts
- Aggregate Roots
- Entities
- Value Objects
- Domain Services only when justified
- Domain Events
- Integration Events
- Explicit business invariants

Business logic MUST NOT live in Controllers, EF configurations, repositories, or presentation code.

Aggregates must protect their own consistency rules.

## Module Isolation
Each module:
- Owns its tables
- Owns its DbContext
- Owns its EF Core migrations
- Owns its SQL schema
- Must not directly access another module's DbContext
- Must not query another module's tables directly
- Must not create EF navigation properties across module boundaries
- Must not create SQL foreign keys across module boundaries

Cross-module references must use IDs and explicit contracts/events.

## SQL Schemas
Use these schemas:
- platform
- identity
- academic
- enrollment
- attendance
- finance
- notifications
- insights

## Communication Rules
Synchronous module communication:
- Explicit module contracts/interfaces
- Used only when an immediate response is required

Asynchronous module communication:
- Domain Events inside a bounded context
- Integration Events between bounded contexts
- Outbox Pattern for reliable publication
- Inbox Pattern for idempotent consumers
- Background event dispatcher

Do not create distributed-transaction style flows across modules.

## Shared Kernel
Keep SharedKernel very small.

Allowed candidates:
- Entity
- AggregateRoot
- ValueObject
- IDomainEvent
- Result
- Error
- DomainException
- Money
- DateRange

Do NOT place business concepts such as Student, Teacher, Invoice, Payment, Enrollment, Attendance, Grade, or Subject in SharedKernel.

## Important Domain Decisions
- Student and Enrollment are separate aggregates.
- Class does not own students; Enrollment owns class membership.
- AttendanceSession is the Attendance Aggregate Root.
- AttendanceRecord is a child Entity inside AttendanceSession.
- Invoice is the Finance Aggregate Root.
- Payment is a child Entity inside Invoice.
- TeacherAssignment is an independent Aggregate Root.
- No cross-module EF navigation properties.
- No cross-module SQL foreign keys.

## Identity Module — Clean Architecture
Projects:
- SmartSchool.Modules.Identity.Domain
- SmartSchool.Modules.Identity.Application
- SmartSchool.Modules.Identity.Infrastructure
- SmartSchool.Modules.Identity.Presentation

Dependency direction:
- Presentation -> Application -> Domain
- Infrastructure -> Application
- Infrastructure -> Domain

Domain MUST NOT reference:
- Entity Framework Core
- SQL Server
- ASP.NET Core
- JWT libraries
- Redis
- Email providers

## Coding Rules
- Enable nullable reference types.
- Use async/await for I/O.
- Accept CancellationToken for async application/infrastructure operations.
- Prefer immutable records for commands, queries, DTOs, and events.
- Do not expose IQueryable between layers/modules.
- Avoid generic repositories.
- Repositories should primarily target Aggregate Roots.
- Keep handlers focused.
- Prefer explicit code over unnecessary abstractions.
- Use UTC timestamps for persisted technical timestamps.
- Use Guid.CreateVersion7() for new aggregate/entity IDs where supported.

## Database Rules
One SQL Server database, separate schemas per module.

Each module has its own DbContext and migration history table, for example:
- identity.__EFMigrationsHistory
- academic.__EFMigrationsHistory
- enrollment.__EFMigrationsHistory

Use rowversion for optimistic concurrency where appropriate.

## Testing
Create:
- Unit Tests
- Integration Tests
- Architecture Tests

Architecture tests must enforce:
- Domain does not reference Infrastructure.
- Application does not reference Infrastructure.
- Modules do not reference another module's Infrastructure project.
- Domain projects do not depend on Presentation projects.
- Cross-module access occurs only through contracts/events.

## Current Implementation Order
Implement in this order:
1. Solution Bootstrap
2. SharedKernel
3. Identity Domain
4. Identity Application
5. Identity Infrastructure
6. IdentityDbContext
7. Initial Identity migration
8. Create User
9. Roles and Permissions
10. Login
11. JWT + Refresh Token
12. Permission authorization
13. Unit Tests
14. Integration Tests
15. Architecture Tests

Do NOT implement Academic or later modules until the Identity milestone is complete and builds/tests pass.

## Project Documentation
Before implementing a feature, read the relevant documents under /docs:
- docs/01-BRD.md
- docs/02-UseCases.md
- docs/03-Domain-Model.md
- docs/04-ERD.md

If code conflicts with these documents, do not silently invent a new design. Prefer the documented business rules and architecture. If a design change is required, document it as an ADR before changing behavior.
