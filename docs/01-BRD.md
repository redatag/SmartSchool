# SmartSchool MVP — Business Requirements Document v1.0

## 1. Project
SmartSchool is a web-based smart schooling platform built for schools in Egypt, Saudi Arabia, and later international-school environments.

The MVP backend will use ASP.NET Core with a Modular Monolith architecture and Domain-Driven Design.

## 2. Business Problem
Schools commonly rely on disconnected systems, spreadsheets, paper processes, manual attendance, ad-hoc payment tracking, and WhatsApp-based communication.

Typical problems:
- Duplicate student information
- Weak integration between academic and finance processes
- Manual attendance
- Poor parent visibility
- Delayed fee follow-up
- Fragmented notifications
- Limited dashboards
- Hard-coded education structures that do not adapt well across markets

## 3. Product Vision
Create a flexible, scalable, modular, and intelligent school platform that manages the student lifecycle from registration through enrollment, attendance, finance, notifications, and basic student-risk monitoring.

## 4. Business Objectives
1. Centralize student and guardian data.
2. Configure academic years, terms, grades, classes, and subjects.
3. Support student registration and enrollment.
4. Support teacher assignments.
5. Record attendance.
6. Manage basic school fees, invoices, and payments.
7. Give parents visibility into children, attendance, invoices, payments, and notifications.
8. Send reliable notifications.
9. Provide role-based dashboards.
10. Introduce explainable student-risk monitoring.
11. Keep the architecture suitable for future multi-school expansion.
12. Avoid hard-coded Egyptian/Saudi/international academic structures.

## 5. MVP User Journey
School configuration
-> Academic year
-> Grades/classes
-> Subjects
-> Teachers
-> Students/guardians
-> Enrollment
-> Teacher assignments
-> Attendance
-> Fees/invoices
-> Payments
-> Notifications
-> Dashboards
-> Student risk profile

## 6. Actors
- Super Administrator
- School Administrator
- Teacher
- Student
- Parent / Guardian
- Accountant
- Academic Supervisor
- System Background Worker

## 7. Functional Requirements

### Identity & Access
- FR-001 Login
- FR-002 Role-Based Access
- FR-003 User Management

### Academic
- FR-010 Academic Year Management
- FR-011 Term Management
- FR-012 Grade Management
- FR-013 Class Management
- FR-014 Subject Management
- FR-015 Teacher Management
- FR-016 Teacher Assignment

### Student & Guardian
- FR-020 Student Registration
- FR-021 Student Number Generation
- FR-022 Guardian Registration
- FR-023 Multiple Guardians per Student
- FR-024 Guardian Can Have Multiple Children

### Enrollment
- FR-030 Student Enrollment
- FR-031 Enrollment Validation
- FR-032 Class Capacity Validation
- FR-033 Enrollment Status Management

Enrollment statuses:
- Pending
- Active
- Suspended
- Withdrawn
- Completed

### Attendance
- FR-040 Class Attendance
- FR-041 Attendance Status
- FR-042 Attendance Validation
- FR-043 Absence Notification

Attendance statuses:
- Present
- Absent
- Late
- Excused

### Finance
- FR-050 Fee Type Management
- FR-051 Student Invoice
- FR-052 Payment Recording
- FR-053 Partial Payment
- FR-054 Invoice Status Management

Invoice statuses:
- Draft
- Issued
- Partially Paid
- Paid
- Overdue
- Cancelled

### Notifications
- FR-060 In-App Notification
- FR-061 Email Notification
- FR-062 Event-Driven Notifications

Initial notification triggers:
- Enrollment
- Absence
- Invoice creation
- Payment receipt
- Overdue invoice

### Smart Insights
- FR-070 Student Risk Score
- FR-071 Risk Categories
- FR-072 Explainability

Risk levels:
- Low
- Medium
- High

### Dashboards
Admin dashboard:
- Total students
- Active students
- Teachers
- Classes
- Today's attendance
- Outstanding payments

Teacher dashboard:
- Assigned classes
- Today's classes
- Attendance tasks

Parent dashboard:
- Linked children
- Attendance summary
- Outstanding invoices
- Notifications

Accountant dashboard:
- Total invoices
- Paid
- Outstanding
- Overdue

