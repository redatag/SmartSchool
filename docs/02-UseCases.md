# SmartSchool
## Detailed Use Cases Specification — MVP v1.0

---

# 1. Purpose

This document describes the detailed business use cases for the SmartSchool MVP.

Each use case defines:

- Actor
- Preconditions
- Trigger
- Main Flow
- Alternative Flows
- Business Rules
- Postconditions
- Events
- Related Module

This document will be used as an input for:

- Domain Modeling
- DDD Aggregate Design
- ERD
- API Design
- Module Contracts
- Test Cases

---

# 2. Main Actors

The MVP contains the following actors:

- Super Administrator
- School Administrator
- Teacher
- Parent / Guardian
- Student
- Accountant
- Academic Supervisor
- System Background Worker

---

# 3. Identity & Access Use Cases

## UC-001 — Login

### Actor

Any registered user

### Module

Identity & Access

### Preconditions

- User account exists.
- User account is active.
- User has at least one role.

### Trigger

User opens the login page and attempts to access the system.

### Main Flow

1. User enters username or email.
2. User enters password.
3. System validates the submitted credentials.
4. System checks whether the account is active.
5. System loads the user's roles and permissions.
6. System creates an authenticated session or JWT token.
7. System records the successful login.
8. System redirects the user to the appropriate dashboard.

### Alternative Flows

#### Invalid Credentials

1. Credentials are incorrect.
2. System rejects the login attempt.
3. System displays a generic authentication error.

#### Account Disabled

1. Credentials are valid.
2. Account status is inactive or suspended.
3. System blocks login.
4. System displays an account-disabled message.

### Business Rules

- Password must never be stored in plain text.
- Disabled users cannot authenticate.
- Failed attempts should be logged.
- Authentication errors must not reveal sensitive information.

### Postconditions

User has a valid authenticated session.

### Events

Optional:

- UserLoggedInDomainEvent

---

## UC-002 — Logout

### Actor

Authenticated user

### Module

Identity & Access

### Preconditions

User is authenticated.

### Main Flow

1. User selects logout.
2. System invalidates the active session or token.
3. User is redirected to the login page.

### Postconditions

User is no longer authenticated.

---

## UC-003 — Create User

### Actor

School Administrator

### Module

Identity & Access

### Preconditions

- Administrator is authenticated.
- Administrator has ManageUsers permission.

### Main Flow

1. Administrator selects Add User.
2. Administrator enters:
   - Full name
   - Email
   - Username
   - Phone number
3. Administrator selects initial role.
4. System validates input.
5. System checks uniqueness of username and email.
6. System creates user.
7. System assigns the selected role.
8. System optionally sends account activation notification.

### Alternative Flows

#### Email Already Exists

System rejects the operation.

#### Username Already Exists

System rejects the operation.

### Business Rules

- Email must be unique where required.
- Username must be unique.
- Every user must have at least one role.

### Postconditions

New user exists in the system.

### Events

- UserCreatedDomainEvent
- UserCreatedIntegrationEvent, if required

---

## UC-004 — Assign Role

### Actor

School Administrator

### Module

Identity & Access

### Preconditions

- User exists.
- Role exists.
- Administrator has ManageRoles permission.

### Main Flow

1. Administrator searches for user.
2. Administrator opens user permissions.
3. Administrator selects one or more roles.
4. System validates roles.
5. System updates user-role assignments.
6. System saves the changes.

### Business Rules

- Invalid roles cannot be assigned.
- Role changes should be auditable.

### Postconditions

User permissions are updated.

---

# 4. Academic Management Use Cases

## UC-005 — Create Academic Year

### Actor

School Administrator

### Module

Academic

### Preconditions

Administrator has academic-configuration permissions.

### Main Flow

1. Administrator selects Academic Years.
2. Administrator selects Create.
3. Administrator enters:
   - Name
   - Start date
   - End date
4. Administrator saves.
5. System validates dates.
6. System creates academic year.

### Alternative Flows

#### Invalid Date Range

