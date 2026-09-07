# SmartSchool MVP — Detailed Use Cases v1.0

## UC-001 Login
Actor: Registered User  
Module: Identity

Preconditions:
- User exists.
- User is active.
- User has at least one role.

Main Flow:
1. User enters username/email and password.
2. System validates credentials.
3. System checks account status.
4. System loads roles and permissions.
5. System issues authentication session/JWT.
6. System records successful login.
7. User is redirected to the relevant dashboard.

Alternative:
- Invalid credentials -> reject.
- Inactive/suspended user -> block login.

Postcondition:
- User is authenticated.

---

## UC-002 Logout
Actor: Authenticated User  
Module: Identity

Flow:
1. User requests logout.
2. Current refresh token/session is invalidated.
3. User is considered logged out.

---

## UC-003 Create User
Actor: School Administrator  
Module: Identity

Preconditions:
- Admin is authenticated.
- Admin has ManageUsers permission.

Flow:
1. Admin enters user information.
2. System validates input.
3. System checks username/email uniqueness.
4. System creates user.
5. Admin assigns one or more roles.
6. System saves the user.

Business Rules:
- Username must be unique within school.
- Email must be unique within school where configured.
- Every protected user needs at least one role.

---

## UC-004 Assign Role
Actor: School Administrator  
Module: Identity

Flow:
1. Admin selects user.
2. Admin selects role.
3. System validates role.
4. System prevents duplicate role assignment.
5. Assignment is stored.

---

## UC-005 Create Academic Year
Actor: School Administrator  
Module: Academic

Flow:
1. Admin enters name, start date, end date.
2. System validates date range.
3. Academic year is created.

Business Rules:
- StartDate < EndDate.
- Name unique within school.

---

## UC-006 Create Term
Actor: School Administrator  
Module: Academic

Preconditions:
- Academic year exists.

Flow:
1. Admin selects academic year.
2. Admin enters term name and dates.
3. System validates term boundaries.
4. Term is added to the AcademicYear aggregate.

---

## UC-007 Create Grade
Actor: School Administrator  
Module: Academic

Flow:
1. Admin enters grade name, code, and sequence.
2. System validates uniqueness.
3. Grade is created.

---

## UC-008 Create Class
Actor: School Administrator  
Module: Academic

Preconditions:
- Grade exists.
- Academic year exists.

Flow:
1. Admin selects grade and academic year.
2. Admin enters class name and capacity.
3. System validates data.
4. Class is created.

Business Rules:
- Capacity > 0.
- Class belongs to grade and academic year.

---

## UC-009 Create Subject
Actor: School Administrator  
Module: Academic

Flow:
1. Admin enters name and code.
2. System validates uniqueness.
3. Subject is created.

---

## UC-010 Register Teacher
Actor: School Administrator  
Module: Academic

Flow:
1. Admin enters teacher profile.
2. System validates required fields.
3. Teacher is registered.
4. Optional Identity UserId may be associated logically.

---

## UC-011 Assign Teacher
Actor: School Administrator  
Module: Academic

Preconditions:
- Teacher exists and is active.
- Subject exists.
- Class exists.
- Academic year exists.

Flow:
1. Admin selects teacher, class, subject, year.
2. System validates assignment.
3. TeacherAssignment is created.

---

## UC-012 Register Student
Actor: School Administrator  
Module: Enrollment

Flow:
1. Admin enters student profile.
2. System validates data.
3. System checks for possible duplicates.
4. System generates unique StudentNumber.
5. Student aggregate is created.

Business Rule:
Student creation is not the same as enrollment.

---

## UC-013 Register Guardian
Actor: School Administrator  
Module: Enrollment

Flow:
1. Admin enters guardian information.
2. System validates data.
3. Guardian is created.

---

## UC-014 Link Guardian to Student
Actor: School Administrator  
Module: Enrollment

Preconditions:
- Student exists.
- Guardian exists.

Flow:
1. Admin selects student and guardian.
2. Admin selects relationship type.
3. Admin marks primary guardian if applicable.
4. StudentGuardian relationship is created.

Business Rules:
- Duplicate student/guardian relationship not allowed.
- Only one primary guardian should normally exist.