## 8. Core Business Rules
- BR-001 Student number must be unique within a school.
- BR-002 A user must have at least one role before accessing protected functionality.
- BR-003 Academic year start date must be before end date.
- BR-004 A class belongs to a grade.
- BR-005 A class belongs to an academic year.
- BR-006 A student cannot have more than one active enrollment in the same academic year unless a future transfer workflow explicitly allows it.
- BR-007 Enrollment cannot exceed class capacity unless an authorized override is introduced later.
- BR-008 Only assigned teachers can record attendance unless an authorized administrator override is introduced.
- BR-009 Only one attendance record may exist for a student in the same attendance session.
- BR-010 Payment amount must be greater than zero.
- BR-011 Payment cannot exceed outstanding invoice balance unless overpayment support is explicitly implemented.
- BR-012 Finalized financial transactions must not be physically deleted.
- BR-013 A guardian may be linked to multiple students.
- BR-014 A student may have multiple guardians.
- BR-015 One guardian should normally be marked as primary for important communication.

## 9. Non-Functional Requirements
- NFR-001 Security
- NFR-002 Permission-based authorization
- NFR-003 Auditability
- NFR-004 Good performance for normal school workloads
- NFR-005 Scalability
- NFR-006 Maintainability
- NFR-007 Strong module isolation
- NFR-008 Reliable asynchronous integration using Outbox/Inbox
- NFR-009 Structured logging
- NFR-010 Centralized error handling
- NFR-011 Input and domain validation
- NFR-012 Redis caching for selected reference/read data

## 10. Multi-Tenancy Direction
The MVP may initially run for one school, but business tables should include SchoolId where relevant.

Future direction:
- Multi-school SaaS
- Tenant-aware authorization
- Tenant-aware configuration

## 11. Education System Configuration
Do not hard-code grade names based on one country.

Support configurable education system values such as:
- Egyptian
- Saudi
- British
- American
- IB
- Custom

## 12. MVP Scope
Included:
- Identity/authentication/authorization
- Academic configuration
- Student and guardian management
- Enrollment
- Teacher management and assignment
- Attendance
- Basic finance
- Notifications
- Dashboards
- Basic explainable risk scoring

## 13. Out of Scope for MVP
- Transportation
- Buses
- Library
- HR
- Payroll
- Staff attendance
- LMS/video learning
- Homework
- Online exams
- Advanced grading
- Advanced timetable optimization
- Full accounting
- Inventory/procurement
- Clinic
- Advanced AI chatbot
- Mobile apps
- Payment gateway

## 14. Future Modules
Possible future modules:
- Transportation
- Library
- Examination
- Grading
- Homework
- LMS
- HR
- Payroll
- Clinic
- Inventory
- Activities
- Private Tutoring Marketplace
- Educational Center Search
- AI Assistant
- Parent/Teacher mobile apps

## 15. Architecture Constraints
Each module owns:
- Business rules
- Persistence
- DbContext
- Schema
- Contracts
- Migrations

Modules must not query another module's tables directly.

Integration options:
- Module contracts
- Commands/queries inside a module
- Domain events
- Integration events
- Outbox/Inbox

## 16. Initial Context Map
Identity provides user/permission context to the platform.

Academic owns:
- Academic years
- Terms
- Grades
- Classes
- Subjects
- Teachers
- Teacher assignments

Enrollment depends on Academic reference data and owns:
- Students
- Guardians
- Enrollments

Attendance depends on:
- Academic teacher/class authorization
- Enrollment class roster

Finance reacts to enrollment and owns:
- Fee types
- Fee plans
- Invoices
- Payments

Notifications consumes integration events.

Smart Insights consumes attendance and future academic/behavioral events.

## 17. Initial Aggregate Candidates
Identity:
- User
- Role

Academic:
- AcademicYear
- Grade
- Class
- Subject
- Teacher
- TeacherAssignment

Enrollment:
- Student
- Guardian
- Enrollment

Attendance:
- AttendanceSession

Finance:
- FeeType
- FeePlan
- Invoice

Notifications:
- Notification

Smart Insights:
- StudentRiskProfile

## 18. MVP End-to-End Definition of Done
1. Admin logs in.
2. Admin creates academic year.
3. Admin creates grade and class.
4. Admin registers teacher.
5. Admin registers student and guardian.
6. Admin enrolls student.
7. Enrollment publishes StudentEnrolledIntegrationEvent.
8. Finance creates an initial invoice if a fee plan exists.
9. Teacher sees enrolled student.
10. Teacher records absence.
11. Attendance publishes an absence/attendance event.
12. Notification sends parent notification.
13. Accountant records payment.
14. Parent dashboard reflects payment and attendance.
15. Smart Insights recalculates student risk.

This flow should become a primary end-to-end integration test.
