# SmartSchool MVP — DDD Domain Model & Aggregate Design v1.0

## 1. Bounded Contexts
- Identity
- Academic
- Enrollment
- Attendance
- Finance
- Notifications
- Smart Insights

Each bounded context is implemented as an isolated module in the Modular Monolith.

## 2. Shared Kernel
Keep it small.

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

Do not put business aggregates in SharedKernel.

## 3. Identity Context

### User Aggregate Root
Properties:
- UserId
- SchoolId
- Username
- Email
- PasswordHash
- FirstName
- LastName
- PhoneNumber
- Status
- LastLoginAtUtc
- CreatedAtUtc

Child Entity:
- UserRole

Behavior:
- Create
- Activate
- Deactivate
- RegisterLogin
- AssignRole
- RemoveRole

Domain Events:
- UserCreatedDomainEvent
- UserActivatedDomainEvent
- UserDeactivatedDomainEvent
- RoleAssignedDomainEvent

### Role Aggregate Root
Properties:
- RoleId
- SchoolId
- Name
- Description
- IsSystemRole

Child Entity:
- RolePermission

Permission examples:
- students.view
- students.manage
- attendance.view
- attendance.record
- finance.invoice.view
- finance.invoice.create
- finance.payment.create

Use permission-based authorization rather than hard-coded role checks.

## 4. Academic Context

### AcademicYear Aggregate Root
Properties:
- AcademicYearId
- SchoolId
- Name
- StartDate
- EndDate
- Status

Child Entity:
- Term

Invariants:
- StartDate < EndDate.
- Term range must fall within academic year.
- Invalid term overlaps may be rejected by policy.

Events:
- AcademicYearCreatedDomainEvent
- AcademicYearActivatedDomainEvent
- AcademicYearClosedDomainEvent
- TermAddedDomainEvent

### Grade Aggregate Root
Properties:
- GradeId
- SchoolId
- Name
- Code
- Sequence
- IsActive

### Class Aggregate Root
Properties:
- ClassId
- SchoolId
- AcademicYearId
- GradeId
- Name
- Capacity
- Status

Important:
Class does not contain Students.
Enrollment owns membership.

### Subject Aggregate Root
Properties:
- SubjectId
- SchoolId
- Name
- Code
- IsActive

### Teacher Aggregate Root
Properties:
- TeacherId
- SchoolId
- UserId optional
- EmployeeNumber
- Name
- Email
- Phone
- Specialization
- Status

### TeacherAssignment Aggregate Root
Properties:
- TeacherAssignmentId
- TeacherId
- AcademicYearId
- ClassId
- SubjectId
- StartDate
- EndDate optional
- Status

Reason for separate aggregate:
Teacher assignments have their own lifecycle and validation.

## 5. Enrollment Context

### Student Aggregate Root
Represents who the student is.

Properties:
- StudentId
- SchoolId
- StudentNumber
- StudentName
- DateOfBirth
- Gender
- Nationality
- Status

Child Entity:
- StudentGuardian

Behavior:
- AddGuardian
- RemoveGuardian
- SetPrimaryGuardian

Invariants:
- Unique StudentNumber within school.
- Date of birth cannot be in the future.
- Duplicate guardian link not allowed.
- Normally only one primary guardian.

### Guardian Aggregate Root
Properties:
- GuardianId
- SchoolId
- Name
- Phone
- Email
- NationalId optional
- Status

### Enrollment Aggregate Root
Represents where/when the student is enrolled.

Properties:
- EnrollmentId
- SchoolId
- StudentId
- AcademicYearId
- GradeId
- ClassId
- EnrollmentDate
- Status

Statuses:
- Pending
- Active
- Suspended
- Withdrawn
- Completed

Behavior:
- Activate
- Suspend
- Withdraw
- Complete
- ChangeClass

Invariants:
- One active enrollment per student per academic year.
- Class/grade/year must be valid.
- Capacity must not be exceeded.
- Status transitions must be valid.

Events:
- StudentEnrolledDomainEvent
- EnrollmentSuspendedDomainEvent
- StudentWithdrawnDomainEvent
- EnrollmentCompletedDomainEvent
- StudentClassChangedDomainEvent

Critical decision:
Student and Enrollment are separate Aggregate Roots.

## 6. Enrollment Capacity Ownership
Academic owns:
- Class existence
- Class status
- Class configured capacity
- Grade/year relationship

Enrollment owns:
- Student memberships
- Active enrollment counts

Recommended synchronous contract:
Academic returns ClassSnapshot:
- ClassId
- GradeId
- AcademicYearId
- Capacity
- Status

Enrollment compares capacity against its own active enrollment count.

## 7. Attendance Context

### AttendanceSession Aggregate Root
Properties:
- AttendanceSessionId
- SchoolId
- AcademicYearId
- ClassId
- AttendanceDate
- RecordedByTeacherId
- Status
- SubmittedAtUtc

Child Entity:
- AttendanceRecord

AttendanceRecord:
- AttendanceRecordId
- StudentId
- Status
- Notes

Attendance statuses:
- Present
- Absent
- Late
- Excused

Invariants:
- One record per student per session.
- No duplicate StudentId inside session.
- Submitted session cannot normally be submitted again.
- Teacher must be authorized for the class.

