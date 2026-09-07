# SmartSchool MVP — Logical & Physical ERD v1.0

## 1. Database Layout
Use one SQL Server database with separate schemas:

- platform
- identity
- academic
- enrollment
- attendance
- finance
- notifications
- insights

Each module owns its schema, DbContext, migrations, and migration history table.

## 2. Cross-Module Database Rule
Do not create SQL foreign keys or EF navigation properties across bounded-context/module boundaries.

Cross-module relationships are logical ID references only.

Inside the same module, normal relational foreign keys are allowed.

## 3. Common Conventions
Typical audit columns:
- CreatedAtUtc datetime2
- CreatedBy uniqueidentifier NULL
- ModifiedAtUtc datetime2 NULL
- ModifiedBy uniqueidentifier NULL

Aggregate roots may include:
- Version rowversion

IDs:
- uniqueidentifier
- Application-generated with Guid.CreateVersion7() where supported

## 4. platform.Schools
Columns:
- SchoolId uniqueidentifier PK
- Code nvarchar(30) NOT NULL
- Name nvarchar(200) NOT NULL
- CountryCode char(2) NOT NULL
- TimeZoneId nvarchar(100) NOT NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Constraints/Indexes:
- PK_Schools(SchoolId)
- UX_Schools_Code(Code)
- IX_Schools_Status(Status)

## 5. Identity Schema

### identity.Users
- UserId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- Username nvarchar(100) NOT NULL
- NormalizedUsername nvarchar(100) NOT NULL
- Email nvarchar(256) NOT NULL
- NormalizedEmail nvarchar(256) NOT NULL
- PasswordHash nvarchar(500) NOT NULL
- FirstName nvarchar(100) NOT NULL
- LastName nvarchar(100) NOT NULL
- PhoneNumber nvarchar(30) NULL
- Status tinyint NOT NULL
- EmailConfirmed bit NOT NULL
- PhoneConfirmed bit NOT NULL
- LastLoginAtUtc datetime2 NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Indexes:
- UX_Users_School_NormalizedUsername(SchoolId, NormalizedUsername)
- UX_Users_School_NormalizedEmail(SchoolId, NormalizedEmail)
- IX_Users_SchoolId_Status(SchoolId, Status)

SchoolId is a logical cross-module reference to platform.Schools; no FK.

### identity.Roles
- RoleId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- Name nvarchar(100) NOT NULL
- NormalizedName nvarchar(100) NOT NULL
- Description nvarchar(500) NULL
- IsSystemRole bit NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Unique:
- UX_Roles_School_NormalizedName(SchoolId, NormalizedName)

### identity.Permissions
- PermissionId uniqueidentifier PK
- Code nvarchar(150) NOT NULL
- Name nvarchar(150) NOT NULL
- Module nvarchar(100) NOT NULL
- Description nvarchar(500) NULL

Unique:
- UX_Permissions_Code(Code)

Index:
- IX_Permissions_Module(Module)

### identity.UserRoles
- UserId uniqueidentifier NOT NULL
- RoleId uniqueidentifier NOT NULL
- AssignedAtUtc datetime2 NOT NULL
- AssignedBy uniqueidentifier NULL

PK:
- (UserId, RoleId)

FKs inside Identity:
- UserId -> identity.Users.UserId
- RoleId -> identity.Roles.RoleId

### identity.RolePermissions
- RoleId uniqueidentifier NOT NULL
- PermissionId uniqueidentifier NOT NULL
- AssignedAtUtc datetime2 NOT NULL

PK:
- (RoleId, PermissionId)

FKs:
- RoleId -> identity.Roles.RoleId
- PermissionId -> identity.Permissions.PermissionId

### identity.RefreshTokens
- RefreshTokenId uniqueidentifier PK
- UserId uniqueidentifier NOT NULL
- TokenHash nvarchar(500) NOT NULL
- ExpiresAtUtc datetime2 NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- RevokedAtUtc datetime2 NULL
- ReplacedByTokenId uniqueidentifier NULL
- CreatedByIp nvarchar(64) NULL
- RevokedByIp nvarchar(64) NULL

FK:
- UserId -> identity.Users.UserId

Indexes:
- IX_RefreshTokens_UserId(UserId)
- IX_RefreshTokens_ExpiresAtUtc(ExpiresAtUtc)

Do not store raw refresh token values when avoidable.

## 6. Academic Schema

### academic.AcademicYears
- AcademicYearId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- Name nvarchar(50) NOT NULL
- StartDate date NOT NULL
- EndDate date NOT NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Unique:
- UX_AcademicYears_School_Name(SchoolId, Name)

Check:
- StartDate < EndDate

### academic.Terms
- TermId uniqueidentifier PK
- AcademicYearId uniqueidentifier NOT NULL
- Name nvarchar(100) NOT NULL
- Sequence int NOT NULL
- StartDate date NOT NULL
- EndDate date NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL

FK:
- AcademicYearId -> academic.AcademicYears.AcademicYearId

Unique:
- (AcademicYearId, Name)
- (AcademicYearId, Sequence)

Check:
- StartDate < EndDate

### academic.Grades
- GradeId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- Code nvarchar(30) NOT NULL
- Name nvarchar(100) NOT NULL
- Sequence int NOT NULL
- IsActive bit NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Unique:
- (SchoolId, Code)

Index:
- (SchoolId, IsActive)

### academic.Classes
- ClassId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- AcademicYearId uniqueidentifier NOT NULL
- GradeId uniqueidentifier NOT NULL
- Name nvarchar(100) NOT NULL
- Capacity int NOT NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

FKs inside Academic:
- AcademicYearId -> academic.AcademicYears
- GradeId -> academic.Grades

Unique:
- (AcademicYearId, GradeId, Name)

Check:
- Capacity > 0

Index:
- (AcademicYearId, GradeId)

### academic.Subjects
- SubjectId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- Code nvarchar(30) NOT NULL
- Name nvarchar(150) NOT NULL
- IsActive bit NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL

Unique:
- (SchoolId, Code)

### academic.Teachers
- TeacherId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- UserId uniqueidentifier NULL
- EmployeeNumber nvarchar(50) NOT NULL
- FirstName nvarchar(100) NOT NULL
- LastName nvarchar(100) NOT NULL
- Email nvarchar(256) NULL
- PhoneNumber nvarchar(30) NULL
- Specialization nvarchar(200) NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Unique:
- (SchoolId, EmployeeNumber)

UserId logically references Identity only; no SQL FK.

### academic.TeacherAssignments
- TeacherAssignmentId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- TeacherId uniqueidentifier NOT NULL
- AcademicYearId uniqueidentifier NOT NULL
- ClassId uniqueidentifier NOT NULL
- SubjectId uniqueidentifier NOT NULL
- StartDate date NOT NULL
- EndDate date NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

FKs inside Academic:
- TeacherId -> Teachers
- AcademicYearId -> AcademicYears
- ClassId -> Classes
- SubjectId -> Subjects

Indexes:
- (ClassId, AcademicYearId)
- (TeacherId, AcademicYearId)

Active assignment uniqueness should be enforced using a suitable filtered unique index or application/domain invariant based on the final numeric status mapping.

## 7. Enrollment Schema

### enrollment.Students
- StudentId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- StudentNumber nvarchar(50) NOT NULL
- FirstName nvarchar(100) NOT NULL
- MiddleName nvarchar(100) NULL
- LastName nvarchar(100) NOT NULL
- DateOfBirth date NOT NULL
- Gender tinyint NOT NULL
- NationalityCode nvarchar(10) NULL
- Phone nvarchar(30) NULL
- Email nvarchar(256) NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Unique:
- (SchoolId, StudentNumber)

Indexes:
- (SchoolId, Status)
- (LastName, FirstName)

### enrollment.Guardians
- GuardianId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- FirstName nvarchar(100) NOT NULL
- LastName nvarchar(100) NOT NULL
- PhoneNumber nvarchar(30) NOT NULL
- Email nvarchar(256) NULL
- NationalId nvarchar(100) NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Indexes:
- (SchoolId, PhoneNumber)
- (SchoolId, Email)

### enrollment.StudentGuardians
- StudentGuardianId uniqueidentifier PK
- StudentId uniqueidentifier NOT NULL
- GuardianId uniqueidentifier NOT NULL
- RelationshipType tinyint NOT NULL
- IsPrimary bit NOT NULL
- CreatedAtUtc datetime2 NOT NULL

FKs:
- StudentId -> enrollment.Students
- GuardianId -> enrollment.Guardians

Unique:
- (StudentId, GuardianId)

Index:
- GuardianId

Filtered unique index:
- StudentId WHERE IsPrimary = 1

### enrollment.Enrollments
- EnrollmentId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- StudentId uniqueidentifier NOT NULL
- AcademicYearId uniqueidentifier NOT NULL
- GradeId uniqueidentifier NOT NULL
- ClassId uniqueidentifier NOT NULL
- EnrollmentDate date NOT NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Internal FK:
- StudentId -> enrollment.Students

No FKs to Academic tables.

Indexes:
- ClassId + Status
- AcademicYearId + GradeId
- StudentId

Critical filtered unique rule:
- One active enrollment per StudentId + AcademicYearId.
Implement via filtered unique index once the numeric value of Active is fixed.

### enrollment.OutboxMessages
- OutboxMessageId uniqueidentifier PK
- OccurredOnUtc datetime2 NOT NULL
- Type nvarchar(500) NOT NULL
- Content nvarchar(max) NOT NULL
- ProcessedOnUtc datetime2 NULL
- Error nvarchar(max) NULL
- RetryCount int NOT NULL

