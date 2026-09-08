# SmartSchool
## ERD v1.0 + Database Migration Strategy + Solution Structure

**Architecture:** Modular Monolith  
**Backend:** ASP.NET Core / .NET 10  
**Database:** SQL Server  
**ORM:** Entity Framework Core  
**Domain Approach:** DDD  
**Initial Module:** Identity — Clean Architecture

---

# Part I — Database Design Principles

## 1. Database Model

The MVP uses:

**One SQL Server database**

with:

**One schema per bounded context/module**

```text
SmartSchoolDb
│
├── platform
├── identity
├── academic
├── enrollment
├── attendance
├── finance
├── notifications
└── insights
```

The database is physically shared, but modules are logically isolated.

Each module owns:

- Its tables
- Its DbContext
- Its migrations
- Its repository implementations
- Its database transactions

---

# 2. Critical Database Rule

There will be:

**No physical Foreign Keys across module boundaries.**

Example:

`finance.Invoices.StudentId`

references a Student conceptually, but SQL Server will NOT create:

```text
FK finance.Invoices
   -> enrollment.Students
```

because that would tightly couple Finance to Enrollment.

Instead:

```text
finance.Invoices.StudentId
```

is an external aggregate identifier.

The Finance module validates or obtains student information through:

- Module contract
- Local projection
- Integration event

This rule gives us proper bounded-context isolation.

---

# 3. Common Column Convention

Most tables will contain:

```text
CreatedAtUtc       datetime2
CreatedBy          uniqueidentifier NULL
ModifiedAtUtc      datetime2 NULL
ModifiedBy         uniqueidentifier NULL
```

Aggregate roots may also have:

```text
Version            rowversion
```

for optimistic concurrency.

Where soft deletion is needed:

```text
IsDeleted          bit
DeletedAtUtc       datetime2 NULL
```

However, financial transaction tables should not normally use business deletion.

---

# 4. ID Strategy

Recommended primary key type:

```text
uniqueidentifier
```

Application-generated:

```csharp
Guid.CreateVersion7()
```

where supported.

Reason:

- Globally unique
- Good for modular boundaries
- Suitable for future service extraction
- Version 7 provides better database locality than random UUIDs

---

# Part II — Platform Schema

The platform schema contains only infrastructure-level information that does not belong to a business bounded context.

---

# 5. platform.Schools

Represents a tenant/school.

```text
platform.Schools
-----------------------------------------
SchoolId               uniqueidentifier PK
Code                   nvarchar(30)
Name                   nvarchar(200)
CountryCode            char(2)
TimeZoneId             nvarchar(100)
Status                 tinyint
CreatedAtUtc           datetime2
ModifiedAtUtc          datetime2 NULL
Version                rowversion
```

## Primary Key

```text
PK_Schools
SchoolId
```

## Unique Constraints

```text
UQ_Schools_Code
Code
```

## Indexes

```text
IX_Schools_Status
Status
```

---

# Part III — Identity Schema

Identity uses **Clean Architecture**.

Tables:

```text
identity.Users
identity.Roles
identity.Permissions
identity.UserRoles
identity.RolePermissions
identity.RefreshTokens
```

---

# 6. identity.Users

```text
identity.Users
---------------------------------------------------
UserId                  uniqueidentifier PK
SchoolId                uniqueidentifier
Username                nvarchar(100)
NormalizedUsername      nvarchar(100)
Email                   nvarchar(256)
NormalizedEmail         nvarchar(256)
PasswordHash            nvarchar(500)
FirstName               nvarchar(100)
LastName                nvarchar(100)
PhoneNumber             nvarchar(30) NULL
Status                  tinyint
EmailConfirmed          bit
PhoneConfirmed          bit
LastLoginAtUtc          datetime2 NULL
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## PK

```text
PK_Users(UserId)
```

## Unique Indexes

Within a school:

```text
UX_Users_School_NormalizedUsername

SchoolId,
NormalizedUsername
```

```text
UX_Users_School_NormalizedEmail

SchoolId,
NormalizedEmail
```

## Other Indexes

```text
IX_Users_SchoolId_Status
SchoolId,
Status
```

---

# 7. identity.Roles

```text
identity.Roles
-------------------------------------------
RoleId                 uniqueidentifier PK
SchoolId               uniqueidentifier
Name                   nvarchar(100)
NormalizedName         nvarchar(100)
Description            nvarchar(500) NULL
IsSystemRole           bit
CreatedAtUtc           datetime2
ModifiedAtUtc          datetime2 NULL
Version                rowversion
```

## Unique

```text
UX_Roles_School_NormalizedName
SchoolId,
NormalizedName
```

---

# 8. identity.Permissions

Permissions are defined using stable permission codes.

```text
identity.Permissions
--------------------------------------------
PermissionId           uniqueidentifier PK
Code                   nvarchar(150)
Name                   nvarchar(150)
Module                 nvarchar(100)
Description            nvarchar(500) NULL
```

Examples:

```text
students.view
students.manage