Start date is after or equal to end date.

System rejects the operation.

### Business Rules

- Start date must be earlier than end date.
- Academic year name should be unique within the school.

### Example

2026 / 2027

### Events

- AcademicYearCreatedDomainEvent

---

## UC-006 — Create Term

### Actor

School Administrator

### Module

Academic

### Preconditions

Academic year exists.

### Main Flow

1. Administrator selects academic year.
2. Administrator creates a term.
3. Administrator enters:
   - Term name
   - Start date
   - End date
4. System validates that term dates fall inside the academic year.
5. System saves the term.

### Business Rules

- Term must belong to one academic year.
- Term dates must fall within academic-year dates.
- Overlapping terms may be prohibited depending on school policy.

---

## UC-007 — Create Grade

### Actor

School Administrator

### Module

Academic

### Main Flow

1. Administrator selects Grades.
2. Administrator selects Add Grade.
3. Administrator enters:
   - Name
   - Code
   - Sequence number
4. System validates information.
5. System creates grade.

### Examples

- Grade 1
- Grade 2
- Primary 1
- Year 5

### Business Rules

- Grade name or code must be unique within school scope.

---

## UC-008 — Create Class

### Actor

School Administrator

### Module

Academic

### Preconditions

- Academic year exists.
- Grade exists.

### Main Flow

1. Administrator selects academic year.
2. Administrator selects grade.
3. Administrator selects Add Class.
4. Administrator enters:
   - Class name
   - Capacity
5. System creates class.

### Example

Grade 5

Class 5-A

Capacity: 30

### Business Rules

- Capacity must be greater than zero.
- Class must belong to a grade.
- Class must belong to an academic year.
- Class name must be unique within grade and academic year.

---

## UC-009 — Create Subject

### Actor

School Administrator

### Module

Academic

### Main Flow

1. Administrator selects Subjects.
2. Administrator enters:
   - Subject name
   - Code
3. System validates uniqueness.
4. System creates subject.

### Examples

- Mathematics
- English
- Science
- Arabic

---

## UC-010 — Register Teacher

### Actor

School Administrator

### Module

Academic

### Main Flow

1. Administrator selects Teachers.
2. Administrator enters teacher information.
3. System validates required information.
4. Teacher record is created.
5. Optional user account is linked.

### Teacher Information

- Teacher ID
- Full name
- Email
- Phone
- Specialization
- Status

### Events

- TeacherRegisteredDomainEvent

---

## UC-011 — Assign Teacher

### Actor

School Administrator

### Module

Academic

### Preconditions

- Teacher exists.
- Subject exists.
- Class exists.
- Academic year exists.

### Main Flow

1. Administrator selects teacher.
2. Administrator selects academic year.
3. Administrator selects class.
4. Administrator selects subject.
5. System validates assignment.
6. System creates TeacherAssignment.

### Business Rules

A teacher may teach:

- Multiple subjects
- Multiple classes

Subject-class assignments must be valid for the selected academic year.

---

# 5. Student & Guardian Use Cases

## UC-012 — Register Student

### Actor

School Administrator

### Module

Enrollment

### Preconditions

Administrator has ManageStudents permission.

### Main Flow

1. Administrator selects Add Student.
2. Administrator enters personal information.
3. System validates mandatory information.
4. System checks for possible duplicate student.
5. System generates Student Number.
6. Student profile is created.
7. System displays the newly created profile.

### Student Information

- Student Number
- First Name
- Middle Name
- Last Name
- Date of Birth
- Gender
- Nationality
- Phone
- Email
- Status

### Example Student Number

STU-2026-000001

### Business Rules

- Student number must be unique.
- Student creation does not automatically mean enrollment.
- Student information must belong to a school.

### Events

- StudentRegisteredDomainEvent

---

## UC-013 — Register Guardian

### Actor

School Administrator

### Module

Enrollment

### Main Flow

1. Administrator selects Add Guardian.
2. Administrator enters:
   - Full name
   - Phone
   - Email
   - National ID if required