Index:
- (ProcessedOnUtc, OccurredOnUtc)

## 8. Attendance Schema

### attendance.AttendanceSessions
- AttendanceSessionId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- AcademicYearId uniqueidentifier NOT NULL
- ClassId uniqueidentifier NOT NULL
- AttendanceDate date NOT NULL
- RecordedByTeacherId uniqueidentifier NOT NULL
- Status tinyint NOT NULL
- SubmittedAtUtc datetime2 NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

No cross-module FKs.

Basic MVP unique:
- (ClassId, AttendanceDate)

If multiple daily periods are introduced later, replace with:
- (ClassId, AttendanceDate, PeriodId)

### attendance.AttendanceRecords
- AttendanceRecordId uniqueidentifier PK
- AttendanceSessionId uniqueidentifier NOT NULL
- StudentId uniqueidentifier NOT NULL
- Status tinyint NOT NULL
- Notes nvarchar(500) NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL

FK:
- AttendanceSessionId -> attendance.AttendanceSessions

Unique:
- (AttendanceSessionId, StudentId)

Index:
- StudentId

### attendance.OutboxMessages
Same reliable-outbox pattern as Enrollment.

## 9. Finance Schema

### finance.FeeTypes
- FeeTypeId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- Code nvarchar(50) NOT NULL
- Name nvarchar(150) NOT NULL
- Description nvarchar(500) NULL
- IsActive bit NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL

Unique:
- (SchoolId, Code)

### finance.FeePlans
- FeePlanId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- AcademicYearId uniqueidentifier NOT NULL
- GradeId uniqueidentifier NULL
- FeeTypeId uniqueidentifier NOT NULL
- Amount decimal(18,2) NOT NULL
- Currency char(3) NOT NULL
- DueDate date NOT NULL
- IsActive bit NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Internal FK:
- FeeTypeId -> finance.FeeTypes

AcademicYearId and GradeId are logical references only.

Check:
- Amount > 0

### finance.Invoices
- InvoiceId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- StudentId uniqueidentifier NOT NULL
- AcademicYearId uniqueidentifier NOT NULL
- InvoiceNumber nvarchar(50) NOT NULL
- IssueDate date NOT NULL
- DueDate date NOT NULL
- Currency char(3) NOT NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Unique:
- (SchoolId, InvoiceNumber)

Indexes:
- (StudentId, Status)
- (DueDate, Status)
- AcademicYearId

### finance.InvoiceLines
- InvoiceLineId uniqueidentifier PK
- InvoiceId uniqueidentifier NOT NULL
- FeeTypeId uniqueidentifier NOT NULL
- Description nvarchar(500) NOT NULL
- Quantity decimal(18,2) NOT NULL
- UnitPrice decimal(18,2) NOT NULL
- CreatedAtUtc datetime2 NOT NULL

FKs:
- InvoiceId -> finance.Invoices
- FeeTypeId -> finance.FeeTypes

Checks:
- Quantity > 0
- UnitPrice >= 0

### finance.Payments
- PaymentId uniqueidentifier PK
- InvoiceId uniqueidentifier NOT NULL
- Amount decimal(18,2) NOT NULL
- Currency char(3) NOT NULL
- PaymentMethod tinyint NOT NULL
- TransactionReference nvarchar(150) NULL
- PaymentDate datetime2 NOT NULL
- RecordedByUserId uniqueidentifier NOT NULL
- CreatedAtUtc datetime2 NOT NULL

FK:
- InvoiceId -> finance.Invoices

Check:
- Amount > 0

Indexes:
- InvoiceId
- TransactionReference

RecordedByUserId is a logical reference to Identity only.

### finance.OutboxMessages
Reliable publication.

### finance.InboxMessages
- InboxMessageId uniqueidentifier PK
- IntegrationEventId uniqueidentifier NOT NULL
- Consumer nvarchar(300) NOT NULL
- ProcessedOnUtc datetime2 NULL
- Error nvarchar(max) NULL

Unique:
- (IntegrationEventId, Consumer)

Used to make consumers idempotent, e.g. StudentEnrolledIntegrationEvent must not create duplicate invoices.

## 10. Notifications Schema

### notifications.Notifications
- NotificationId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- RecipientUserId uniqueidentifier NOT NULL
- StudentId uniqueidentifier NULL
- SourceEventId uniqueidentifier NULL
- Type nvarchar(100) NOT NULL
- Channel tinyint NOT NULL
- Subject nvarchar(300) NOT NULL
- Body nvarchar(max) NOT NULL
- Status tinyint NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- SentAtUtc datetime2 NULL
- FailedAtUtc datetime2 NULL
- FailureReason nvarchar(2000) NULL
- Version rowversion