attendance.view
attendance.record

finance.invoice.view
finance.invoice.create

finance.payment.create
```

## Unique

```text
UX_Permissions_Code
Code
```

## Index

```text
IX_Permissions_Module
Module
```

---

# 9. identity.UserRoles

Many-to-many relationship.

```text
identity.UserRoles
--------------------------------------
UserId                 uniqueidentifier
RoleId                 uniqueidentifier
AssignedAtUtc          datetime2
AssignedBy             uniqueidentifier NULL
```

## Primary Key

Composite:

```text
PK_UserRoles
(UserId, RoleId)
```

## Foreign Keys

Within Identity module:

```text
UserId -> identity.Users.UserId
RoleId -> identity.Roles.RoleId
```

Delete behavior:

```text
NO ACTION
```

or controlled cascade only if explicitly desired.

---

# 10. identity.RolePermissions

```text
identity.RolePermissions
--------------------------------------
RoleId                  uniqueidentifier
PermissionId            uniqueidentifier
AssignedAtUtc           datetime2
```

## Primary Key

```text
(RoleId, PermissionId)
```

## FKs

```text
RoleId
 -> identity.Roles.RoleId

PermissionId
 -> identity.Permissions.PermissionId
```

---

# 11. identity.RefreshTokens

Recommended if JWT + refresh token authentication is used.

```text
identity.RefreshTokens
--------------------------------------------------
RefreshTokenId          uniqueidentifier PK
UserId                  uniqueidentifier
TokenHash               nvarchar(500)
ExpiresAtUtc            datetime2
CreatedAtUtc            datetime2
RevokedAtUtc            datetime2 NULL
ReplacedByTokenId       uniqueidentifier NULL
CreatedByIp             nvarchar(64) NULL
RevokedByIp             nvarchar(64) NULL
```

## FK

```text
UserId
 -> Users.UserId
```

## Indexes

```text
IX_RefreshTokens_UserId

IX_RefreshTokens_ExpiresAtUtc
```

Never store raw refresh tokens when avoidable.

Store their hash.

---

# Identity ERD

```text
Users
  │
  │ 1
  │
  * UserRoles *
            │
            │
            1
           Roles
            │
            │ 1
            │
            *
     RolePermissions
            *
            │
            │
            1
       Permissions


Users
  │
  │ 1
  │
  *
RefreshTokens
```

---

# Part IV — Academic Schema

Tables:

```text
academic.AcademicYears
academic.Terms
academic.Grades
academic.Classes
academic.Subjects
academic.Teachers
academic.TeacherAssignments
```

---

# 12. academic.AcademicYears

```text
AcademicYearId          uniqueidentifier PK
SchoolId                uniqueidentifier
Name                    nvarchar(50)
StartDate               date
EndDate                 date
Status                  tinyint
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Unique

```text
UX_AcademicYears_School_Name
SchoolId,
Name
```

## Check Constraint

```text
CK_AcademicYears_DateRange

StartDate < EndDate
```

---

# 13. academic.Terms

```text
TermId                  uniqueidentifier PK
AcademicYearId          uniqueidentifier
Name                    nvarchar(100)
Sequence                int
StartDate               date
EndDate                 date
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
```

## FK

```text
AcademicYearId
 -> AcademicYears
```

## Unique

```text
AcademicYearId + Name

AcademicYearId + Sequence
```

## Check

```text
StartDate < EndDate
```

Term-inside-academic-year validation belongs primarily to the Domain.

---

# 14. academic.Grades

```text
GradeId                 uniqueidentifier PK
SchoolId                uniqueidentifier
Code                    nvarchar(30)
Name                    nvarchar(100)
Sequence                int
IsActive                bit
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Unique

```text
SchoolId + Code
```

## Index

```text
SchoolId + IsActive
```

---

# 15. academic.Classes

```text
ClassId                 uniqueidentifier PK
SchoolId                uniqueidentifier
AcademicYearId          uniqueidentifier
GradeId                 uniqueidentifier
Name                    nvarchar(100)
Capacity                int
Status                  tinyint
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## FKs

Both inside Academic:

```text
AcademicYearId -> AcademicYears

GradeId -> Grades
```

## Unique

```text
AcademicYearId
GradeId
Name
```

## Check

```text
Capacity > 0
```

## Indexes

```text
IX_Classes_AcademicYear_Grade

AcademicYearId,
GradeId
```

---

# 16. academic.Subjects

```text
SubjectId               uniqueidentifier PK
SchoolId                uniqueidentifier
Code                    nvarchar(30)
Name                    nvarchar(150)
IsActive                bit
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
```