Behavior:
- MarkPresent
- MarkAbsent
- MarkLate
- MarkExcused
- Submit

Events:
- AttendanceSessionSubmittedDomainEvent
- StudentMarkedAbsentDomainEvent
- StudentMarkedLateDomainEvent

Critical decision:
AttendanceSession is Aggregate Root.
AttendanceRecord is not independently persisted through its own repository.

## 8. Finance Context

### FeeType Aggregate Root
Properties:
- FeeTypeId
- SchoolId
- Name
- Code
- Description
- IsActive

### FeePlan Aggregate Root
Properties:
- FeePlanId
- SchoolId
- AcademicYearId
- GradeId optional
- FeeTypeId
- Amount
- Currency
- DueDate
- IsActive

### Invoice Aggregate Root
Properties:
- InvoiceId
- SchoolId
- StudentId
- AcademicYearId
- InvoiceNumber
- IssueDate
- DueDate
- Currency
- Status

Child Entities:
- InvoiceLine
- Payment

InvoiceLine:
- InvoiceLineId
- FeeTypeId
- Description
- Quantity
- UnitPrice

Payment:
- PaymentId
- Amount
- Currency
- PaymentMethod
- TransactionReference
- PaymentDate
- RecordedByUserId

Invoice statuses:
- Draft
- Issued
- PartiallyPaid
- Paid
- Overdue
- Cancelled

Behavior:
- AddLine
- RemoveLine
- Issue
- RecordPayment
- Cancel
- MarkOverdue

Invariants:
- Invoice must contain at least one line before issue.
- Total > 0.
- Payment > 0.
- Payment cannot exceed outstanding balance unless overpayment is explicitly implemented.
- Cancelled invoice cannot receive payment.
- Fully paid invoice cannot normally receive more payment.

Domain Events:
- InvoiceCreatedDomainEvent
- InvoiceIssuedDomainEvent
- PaymentRecordedDomainEvent
- InvoicePaidDomainEvent
- InvoiceCancelledDomainEvent
- InvoiceOverdueDomainEvent

Critical decision:
Invoice is Aggregate Root.
Payment is a child Entity inside Invoice.

Do not create IPaymentRepository.

## 9. Notifications Context

### Notification Aggregate Root
Properties:
- NotificationId
- SchoolId
- RecipientUserId
- StudentId optional
- SourceEventId optional
- Type
- Channel
- Subject
- Body
- Status
- CreatedAtUtc
- SentAtUtc
- FailedAtUtc
- FailureReason

Channels:
- InApp
- Email

Future:
- SMS
- WhatsApp
- Push

Statuses:
- Pending
- Processing
- Sent
- Failed

Behavior:
- MarkProcessing
- MarkSent
- MarkFailed

Idempotency:
Consume integration events through Inbox and prevent duplicate critical notifications.

## 10. Smart Insights Context

### StudentRiskProfile Aggregate Root
Properties:
- StudentRiskProfileId
- SchoolId
- StudentId
- AcademicYearId
- Score
- RiskLevel
- LastCalculatedAtUtc

Child Entity:
- RiskFactor

RiskFactor:
- RiskFactorId
- Type
- RawValue
- Weight
- ContributionScore
- Description

Risk levels:
- Low
- Medium
- High

Behavior:
- Recalculate
- AddFactor
- RemoveExpiredFactors
- ChangeRiskLevel

Domain Event:
- StudentRiskLevelChangedDomainEvent

Possible Integration Event:
- HighRiskStudentDetectedIntegrationEvent

## 11. Domain Service Candidates
Use only when behavior does not naturally belong to an aggregate.

Candidates:
- EnrollmentCapacityPolicy
- StudentRiskCalculator
- InvoiceNumberGenerator

## 12. Value Objects
Candidates:
- Money
- DateRange
- EmailAddress
- PhoneNumber
- StudentName
- StudentNumber
- InvoiceNumber
- RiskScore

## 13. Domain Event vs Integration Event
Example:

Enrollment aggregate raises:
StudentEnrolledDomainEvent

Application event handler translates it to:
StudentEnrolledIntegrationEvent

Integration event is written to Outbox and later consumed by:
- Finance
- Notifications
- Smart Insights

## 14. Repository Strategy
Repositories primarily for Aggregate Roots.

Recommended:
- IUserRepository
- IRoleRepository
- IAcademicYearRepository
- IGradeRepository
- IClassRepository
- ITeacherRepository
- ITeacherAssignmentRepository
- IStudentRepository
- IGuardianRepository
- IEnrollmentRepository
- IAttendanceSessionRepository
- IFeeTypeRepository
- IFeePlanRepository
- IInvoiceRepository
- INotificationRepository
- IStudentRiskProfileRepository

Avoid:
- IPaymentRepository
- IAttendanceRecordRepository
- IInvoiceLineRepository

## 15. Aggregate Transaction Rule
A transaction should normally mutate one aggregate.

Cross-aggregate/module reactions should occur via domain/integration events rather than one large transaction.

Examples:
- Enroll student -> Enrollment transaction -> Outbox -> Finance/Notifications/Insights.
- Record payment -> Invoice transaction -> Outbox -> Notification.