---

## UC-015 Enroll Student
Actor: School Administrator  
Module: Enrollment

Preconditions:
- Student exists.
- Academic year exists.
- Grade exists.
- Class exists.
- Student does not already have active enrollment for the same academic year.

Main Flow:
1. Admin selects student.
2. Admin selects academic year, grade, and class.
3. Enrollment module calls Academic module contract.
4. Academic returns a class snapshot containing relevant configuration such as ClassId, GradeId, AcademicYearId, Capacity, Status.
5. Enrollment validates class/grade/year compatibility.
6. Enrollment checks its own active enrollment count for the class.
7. Enrollment validates capacity.
8. Enrollment aggregate is created/activated.
9. Transaction commits.
10. StudentEnrolledIntegrationEvent is written to Outbox.
11. Background dispatcher publishes it.

Asynchronous Consumers:
- Finance
- Notifications
- Smart Insights

Alternative:
- Duplicate active enrollment -> reject.
- Class full -> reject.

Important Ownership Rule:
Academic owns class capacity configuration.
Enrollment owns enrollment membership/count.

---

## UC-016 Change Enrollment Status
Actor: School Administrator  
Module: Enrollment

Statuses:
- Pending
- Active
- Suspended
- Withdrawn
- Completed

Flow:
1. Admin selects enrollment.
2. Admin requests status transition.
3. Enrollment aggregate validates allowed transition.
4. Status changes.
5. Relevant domain/integration events are raised.

---

## UC-017 View Class Students
Actor: Teacher / Administrator  
Module: Enrollment

Flow:
1. User selects class.
2. Enrollment returns active class enrollments.
3. Authorized student list is displayed.

---

## UC-018 Record Attendance
Actor: Teacher  
Module: Attendance

Preconditions:
- Teacher is authenticated.
- Teacher is assigned to class.
- Students are actively enrolled.

Main Flow:
1. Teacher selects class/date.
2. Attendance validates assignment through Academic module contract.
3. Attendance gets active class roster through Enrollment module contract or approved local read model.
4. Teacher marks students Present/Absent/Late/Excused.
5. AttendanceSession aggregate is created.
6. AttendanceRecords are added.
7. Session is submitted.
8. Transaction commits.
9. Attendance-related integration events are written to Outbox.
10. Notifications and Smart Insights consume them asynchronously.

Business Rules:
- No duplicate student in the same AttendanceSession.
- Session cannot normally be submitted twice.
- Teacher must be authorized for the class.

---

## UC-019 View Student Attendance
Actor: Parent / Teacher / Administrator / Academic Supervisor  
Module: Attendance

Flow:
1. User selects student and date range.
2. System retrieves attendance records.
3. System calculates summary:
   - Present
   - Absent
   - Late
   - Excused
   - Attendance rate

Authorization:
- Parent sees linked children only.
- Teacher sees authorized students only.

---

## UC-020 Create Fee Type
Actor: Accountant  
Module: Finance

Flow:
1. Accountant enters code, name, description.
2. System validates uniqueness.
3. FeeType is created.

---

## UC-021 Create Invoice
Actor: Accountant or System  
Module: Finance

Flow:
1. Student reference is identified.
2. Invoice lines are prepared.
3. Invoice aggregate validates amounts.
4. Invoice is issued.
5. InvoiceCreatedIntegrationEvent is published via Outbox.

Automatic Variant:
StudentEnrolledIntegrationEvent may cause invoice generation only when an applicable fee plan exists.

---

## UC-022 Record Payment
Actor: Accountant  
Module: Finance

Preconditions:
- Invoice exists.
- Invoice is not cancelled.
- Outstanding balance > 0.

Flow:
1. Accountant enters amount, method, transaction reference, date.
2. Invoice.RecordPayment() validates the payment.
3. Payment entity is added inside Invoice aggregate.
4. Outstanding balance/status is recalculated.
5. PaymentReceivedIntegrationEvent is stored in Outbox.

Business Rules:
- Amount > 0.
- Cannot exceed outstanding balance unless overpayment is explicitly supported.

---

## UC-023 View Student Balance
Actor: Parent / Accountant / Administrator  
Module: Finance