## Unique

```text
SchoolId + Code
```

---

# 17. academic.Teachers

```text
TeacherId               uniqueidentifier PK
SchoolId                uniqueidentifier
UserId                  uniqueidentifier NULL
EmployeeNumber          nvarchar(50)
FirstName               nvarchar(100)
LastName                nvarchar(100)
Email                   nvarchar(256) NULL
PhoneNumber             nvarchar(30) NULL
Specialization          nvarchar(200) NULL
Status                  tinyint
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

`UserId` logically points to Identity but has:

**No SQL FK.**

## Unique

```text
SchoolId + EmployeeNumber
```

---

# 18. academic.TeacherAssignments

```text
TeacherAssignmentId     uniqueidentifier PK
SchoolId                uniqueidentifier
TeacherId               uniqueidentifier
AcademicYearId          uniqueidentifier
ClassId                 uniqueidentifier
SubjectId               uniqueidentifier
StartDate               date
EndDate                 date NULL
Status                  tinyint
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Internal FKs

```text
TeacherId -> Teachers
AcademicYearId -> AcademicYears
ClassId -> Classes
SubjectId -> Subjects
```

## Unique Active Assignment

SQL filtered unique index:

```text
TeacherId
AcademicYearId
ClassId
SubjectId
```

for active assignments where practical.

## Indexes

```text
ClassId + AcademicYearId

TeacherId + AcademicYearId
```

---

# Academic ERD

```text
AcademicYear
    │
    ├────< Terms
    │
    └────< Classes >──── Grade
                │
                │
                *
      TeacherAssignments
           /          \
          /            \
     Teacher          Subject
```

---

# Part V — Enrollment Schema

Tables:

```text
enrollment.Students
enrollment.Guardians
enrollment.StudentGuardians
enrollment.Enrollments
enrollment.OutboxMessages
```

---

# 19. enrollment.Students

```text
StudentId               uniqueidentifier PK
SchoolId                uniqueidentifier
StudentNumber           nvarchar(50)
FirstName               nvarchar(100)
MiddleName              nvarchar(100) NULL
LastName                nvarchar(100)
DateOfBirth             date
Gender                  tinyint
NationalityCode         nvarchar(10) NULL
Phone                   nvarchar(30) NULL
Email                   nvarchar(256) NULL
Status                  tinyint
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Unique

```text
SchoolId + StudentNumber
```

## Indexes

```text
SchoolId + Status

LastName + FirstName
```

---

# 20. enrollment.Guardians

```text
GuardianId              uniqueidentifier PK
SchoolId                uniqueidentifier
FirstName               nvarchar(100)
LastName                nvarchar(100)
PhoneNumber             nvarchar(30)
Email                   nvarchar(256) NULL
NationalId              nvarchar(100) NULL
Status                  tinyint
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Indexes

```text
SchoolId + PhoneNumber

SchoolId + Email
```

Potential duplicate detection remains application-driven.

---

# 21. enrollment.StudentGuardians

```text
StudentGuardianId       uniqueidentifier PK
StudentId               uniqueidentifier
GuardianId              uniqueidentifier
RelationshipType        tinyint
IsPrimary               bit
CreatedAtUtc            datetime2
```

## FKs

Within Enrollment:

```text
StudentId -> Students
GuardianId -> Guardians
```

## Unique

```text
StudentId + GuardianId
```

## Index

```text
GuardianId
```

A filtered unique index can enforce one primary guardian:

```text
StudentId
WHERE IsPrimary = 1
```

---

# 22. enrollment.Enrollments

```text
EnrollmentId            uniqueidentifier PK
SchoolId                uniqueidentifier
StudentId               uniqueidentifier
AcademicYearId          uniqueidentifier
GradeId                 uniqueidentifier
ClassId                 uniqueidentifier
EnrollmentDate          date
Status                  tinyint
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Internal FK

Only:

```text
StudentId -> enrollment.Students
```

No FKs to:

```text
academic.AcademicYears
academic.Grades
academic.Classes
```

because they belong to another bounded context.

## Critical Unique Rule

One active enrollment per student per academic year.

SQL Server can use a filtered unique index:

```text
UX_Enrollments_ActiveStudentYear

StudentId,
AcademicYearId

WHERE Status = Active
```

Depending on numeric enum mapping.

## Other Indexes

```text
ClassId + Status

AcademicYearId + GradeId

StudentId
```

---

# Enrollment ERD

```text
Student
   │
   ├────< StudentGuardians >──── Guardian
   │
   └────< Enrollment
```

Logical external references:

```text
Enrollment
   |
   +---- AcademicYearId
   +---- GradeId
   +---- ClassId

          ↓ logical contract

      Academic Module