Indexes:
- (RecipientUserId, CreatedAtUtc)
- (Status, CreatedAtUtc)
- SourceEventId

### notifications.InboxMessages
Same idempotency model as Finance.

### notifications.OutboxMessages
Used if Notification later emits its own integration events.

## 11. Smart Insights Schema

### insights.StudentRiskProfiles
- StudentRiskProfileId uniqueidentifier PK
- SchoolId uniqueidentifier NOT NULL
- StudentId uniqueidentifier NOT NULL
- AcademicYearId uniqueidentifier NOT NULL
- RiskScore decimal(5,2) NOT NULL
- RiskLevel tinyint NOT NULL
- LastCalculatedAtUtc datetime2 NOT NULL
- CreatedAtUtc datetime2 NOT NULL
- ModifiedAtUtc datetime2 NULL
- Version rowversion

Unique:
- (StudentId, AcademicYearId)

Indexes:
- RiskLevel
- (AcademicYearId, RiskLevel)

### insights.RiskFactors
- RiskFactorId uniqueidentifier PK
- StudentRiskProfileId uniqueidentifier NOT NULL
- FactorType nvarchar(100) NOT NULL
- RawValue decimal(18,4) NULL
- Weight decimal(9,4) NOT NULL
- ContributionScore decimal(9,4) NOT NULL
- Description nvarchar(500) NOT NULL
- CreatedAtUtc datetime2 NOT NULL

FK:
- StudentRiskProfileId -> insights.StudentRiskProfiles

### insights.InboxMessages
Used for idempotent consumption of attendance/enrollment events.

## 12. Cross-Module Logical Reference Matrix

| Column | Logical Owner |
|---|---|
| identity.Users.SchoolId | Platform |
| academic.Teachers.UserId | Identity |
| enrollment.Enrollments.AcademicYearId | Academic |
| enrollment.Enrollments.GradeId | Academic |
| enrollment.Enrollments.ClassId | Academic |
| attendance.AttendanceSessions.AcademicYearId | Academic |
| attendance.AttendanceSessions.ClassId | Academic |
| attendance.AttendanceSessions.RecordedByTeacherId | Academic |
| attendance.AttendanceRecords.StudentId | Enrollment |
| finance.Invoices.StudentId | Enrollment |
| finance.Invoices.AcademicYearId | Academic |
| finance.Payments.RecordedByUserId | Identity |
| notifications.Notifications.RecipientUserId | Identity |
| notifications.Notifications.StudentId | Enrollment |
| insights.StudentRiskProfiles.StudentId | Enrollment |
| insights.StudentRiskProfiles.AcademicYearId | Academic |

These are logical references only and must not become SQL FKs or EF navigation properties across modules.

## 13. Migration Strategy
DbContexts:
- IdentityDbContext
- AcademicDbContext
- EnrollmentDbContext
- AttendanceDbContext
- FinanceDbContext
- NotificationDbContext
- InsightsDbContext

Migration history tables:
- identity.__EFMigrationsHistory
- academic.__EFMigrationsHistory
- enrollment.__EFMigrationsHistory
- attendance.__EFMigrationsHistory
- finance.__EFMigrationsHistory
- notifications.__EFMigrationsHistory
- insights.__EFMigrationsHistory

Production:
- Prefer reviewed/idempotent migration scripts.
- Do not blindly auto-migrate production on startup.
- Prefer expand-and-contract changes for breaking schema modifications.
- Prefer forward-fix migration strategy.

## 14. Initial Solution Structure

```text
SmartSchool
|
+-- src
|   +-- SmartSchool.Api
|   +-- BuildingBlocks
|   |   +-- SmartSchool.SharedKernel
|   |   +-- SmartSchool.Application.Abstractions
|   |   +-- SmartSchool.Infrastructure
|   |   +-- SmartSchool.EventBus
|   |
|   +-- Modules
|       +-- Identity
|       |   +-- SmartSchool.Modules.Identity.Domain
|       |   +-- SmartSchool.Modules.Identity.Application
|       |   +-- SmartSchool.Modules.Identity.Infrastructure
|       |   +-- SmartSchool.Modules.Identity.Presentation
|       |
|       +-- Academic
|       +-- Enrollment
|       +-- Attendance
|       +-- Finance
|       +-- Notifications
|       +-- SmartInsights
|
+-- tests
|   +-- SmartSchool.ArchitectureTests
|   +-- Modules
|       +-- SmartSchool.Modules.Identity.UnitTests
|       +-- SmartSchool.Modules.Identity.IntegrationTests
|
+-- docs
|   +-- 01-BRD.md
|   +-- 02-UseCases.md
|   +-- 03-Domain-Model.md
|   +-- 04-ERD.md
|
+-- AGENTS.md
+-- Directory.Build.props
+-- Directory.Packages.props
+-- SmartSchool.sln
```
