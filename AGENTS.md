# SmartSchool Project Instructions

## Project
SmartSchool is a web-based smart schooling platform.

Technology:
- .NET 10
- ASP.NET Core
- C# 14
- Entity Framework Core
- SQL Server
- Redis
- xUnit

## Architecture

The overall system MUST use Modular Monolithic Architecture.

Each business module is a separate bounded context.

Modules:

1. Identity
   - Clean Architecture

2. Academic
   - Hexagonal Architecture

3. Enrollment
   - Vertical Slice Architecture

4. Attendance
   - Onion Architecture

5. Finance
   - DDD + CQRS

6. Notifications
   - Event-Driven Architecture

7. SmartInsights
   - DDD + Processing Pipeline

## DDD Rules

Use:
- Aggregate Roots
- Entities
- Value Objects
- Domain Events
- Integration Events
- Domain Services where appropriate

Business logic MUST NOT exist in Controllers.

Aggregates must protect business invariants.

## Module Isolation

Each module:
- Has its own DbContext
- Has its own SQL schema
- Has its own migrations
- Owns its tables
- Must not directly access tables from another module

Do NOT create EF navigation properties across modules.

Do NOT create SQL foreign keys across module boundaries.

Cross-module references must use IDs.

## Database Schemas

platform
identity
academic
enrollment
attendance
finance
notifications
insights

## Communication

Support both synchronous and asynchronous communication.

Synchronous:
- Explicit module contracts/interfaces

Asynchronous:
- Domain Events
- Integration Events
- Outbox Pattern
- Inbox Pattern
- Background Event Dispatcher

Modules must not communicate through direct DbContext access.

## Shared Kernel

SharedKernel must remain small.

Allowed concepts:
- Entity
- AggregateRoot
- ValueObject
- IDomainEvent
- Result
- Error
- DomainException
- Money
- DateRange

Never put business aggregates such as Student, Invoice, Teacher or Enrollment in SharedKernel.

## Identity Module

Identity follows Clean Architecture.

Projects:

SmartSchool.Modules.Identity.Domain
SmartSchool.Modules.Identity.Application
SmartSchool.Modules.Identity.Infrastructure
SmartSchool.Modules.Identity.Presentation

Dependency direction:

Presentation -> Application -> Domain
Infrastructure -> Application
Infrastructure -> Domain

Domain must have no references to:
- EF Core
- SQL Server
- ASP.NET Core
- JWT
- Redis

## Identity Domain

Main aggregates:

User
Role

Entities:
UserRole
RolePermission

Permissions should use stable codes such as:

students.view
students.manage
attendance.view
attendance.record
finance.invoice.view
finance.invoice.create
finance.payment.create

## Database

Use one SQL Server database with separate schemas.

Each module must have:
- Separate DbContext
- Separate migration history table

Example:

identity.__EFMigrationsHistory
academic.__EFMigrationsHistory

## Testing

Create:
- Unit Tests
- Integration Tests
- Architecture Tests

Architecture tests must verify module boundaries.

## Coding Rules

- Use async/await for I/O
- Use CancellationToken
- Enable nullable reference types
- Prefer immutable records for commands/events
- Use dependency injection
- Do not use generic repositories
- Repositories should primarily exist for Aggregate Roots
- Do not expose IQueryable between layers
- Do not put business logic in repositories
- Avoid unnecessary abstractions
- Prefer explicit code over over-engineering

## Current Development Order

Implement only the following milestone first:

1. Bootstrap solution
2. SharedKernel
3. Identity module
4. IdentityDbContext
5. Initial Identity migration
6. Create User
7. Roles and Permissions
8. Login
9. JWT + Refresh Token
10. Permission authorization
11. Unit tests
12. Integration tests
13. Architecture tests

Do NOT implement Academic or other modules until Identity milestone is complete.