```

---

# 23. enrollment.OutboxMessages

```text
OutboxMessageId         uniqueidentifier PK
OccurredOnUtc           datetime2
Type                    nvarchar(500)
Content                 nvarchar(max)
ProcessedOnUtc          datetime2 NULL
Error                   nvarchar(max) NULL
RetryCount              int
```

## Index

```text
ProcessedOnUtc,
OccurredOnUtc
```

Used for:

```text
StudentEnrolledIntegrationEvent
StudentWithdrawnIntegrationEvent
```

---

# Part VI — Attendance Schema

Tables:

```text
attendance.AttendanceSessions
attendance.AttendanceRecords
attendance.OutboxMessages
```

---

# 24. attendance.AttendanceSessions

```text
AttendanceSessionId     uniqueidentifier PK
SchoolId                uniqueidentifier
AcademicYearId          uniqueidentifier
ClassId                 uniqueidentifier
AttendanceDate          date
RecordedByTeacherId     uniqueidentifier
Status                  tinyint
SubmittedAtUtc          datetime2 NULL
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

These references are logical:

```text
AcademicYearId
ClassId
RecordedByTeacherId
```

No cross-context FKs.

## Unique

For basic MVP:

```text
ClassId + AttendanceDate
```

Later, if multiple sessions per day are needed:

```text
ClassId
AttendanceDate
PeriodId
```

---

# 25. attendance.AttendanceRecords

```text
AttendanceRecordId      uniqueidentifier PK
AttendanceSessionId     uniqueidentifier
StudentId               uniqueidentifier
Status                  tinyint
Notes                   nvarchar(500) NULL
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
```

## FK

```text
AttendanceSessionId
 -> AttendanceSessions
```

## Unique

```text
AttendanceSessionId
StudentId
```

StudentId is cross-context logical reference only.

## Index

```text
StudentId
```

This index is important for:

```text
View Student Attendance
```

---

# Attendance ERD

```text
AttendanceSession
       │
       │ 1
       │
       *
AttendanceRecord
```

External logical IDs:

```text
ClassId
TeacherId
StudentId
AcademicYearId
```

---

# Part VII — Finance Schema

Tables:

```text
finance.FeeTypes
finance.FeePlans
finance.Invoices
finance.InvoiceLines
finance.Payments
finance.OutboxMessages
finance.InboxMessages
```

---

# 26. finance.FeeTypes

```text
FeeTypeId               uniqueidentifier PK
SchoolId                uniqueidentifier
Code                    nvarchar(50)
Name                    nvarchar(150)
Description             nvarchar(500) NULL
IsActive                bit
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
```

## Unique

```text
SchoolId + Code
```

---

# 27. finance.FeePlans

```text
FeePlanId               uniqueidentifier PK
SchoolId                uniqueidentifier
AcademicYearId          uniqueidentifier
GradeId                 uniqueidentifier NULL
FeeTypeId               uniqueidentifier
Amount                  decimal(18,2)
Currency                char(3)
DueDate                 date
IsActive                bit
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Internal FK

```text
FeeTypeId -> FeeTypes
```

AcademicYearId and GradeId are logical external references.

## Check

```text
Amount > 0
```

---

# 28. finance.Invoices

```text
InvoiceId               uniqueidentifier PK
SchoolId                uniqueidentifier
StudentId               uniqueidentifier
AcademicYearId          uniqueidentifier
InvoiceNumber           nvarchar(50)
IssueDate               date
DueDate                 date
Currency                char(3)
Status                  tinyint
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Unique

```text
SchoolId + InvoiceNumber
```

## Indexes

```text
StudentId + Status

DueDate + Status

AcademicYearId
```

---

# 29. finance.InvoiceLines

```text
InvoiceLineId           uniqueidentifier PK
InvoiceId               uniqueidentifier
FeeTypeId               uniqueidentifier
Description             nvarchar(500)
Quantity                decimal(18,2)
UnitPrice               decimal(18,2)
CreatedAtUtc            datetime2
```

## FKs

```text
InvoiceId -> Invoices

FeeTypeId -> FeeTypes
```

## Checks

```text
Quantity > 0

UnitPrice >= 0
```

Line total should preferably be calculated by domain code.

---

# 30. finance.Payments

```text
PaymentId               uniqueidentifier PK
InvoiceId               uniqueidentifier
Amount                  decimal(18,2)
Currency                char(3)
PaymentMethod           tinyint
TransactionReference    nvarchar(150) NULL
PaymentDate             datetime2
RecordedByUserId        uniqueidentifier
CreatedAtUtc            datetime2
```

## FK

```text
InvoiceId -> Invoices
```

## Check

```text
Amount > 0
```

## Indexes

```text
InvoiceId

TransactionReference
```

No direct payment deletion should be permitted after financial posting.

Corrections should later be represented by:

- Reversal
- Adjustment

rather than DELETE.

---

# Finance ERD

```text
FeeType
   │
   ├────< FeePlan
   │
   └────< InvoiceLine
              │
              *
              │
              1
           Invoice
              │
              │ 1
              │
              *
           Payment
```

---

# 31. finance.InboxMessages

Required for idempotent asynchronous processing.

```text
InboxMessageId          uniqueidentifier PK
IntegrationEventId      uniqueidentifier
Consumer                nvarchar(300)
ProcessedOnUtc          datetime2 NULL
Error                   nvarchar(max) NULL
```

## Unique

```text
IntegrationEventId
Consumer
```

Example:

Finance consumes:

```text
StudentEnrolledIntegrationEvent
```

If the message is delivered twice, only one invoice should be created.

---

# Part VIII — Notifications Schema

Tables:

```text
notifications.Notifications
notifications.InboxMessages
notifications.OutboxMessages
```

---

# 32. notifications.Notifications

```text
NotificationId          uniqueidentifier PK
SchoolId                uniqueidentifier
RecipientUserId         uniqueidentifier
StudentId               uniqueidentifier NULL
SourceEventId           uniqueidentifier NULL
Type                    nvarchar(100)
Channel                 tinyint
Subject                 nvarchar(300)
Body                    nvarchar(max)
Status                  tinyint
CreatedAtUtc            datetime2
SentAtUtc               datetime2 NULL
FailedAtUtc             datetime2 NULL
FailureReason           nvarchar(2000) NULL
Version                 rowversion
```

## Indexes

```text
RecipientUserId + CreatedAtUtc DESC

Status + CreatedAtUtc

SourceEventId
```

---

# 33. notifications.InboxMessages

Same idempotency concept as Finance.

```text
InboxMessageId
IntegrationEventId
Consumer
ProcessedOnUtc
Error
```

Unique:

```text
IntegrationEventId + Consumer
```

---

# Part IX — Smart Insights Schema

Tables:

```text
insights.StudentRiskProfiles
insights.RiskFactors
insights.InboxMessages
```

---

# 34. insights.StudentRiskProfiles

```text
StudentRiskProfileId    uniqueidentifier PK
SchoolId                uniqueidentifier
StudentId               uniqueidentifier
AcademicYearId          uniqueidentifier
RiskScore               decimal(5,2)
RiskLevel               tinyint
LastCalculatedAtUtc     datetime2
CreatedAtUtc            datetime2
ModifiedAtUtc           datetime2 NULL
Version                 rowversion
```

## Unique

```text
StudentId + AcademicYearId
```

## Indexes

```text
RiskLevel

AcademicYearId + RiskLevel
```

---

# 35. insights.RiskFactors

```text
RiskFactorId            uniqueidentifier PK
StudentRiskProfileId    uniqueidentifier
FactorType              nvarchar(100)
RawValue                decimal(18,4) NULL
Weight                  decimal(9,4)
ContributionScore       decimal(9,4)
Description             nvarchar(500)
CreatedAtUtc            datetime2
```

## FK

```text
StudentRiskProfileId
 -> StudentRiskProfiles
```

---

# Part X — Complete Logical ERD

```text
                         PLATFORM
                       +---------+
                       | School  |
                       +---------+
                             |
             logical SchoolId reference
                             |
        +--------------------+-------------------+
        |                    |                   |
    IDENTITY              ACADEMIC           ENROLLMENT
        |                    |                   |
      Users            AcademicYear           Student
       |  \                 |                 /     \
       |   \              Terms      StudentGuardian Guardian
       |    Roles            |                 |
       |      \              |                 |
       |     Permissions   Classes         Enrollment
       |
  RefreshTokens


                 ACADEMIC
                    |
            TeacherAssignment
           /        |        \
      Teacher      Class     Subject


              ENROLLMENT
                   |
           StudentEnrolled
          Integration Event
          /       |        \
         /        |         \
    FINANCE   NOTIFICATION   INSIGHTS


ATTENDANCE
    |
AttendanceSession
    |
AttendanceRecords
    |
StudentAbsentIntegrationEvent
            |
            +--------> Notifications
            |
            +--------> Smart Insights


FINANCE
   |
Invoice
 /      \
Lines   Payments
```

---

# Part XI — Cross-Module Relationship Matrix

| Column | Owner | Reference Type |
|---|---|---|
| Users.SchoolId | Platform | Logical |
| Teachers.UserId | Identity | Logical |
| Enrollment.AcademicYearId | Academic | Logical |
| Enrollment.GradeId | Academic | Logical |
| Enrollment.ClassId | Academic | Logical |
| Attendance.ClassId | Academic | Logical |
| Attendance.RecordedByTeacherId | Academic | Logical |
| AttendanceRecord.StudentId | Enrollment | Logical |
| Invoice.StudentId | Enrollment | Logical |
| Invoice.AcademicYearId | Academic | Logical |
| Notification.RecipientUserId | Identity | Logical |
| RiskProfile.StudentId | Enrollment | Logical |