3. System validates guardian information.
4. System creates guardian.

### Business Rules

- Same guardian may be linked to several students.
- Duplicate detection should use email, phone, and other identifiers.

---

## UC-014 — Link Guardian to Student

### Actor

School Administrator

### Module

Enrollment

### Preconditions

- Student exists.
- Guardian exists.

### Main Flow

1. Administrator opens student profile.
2. Administrator selects Add Guardian.
3. Administrator selects existing guardian or creates one.
4. Administrator selects relationship type.
5. Administrator selects whether guardian is primary.
6. System creates relationship.

### Relationship Types

- Father
- Mother
- Legal Guardian
- Other

### Business Rules

- Student may have multiple guardians.
- Guardian may have multiple students.
- Only one primary guardian should normally exist per student.

---

# 6. Enrollment Use Cases

## UC-015 — Enroll Student

### Actor

School Administrator

### Module

Enrollment

### Preconditions

- Student exists.
- Active academic year exists.
- Grade exists.
- Class exists.
- Student is not already actively enrolled in the same academic year.

### Trigger

Administrator decides to enroll a student.

### Main Flow

1. Administrator searches for student.
2. Administrator selects Enroll.
3. Administrator selects academic year.
4. Administrator selects grade.
5. Administrator selects class.
6. Enrollment module requests class details from Academic module.
7. Academic module confirms:
   - Class exists
   - Grade is valid
   - Academic year is valid
   - Capacity information
8. Enrollment module checks existing enrollment.
9. System creates Enrollment aggregate.
10. Enrollment becomes Active.
11. Transaction is committed.
12. StudentEnrolledIntegrationEvent is written to Outbox.
13. Background worker publishes the integration event.

### Asynchronous Consumers

Finance Module:

Creates initial fees or invoice when configuration allows.

Notification Module:

Sends enrollment confirmation.

Smart Insights:

Initializes StudentRiskProfile.

### Alternative Flow — Duplicate Enrollment

1. Student already has active enrollment.
2. System rejects operation.

### Alternative Flow — Full Class

1. Class capacity has been reached.
2. System rejects enrollment.
3. Administrator may select another class.

### Business Rules

- One active enrollment per student per academic year.
- Enrollment cannot exceed class capacity.
- Grade/class relationship must be valid.
- Only active/open academic years may accept enrollment.

### Postconditions

Student is actively enrolled.

### Events

Domain:

- StudentEnrolledDomainEvent

Integration:

- StudentEnrolledIntegrationEvent

---

## UC-016 — Change Enrollment Status

### Actor

School Administrator

### Module

Enrollment

### Preconditions

Enrollment exists.

### Possible Statuses

- Pending
- Active
- Suspended
- Withdrawn
- Completed

### Main Flow

1. Administrator opens enrollment.
2. Administrator selects new status.
3. System validates status transition.
4. Enrollment status changes.
5. System records timestamp and actor.

### Business Rules

Invalid transitions must not be allowed.

Example:

Completed → Active

may require special authorization.

### Events

Possible:

- StudentWithdrawnIntegrationEvent
- EnrollmentCompletedIntegrationEvent

---

## UC-017 — View Class Students

### Actor

Teacher / Administrator

### Module

Enrollment

### Preconditions

User is authorized to access the class.

### Main Flow

1. User selects academic year.
2. User selects class.
3. System retrieves active enrollments.
4. System returns list of students.

### Output

- Student Number
- Name
- Enrollment Status
- Basic profile information

---

# 7. Attendance Use Cases

## UC-018 — Record Attendance

### Actor

Teacher

### Module

Attendance

### Preconditions

- Teacher is authenticated.
- Teacher is assigned to class.
- Class exists.
- Students are actively enrolled.

### Main Flow

1. Teacher selects Today's Attendance.
2. System shows assigned classes.
3. Teacher selects class.
4. Attendance module obtains current class students.
5. System displays enrolled students.
6. Teacher marks each student:
   - Present
   - Absent
   - Late
   - Excused