Flow:
1. User selects student.
2. Finance retrieves relevant invoices/payments.
3. System shows:
   - Total invoiced
   - Total paid
   - Outstanding
   - Payment history

---

## UC-024 Send Notification
Actor: System Background Worker  
Module: Notifications

Triggers may include:
- StudentEnrolledIntegrationEvent
- StudentAbsentIntegrationEvent
- AttendanceRecordedIntegrationEvent
- InvoiceCreatedIntegrationEvent
- PaymentReceivedIntegrationEvent

Flow:
1. Integration event is received.
2. Inbox idempotency is checked.
3. Recipient is resolved.
4. Template/channel is selected.
5. Notification entity is created.
6. Notification is sent.
7. Delivery status is stored.

MVP Channels:
- In-App
- Email

---

## UC-025 View Parent Dashboard
Actor: Parent

Flow:
1. Parent logs in.
2. System resolves linked children.
3. Parent selects child.
4. Dashboard composition/read model returns:
   - Enrollment
   - Attendance summary
   - Outstanding invoices
   - Recent payments
   - Notifications
   - Risk information if permitted

Do not implement dashboard through cross-module EF joins.

---

## UC-026 View Teacher Dashboard
Actor: Teacher

Shows:
- Assigned classes
- Subjects
- Student counts
- Pending attendance tasks

---

## UC-027 View Administrator Dashboard
Actor: School Administrator

Shows:
- Total active students
- Teachers
- Classes
- Today's attendance
- Outstanding payments
- Recent enrollments
- Important notifications

Prefer dedicated read models/composition instead of loading domain aggregates for dashboard queries.

---

## UC-028 Calculate Student Risk
Actor: System Background Worker / Academic Supervisor  
Module: Smart Insights

Triggers:
- Attendance event
- Scheduled recalculation
- Authorized manual recalculation

Initial Inputs:
- Attendance percentage
- Absence count
- Late count

Initial rule-based example:
- >= 90% attendance -> Low
- 75% to 89% -> Medium
- < 75% -> High

Flow:
1. Smart Insights receives data/event.
2. StudentRiskProfile is loaded/created.
3. Risk factors are updated.
4. Score is calculated.
5. Risk level is assigned.
6. Explanation factors are stored.

Important:
The MVP rule-based engine must not be presented as an ML prediction model.

Future:
- Logistic Regression
- Random Forest
- XGBoost
- SHAP explainability

---

# System Use Case — Outbox Processing

## UC-SYS-001 Publish Outbox Messages
Actor: Background Worker

Flow:
1. Query unprocessed Outbox messages.
2. Deserialize event.
3. Publish to internal event bus.
4. Consumers process event.
5. Mark message processed.

Failure:
- Keep/retry failed message.
- Record error and retry count.
- Use idempotent consumers.

---

# Integration Event Matrix

| Producer | Event | Consumers |
|---|---|---|
| Enrollment | StudentEnrolledIntegrationEvent | Finance, Notifications, Smart Insights |
| Enrollment | StudentWithdrawnIntegrationEvent | Finance/Notifications as needed |
| Attendance | AttendanceRecordedIntegrationEvent | Smart Insights |
| Attendance | StudentAbsentIntegrationEvent | Notifications |
| Finance | InvoiceCreatedIntegrationEvent | Notifications |
| Finance | PaymentReceivedIntegrationEvent | Notifications |

# Synchronous Module Contract Matrix

| Caller | Provider | Purpose |
|---|---|---|
| Enrollment | Academic | Validate academic year/grade/class and retrieve class snapshot/capacity |
| Attendance | Academic | Validate teacher assignment |
| Attendance | Enrollment | Retrieve active class roster |
| Finance | Enrollment | Resolve student reference only when immediate resolution is required |

# Primary End-to-End Scenario
Admin login
-> Academic setup
-> Teacher registration
-> Student/guardian registration
-> Enrollment
-> StudentEnrolledIntegrationEvent
-> Finance invoice if fee plan exists
-> Teacher sees roster
-> Teacher records absence
-> StudentAbsentIntegrationEvent
-> Parent notification
-> Accountant records payment
-> Parent dashboard updated
-> Smart Insights recalculates risk