These must **not** generate EF navigation properties between modules.

---

# Part XII — Recommended Index Strategy

Indexes should support actual use cases rather than indexing every FK automatically.

Important indexes:

```text
Users
SchoolId + NormalizedUsername

Students
SchoolId + StudentNumber

Enrollments
StudentId + AcademicYearId

Enrollments
ClassId + Status

AttendanceSessions
ClassId + AttendanceDate

AttendanceRecords
StudentId

Invoices
StudentId + Status

Invoices
DueDate + Status

Notifications
RecipientUserId + CreatedAtUtc

RiskProfiles
AcademicYearId + RiskLevel
```

Avoid over-indexing because every index increases write cost.

---

# Part XIII — Database Migration Strategy

Each module gets its own:

```text
DbContext
Migration set
Migration history table
SQL schema
```

Example:

```text
IdentityDbContext
AcademicDbContext
EnrollmentDbContext
AttendanceDbContext
FinanceDbContext
NotificationDbContext
InsightsDbContext
```

---

# 36. Separate Migration History Per Module

Identity:

```text
identity.__EFMigrationsHistory
```

Academic:

```text
academic.__EFMigrationsHistory
```

Enrollment:

```text
enrollment.__EFMigrationsHistory
```

etc.

Configuration example:

```csharp
options.UseSqlServer(
    connectionString,
    sql =>
    {
        sql.MigrationsHistoryTable(
            "__EFMigrationsHistory",
            "identity");
    });
```

This is important because each module owns its migrations independently.

---

# 37. Migration Folders

Recommended:

```text
Modules
│
├── Identity
│   └── SmartSchool.Modules.Identity.Infrastructure
│       └── Persistence
│           └── Migrations
│
├── Academic
│   └── SmartSchool.Modules.Academic.Infrastructure
│       └── Persistence
│           └── Migrations
```

---

# 38. Migration Naming Convention

Use:

```text
YYYYMMDD_HHMM_Description
```

or meaningful EF names such as:

```text
InitialIdentitySchema

AddRefreshTokens

AddRolePermissions

InitialAcademicSchema

AddTeacherAssignments
```

Never create names such as:

```text
Migration1
UpdateDB
FixTable
FinalMigration
```

---

# 39. Creating Identity Migration

Example:

```bash
dotnet ef migrations add InitialIdentitySchema \
  --project src/Modules/Identity/SmartSchool.Modules.Identity.Infrastructure \
  --startup-project src/SmartSchool.Api \
  --context IdentityDbContext
```

Then:

```bash
dotnet ef database update \
  --project src/Modules/Identity/SmartSchool.Modules.Identity.Infrastructure \
  --startup-project src/SmartSchool.Api \
  --context IdentityDbContext
```

---

# 40. Production Migration Rule

For local development:

```text
dotnet ef database update
```

is acceptable.

For production:

Do not automatically migrate blindly during application startup.

Preferred:

```text
CI/CD
   |
Generate idempotent migration script
   |
Review
   |
Execute migration
   |
Deploy application
```

Example:

```bash
dotnet ef migrations script \
    --idempotent \
    --context IdentityDbContext \
    --output identity-migration.sql
```

---

# 41. Production Rollback Strategy

Prefer:

**Forward-fix migrations**

instead of relying heavily on automatic downgrade.

Example:

Bad production workflow:

```text
Deploy
↓
Failure
↓
dotnet ef database update PreviousMigration
```

Better:

```text
Backward-compatible DB migration

↓

Deploy application

↓

Remove obsolete database object in later deployment
```

---

# 42. Expand-and-Contract Pattern

Example:

Rename:

```text
Name
```

to:

```text
DisplayName
```

Do not immediately drop `Name`.

Phase 1:

```text
Add DisplayName
```

Phase 2:

Application writes both.

Phase 3:

Backfill data.

Phase 4:

Application reads DisplayName.

Phase 5:

Drop Name in later deployment.

Important for zero/low-downtime deployments.

---

# 43. Seed Data Strategy

Do not put large business data inside `HasData`.

Recommended startup/admin seeders:

```text
IdentityPermissionSeeder
SystemRoleSeeder
DevelopmentSchoolSeeder
```

Initial system roles:

```text
SchoolAdmin
Teacher
Parent
Student
Accountant
AcademicSupervisor
```

Initial permission codes should be stable.

---

# Part XIV — Solution Structure

Recommended final structure:

```text
SmartSchool
│
├── src
│   │
│   ├── SmartSchool.Api
│   │
│   ├── BuildingBlocks
│   │   │
│   │   ├── SmartSchool.SharedKernel
│   │   ├── SmartSchool.Application.Abstractions
│   │   ├── SmartSchool.Infrastructure
│   │   └── SmartSchool.EventBus
│   │
│   └── Modules
│       │
│       ├── Identity
│       │   ├── SmartSchool.Modules.Identity.Domain
│       │   ├── SmartSchool.Modules.Identity.Application
│       │   ├── SmartSchool.Modules.Identity.Infrastructure
│       │   └── SmartSchool.Modules.Identity.Presentation
│       │
│       ├── Academic
│       │
│       ├── Enrollment
│       │
│       ├── Attendance
│       │
│       ├── Finance
│       │
│       ├── Notifications
│       │
│       └── SmartInsights
│
├── tests
│   │
│   ├── ArchitectureTests
│   │
│   ├── IntegrationTests
│   │
│   └── Modules
│       ├── Identity.UnitTests
│       ├── Academic.UnitTests
│       ├── Enrollment.UnitTests
│       ├── Attendance.UnitTests
│       └── Finance.UnitTests
│
├── docker
│
├── docs
│   ├── BRD
│   ├── UseCases
│   ├── Domain
│   ├── ERD
│   ├── ADR
│   └── API
│
├── Directory.Build.props
├── Directory.Packages.props
├── docker-compose.yml
└── SmartSchool.sln
```

---

# Part XV — Identity Clean Architecture

Identity will demonstrate classical Clean Architecture.

Dependency rule:

```text
Presentation
      |
      v
Application
      |
      v
Domain

Infrastructure
      |
      +------> Application
      |
      +------> Domain
```

Domain knows nothing about:

- EF Core
- SQL Server
- ASP.NET Core
- JWT
- Redis
- Email

---

# 44. Identity Project Structure

```text
Identity
│
├── Domain
│   │
│   ├── Users
│   │   ├── User.cs
│   │   ├── UserId.cs
│   │   ├── UserStatus.cs
│   │   └── Events
│   │
│   ├── Roles
│   │   ├── Role.cs
│   │   └── Permission.cs
│   │
│   └── Abstractions
│
├── Application
│   │
│   ├── Authentication
│   │   ├── Login
│   │   └── RefreshToken
│   │
│   ├── Users
│   │   ├── CreateUser
│   │   ├── GetUser
│   │   └── ActivateUser
│   │
│   ├── Roles
│   │   └── AssignRole
│   │
│   └── Abstractions
│
├── Infrastructure
│   │
│   ├── Persistence
│   │   ├── IdentityDbContext.cs
│   │   ├── Configurations
│   │   └── Migrations
│   │
│   ├── Authentication
│   │   ├── JwtTokenProvider.cs
│   │   └── PasswordHasher.cs
│   │
│   └── Repositories
│
└── Presentation
    ├── AuthenticationController.cs
    ├── UsersController.cs
    └── RolesController.cs
```

---

# Part XVI — First Identity Domain Model

## User Aggregate

```csharp
public sealed class User : AggregateRoot
{
    private readonly List<UserRole> _roles = [];

    private User()
    {
    }

    private User(
        Guid id,
        Guid schoolId,
        string username,
        string email,
        string passwordHash,
        string firstName,
        string lastName)
    {
        Id = id;
        SchoolId = schoolId;
        Username = username;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        Status = UserStatus.Active;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid SchoolId { get; private set; }

    public string Username { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string PasswordHash { get; private set; } = null!;

    public string FirstName { get; private set; } = null!;

    public string LastName { get; private set; } = null!;

    public UserStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? LastLoginAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles;

    public static User Create(
        Guid schoolId,
        string username,
        string email,
        string passwordHash,
        string firstName,
        string lastName)
    {
        var user = new User(
            Guid.CreateVersion7(),
            schoolId,
            username,
            email,
            passwordHash,
            firstName,
            lastName);

        user.RaiseDomainEvent(
            new UserCreatedDomainEvent(user.Id));

        return user;
    }

    public void Deactivate()
    {
        if (Status == UserStatus.Inactive)
            return;

        Status = UserStatus.Inactive;

        RaiseDomainEvent(
            new UserDeactivatedDomainEvent(Id));
    }

    public void Activate()
    {
        Status = UserStatus.Active;
    }

    public void RegisterLogin()
    {
        LastLoginAtUtc = DateTime.UtcNow;
    }
}
```

---

# 45. UserStatus

```csharp
public enum UserStatus
{
    Inactive = 0,
    Active = 1,
    Suspended = 2
}
```

---

# 46. User Repository Contract

Located in:

```text
Identity.Application
```