7. Teacher submits attendance.
8. System validates attendance session.
9. Attendance records are stored.
10. System identifies absent students.
11. StudentAbsentIntegrationEvents are saved in Outbox.
12. Background worker publishes events.
13. Notification module processes absence notifications.

### Business Rules

- Attendance cannot normally be recorded twice for the same session.
- Teacher must be assigned to the class.
- Student must belong to the class.
- Attendance date cannot normally be outside academic-year boundaries.

### Events

- AttendanceRecordedDomainEvent
- StudentAbsentIntegrationEvent

---

## UC-019 — View Student Attendance

### Actor

Parent / Teacher / Administrator / Academic Supervisor

### Module

Attendance

### Main Flow

1. User selects student.
2. User chooses date range.
3. System retrieves attendance.
4. System calculates summary.

### Output Example

Total School Days: 50

Present: 43

Absent: 4

Late: 2

Excused: 1

Attendance Rate: 86%

### Authorization Rules

Parent may only see linked children.

Teacher may only see authorized students.

---

# 8. Finance Use Cases

## UC-020 — Create Fee Type

### Actor

Accountant

### Module

Finance

### Main Flow

1. Accountant selects Fee Types.
2. Accountant selects Add.
3. Accountant enters:
   - Name
   - Description
   - Optional default amount
4. System saves fee type.

### Examples

- Tuition
- Books
- Transportation
- Activities

---

## UC-021 — Create Invoice

### Actor

Accountant or System

### Module

Finance

### Preconditions

Student exists as an external finance reference.

### Main Flow

1. Accountant selects student.
2. Accountant selects fee.
3. Accountant enters:
   - Amount
   - Due date
   - Description
4. System generates invoice number.
5. Invoice aggregate is created.
6. Status becomes Issued.
7. InvoiceCreatedIntegrationEvent is produced.

### Automatic Flow

The invoice may also be generated automatically after:

StudentEnrolledIntegrationEvent

### Business Rules

- Amount must be greater than zero.
- Invoice number must be unique.
- Due date must be valid.
- Finalized financial transactions cannot be physically deleted.

### Events

- InvoiceCreatedDomainEvent
- InvoiceCreatedIntegrationEvent

---

## UC-022 — Record Payment

### Actor

Accountant

### Module

Finance

### Preconditions

- Invoice exists.
- Invoice is not cancelled.
- Outstanding balance exists.

### Main Flow

1. Accountant opens invoice.
2. Accountant selects Record Payment.
3. Accountant enters:
   - Amount
   - Payment method
   - Transaction reference
   - Payment date
4. System validates payment.
5. Payment is added to Invoice aggregate.
6. Invoice recalculates balance.
7. Invoice status changes if necessary.
8. System commits transaction.
9. PaymentReceivedIntegrationEvent is generated.

### Example

Invoice:

10,000

Payment:

4,000

Balance:

6,000

Status:

Partially Paid

### Business Rules

- Payment must be greater than zero.
- Payment cannot exceed outstanding balance unless overpayment is supported.
- Paid invoice cannot normally accept new payment.

### Events

- PaymentRecordedDomainEvent
- PaymentReceivedIntegrationEvent

---

## UC-023 — View Student Balance

### Actor

Parent / Accountant / Administrator

### Module

Finance

### Main Flow

1. User selects student.
2. System retrieves open invoices.
3. System calculates:
   - Total invoiced
   - Total paid
   - Outstanding balance
4. System displays payment history.

### Authorization

Parent may only see invoices for linked children.

---

# 9. Notification Use Cases

## UC-024 — Send Notification

### Actor

System Background Worker

### Module

Notifications

### Trigger

Integration event is received.

Possible triggers include:

- StudentEnrolledIntegrationEvent
- StudentAbsentIntegrationEvent
- InvoiceCreatedIntegrationEvent
- PaymentReceivedIntegrationEvent

### Main Flow

1. Notification module receives integration event.
2. Module identifies recipients.
3. Notification template is selected.
4. Notification entity is created.
5. Notification is sent through configured channel.
6. Delivery status is recorded.