or Domain depending on chosen repository convention.

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<User?> GetByUsernameAsync(
        Guid schoolId,
        string username,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        Guid schoolId,
        string email,
        CancellationToken cancellationToken = default);

    void Add(User user);
}
```

Implementation belongs in:

```text
Identity.Infrastructure
```

---

# 47. Password Abstraction

Application:

```csharp
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(
        string password,
        string passwordHash);
}
```

Infrastructure provides implementation.

The Domain never hashes passwords itself.

---

# 48. Token Provider

Application abstraction:

```csharp
public interface ITokenProvider
{
    string CreateAccessToken(User user);
}
```

Infrastructure:

```text
JwtTokenProvider
```

---

# 49. Create User Command

```csharp
public sealed record CreateUserCommand(
    Guid SchoolId,
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName);
```

Handler flow:

```text
CreateUserCommand

↓

Validate input

↓

Check username

↓

Check email

↓

Hash password

↓

User.Create()

↓

Repository.Add()

↓

SaveChanges

↓

Return UserId
```

---

# 50. Login Flow

```text
POST /api/identity/auth/login

↓

LoginCommand

↓

Find User

↓

Check status

↓

Verify password

↓

Load roles + permissions

↓

Generate JWT

↓

Create Refresh Token

↓

Return authentication result
```

---

# 51. Initial API Endpoints

Authentication:

```http
POST /api/identity/auth/login

POST /api/identity/auth/refresh

POST /api/identity/auth/logout
```

Users:

```http
POST /api/identity/users

GET /api/identity/users/{id}

PUT /api/identity/users/{id}/activate

PUT /api/identity/users/{id}/deactivate
```

Roles:

```http
POST /api/identity/roles

POST /api/identity/users/{userId}/roles/{roleId}

DELETE /api/identity/users/{userId}/roles/{roleId}
```

---

# Part XVII — Module Registration

Each module exposes only one public startup method.

For example:

```csharp
builder.Services
    .AddIdentityModule(configuration);
```

and:

```csharp
app.MapIdentityEndpoints();
```

The main API should not know internal Identity implementation details.

---

# 52. SmartSchool.Api Program.cs

Conceptually:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddIdentityModule(builder.Configuration);

builder.Services
    .AddAcademicModule(builder.Configuration);

builder.Services
    .AddEnrollmentModule(builder.Configuration);

builder.Services
    .AddAttendanceModule(builder.Configuration);

builder.Services
    .AddFinanceModule(builder.Configuration);

builder.Services
    .AddNotificationsModule(builder.Configuration);

builder.Services
    .AddSmartInsightsModule(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();

app.UseAuthorization();

app.MapIdentityEndpoints();

app.MapAcademicEndpoints();

app.MapEnrollmentEndpoints();

app.MapAttendanceEndpoints();

app.MapFinanceEndpoints();

app.Run();
```

---

# Part XVIII — Development Order

Now that the business/domain/database design is established, implementation should proceed as follows:

```text
STEP 01
Create SmartSchool solution

↓

STEP 02
Create Shared Kernel

↓

STEP 03
Create Identity projects

↓

STEP 04
Implement User Aggregate

↓

STEP 05
Implement Role + Permission

↓

STEP 06
Implement IdentityDbContext

↓

STEP 07
Create EF mappings

↓

STEP 08
Create first migration

↓

STEP 09
Implement Create User

↓

STEP 10
Implement Login

↓

STEP 11
Implement JWT authentication

↓

STEP 12
Implement permission authorization

↓

STEP 13
Unit Tests

↓

STEP 14
Integration Tests

↓

STEP 15
Architecture Tests

↓

STEP 16
Move to Academic Module
```

---

# Part XIX — Architecture Testing Rules

Architecture tests should enforce:

```text
Domain
must NOT reference
Infrastructure
```

```text
Domain
must NOT reference
Presentation
```

```text
Application
must NOT reference
Infrastructure
```

and especially:

```text
Identity.Domain
must NOT reference
Academic.*
```

```text
Finance.*
must NOT reference
Enrollment.Infrastructure
```

Modules communicate through explicit contracts only.

---

# Part XX — Current Project Baseline

At this point SmartSchool has:

```text
✓ Business vision

✓ BRD

✓ Detailed use cases

✓ Bounded contexts

✓ DDD domain model

✓ Aggregate boundaries

✓ Logical ERD

✓ Physical ERD

✓ PK/FK strategy

✓ Constraints

✓ Index strategy

✓ Schema isolation strategy

✓ Migration strategy

✓ Modular Monolith structure

✓ Identity Clean Architecture design
```

The next implementation milestone is:

**SmartSchool Solution Bootstrap + Identity Module Implementation.**

The first runnable vertical scenario should be:

```text
Create School

↓

Seed Administrator Role

↓

Create User

↓

Assign Administrator Role

↓

Login

↓

Generate JWT

↓

Access protected endpoint
```