### Channels in MVP

- In-App
- Email

### Future Channels

- SMS
- WhatsApp
- Mobile Push

### Business Rules

- Notifications must be idempotent where possible.
- Duplicate processing must not result in duplicate critical notifications.

---

# 10. Dashboard Use Cases

## UC-025 — View Parent Dashboard

### Actor

Parent

### Modules

Multiple read models

### Preconditions

Parent account is linked to at least one student.

### Main Flow

1. Parent logs in.
2. System identifies linked students.
3. Parent selects child.
4. Dashboard retrieves:
   - Attendance summary
   - Current enrollment
   - Outstanding invoices
   - Recent payments
   - Notifications
   - Risk information if permitted
5. System displays dashboard.

### Security Rule

Parent cannot access another parent's child.

---

## UC-026 — View Teacher Dashboard

### Actor

Teacher

### Main Flow

1. Teacher logs in.
2. System retrieves assigned classes.
3. System displays:
   - Classes
   - Subjects
   - Student counts
   - Pending attendance tasks
4. Teacher navigates to selected class.

---

## UC-027 — View Administrator Dashboard

### Actor

School Administrator

### Main Flow

System displays aggregated information such as:

- Total active students
- Total teachers
- Total classes
- Today's attendance
- Outstanding payments
- Recent enrollments
- Important notifications

### Architectural Note

Dashboard should preferably use specialized read models rather than loading multiple domain aggregates.

---

# 11. Smart Insights Use Cases

## UC-028 — Calculate Student Risk

### Actor

System Background Worker / Academic Supervisor

### Module

Smart Insights

### Trigger

The risk score may be recalculated:

- After attendance events
- On scheduled background job
- Manually by authorized user

### Input

Initial MVP inputs:

- Total attendance days
- Absence count
- Late count
- Attendance percentage

### Example Rule-Based Algorithm

Attendance >= 90%

Risk = Low

Attendance 75%–89%

Risk = Medium

Attendance < 75%

Risk = High

Other factors may increase score.

### Main Flow

1. Smart Insights receives attendance data/event.
2. System updates StudentRiskProfile.
3. Risk calculation executes.
4. System assigns numeric score.
5. System assigns category.
6. System saves contributing factors.

### Example Result

Student:

Ahmed Mohamed

Risk Score:

78 / 100

Risk Level:

High

Reasons:

- Attendance below 75%
- Five absences this month
- Multiple late attendance records

### Business Rules

Risk calculation must remain explainable.

The initial implementation should not claim to be an ML prediction model.

### Future Enhancement

Replace or augment rules with:

- Logistic Regression
- Random Forest
- XGBoost

Possible explainability:

- SHAP

---

# 12. Integration Event Matrix

| Producer | Event | Consumer |
|---|---|---|
| Enrollment | StudentEnrolledIntegrationEvent | Finance |
| Enrollment | StudentEnrolledIntegrationEvent | Notifications |
| Enrollment | StudentEnrolledIntegrationEvent | Smart Insights |
| Attendance | StudentAbsentIntegrationEvent | Notifications |
| Attendance | AttendanceRecordedIntegrationEvent | Smart Insights |
| Finance | InvoiceCreatedIntegrationEvent | Notifications |
| Finance | PaymentReceivedIntegrationEvent | Notifications |

---

# 13. Synchronous Module Communication Matrix

| Caller | Provider | Purpose |
|---|---|---|
| Enrollment | Academic | Validate class and academic year |
| Attendance | Academic | Validate teacher-class assignment |
| Attendance | Enrollment | Retrieve active class students |
| Finance | Enrollment | Resolve student basic reference when required |

Synchronous communication should only be used when the caller requires an immediate response to complete the current use case.

---

# 14. Asynchronous Communication Rules

Asynchronous integration should be preferred when:

- The consumer does not need to complete before the current transaction finishes.
- Multiple modules need to react to the same business event.
- Temporary consumer failure should not break the source business transaction.

Example:

Enrollment should succeed even if the email service is temporarily unavailable.

Therefore:

Enrollment

must not synchronously call:

Notification Email Service

during the enrollment transaction.

---

# 15. Outbox Processing Use Case

## UC-SYS-001 — Publish Outbox Messages

### Actor

Background Worker

### Preconditions

Committed outbox messages exist.

### Main Flow

1. Worker queries unpublished outbox messages.
2. Worker deserializes integration event.
3. Worker publishes event to internal event bus.
4. Consumers process the event.
5. Message is marked as processed.

### Failure Flow

If processing fails:

1. Message remains available for retry.
2. Failure is logged.
3. Retry count may be increased.
4. Message may eventually move to an error state.

---

# 16. Authorization Matrix

| Feature | Admin | Teacher | Parent | Accountant | Supervisor |
|---|---:|---:|---:|---:|---:|
| Academic setup | Yes | No | No | No | View |
| Register student | Yes | No | No | No | No |
| Enrollment | Yes | No | No | No | View |
| View class students | Yes | Yes | Child only | No | Yes |
| Record attendance | Override | Yes | No | No | View |
| View attendance | Yes | Assigned | Child only | No | Yes |
| Create invoice | Optional | No | No | Yes | No |
| Record payment | No | No | No | Yes | No |
| View balance | Yes | No | Child only | Yes | No |
| View risk | Yes | Assigned/optional | Optional | No | Yes |

Final permissions should be implemented using permissions rather than only checking role names.

---

# 17. Critical End-to-End Use Case

## E2E-001 — Student Enrollment to Parent Notification

### Flow

Administrator registers student.

↓

Administrator links guardian.

↓

Administrator enrolls student.

↓

Enrollment validates Academic module synchronously.

↓

Enrollment transaction commits.

↓

StudentEnrolledIntegrationEvent stored in Outbox.

↓

Background Worker publishes event.

↓

Finance creates student invoice.

↓

Finance publishes InvoiceCreatedIntegrationEvent.

↓

Notifications receive enrollment event.

↓

Parent receives enrollment notification.

↓

Notifications receive invoice event.

↓

Parent receives fee notification.

↓

Student appears in teacher's class.

↓

Teacher records attendance.

↓

Student is absent.

↓

StudentAbsentIntegrationEvent is published.

↓

Parent receives absence notification.

↓

Smart Insights recalculates risk score.

This scenario should become one of the main automated integration tests of SmartSchool.

---

# 18. Traceability Example

BRD Requirement:

FR-030 Student Enrollment

↓

Use Case:

UC-015 Enroll Student

↓

Domain:

Enrollment Aggregate

↓

Application:

EnrollStudentCommand

↓

API:

POST /api/enrollments

↓

Integration Event:

StudentEnrolledIntegrationEvent

↓

Tests:

EnrollStudentTests

This traceability should be maintained for critical business features.

---

# 19. Use Case Priority

## MVP Critical

- UC-001 Login
- UC-003 Create User
- UC-005 Create Academic Year
- UC-007 Create Grade
- UC-008 Create Class
- UC-009 Create Subject
- UC-010 Register Teacher
- UC-011 Assign Teacher
- UC-012 Register Student
- UC-013 Register Guardian
- UC-014 Link Guardian
- UC-015 Enroll Student
- UC-017 View Class Students
- UC-018 Record Attendance
- UC-019 View Attendance
- UC-020 Create Fee Type
- UC-021 Create Invoice
- UC-022 Record Payment
- UC-023 View Balance
- UC-024 Send Notification

## MVP Important

- Dashboards
- Smart Insights
- Enrollment status management

---

# 20. Next Design Artifacts

The use cases provide enough information to begin domain discovery.

The next artifacts should be created in this order:

1. Domain Model
2. Bounded Context refinement
3. Aggregate identification
4. Entity and Value Object identification
5. Domain Events
6. Context Map
7. Logical ERD
8. Physical ERD
9. Module contracts
10. API contracts
11. Solution structure
12. Implementation