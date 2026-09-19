# RaceDay Requirements and Business Rules

## 1. Purpose

RaceDay is intended to support the planning and management of South African road running, walking and cycling events. The system must give organisers a structured way to manage events, categories, enrolments and results. Participants must be able to find events, enter a suitable category and review their own participation history.

This document records the requirements and business rules that will guide the Part 1 ERD, API endpoint plan and SQL Server database script. It is based on the supplied Portfolio of Evidence brief (The Independent Institute of Education, 2026).

## 2. Part 1 scope

Part 1 includes:

- A complete Entity Relationship Diagram.
- A planned list of REST API endpoints.
- A SQL Server database creation and seed script.
- Repository documentation and a GitHub Actions validation workflow.
- A video presentation explaining the planning decisions and demonstrating the SQL script in SSMS.

Part 1 does not include:

- C# API implementation.
- An MVC web application.
- Azure Blob Storage integration.
- Docker configuration.
- Online payment processing.
- Automatic timing hardware integration.

## 3. User roles

### 3.1 Organiser

An organiser is responsible for managing events. An organiser must be able to:

- Sign in to the system.
- Create an event.
- Update an event that they manage.
- Delete or cancel an event that they manage.
- Create, update and remove categories for their events.
- View all enrolments for an event that they manage.
- Capture and update participant results for their events.

### 3.2 Participant

A participant uses RaceDay to find and enter events. A participant must be able to:

- Register an account.
- Sign in to the system.
- View and update their own profile.
- Browse upcoming events.
- View the details and categories of an event.
- Enter an event by selecting one of its categories.
- View their own enrolments.
- View their own results and performance history.

## 4. Functional requirements

### 4.1 Authentication and profile requirements

| ID | Requirement |
|---|---|
| FR-AUTH-01 | The system must allow a Participant to register an account. |
| FR-AUTH-02 | The system must allow an existing Organiser or Participant to sign in. |
| FR-AUTH-03 | The system must prevent more than one account from using the same email address. |
| FR-PROFILE-01 | An authenticated user must be able to view their own profile. |
| FR-PROFILE-02 | An authenticated user must be able to update permitted fields on their own profile. |

### 4.2 Event requirements

| ID | Requirement |
|---|---|
| FR-EVENT-01 | Any visitor must be able to browse upcoming events. |
| FR-EVENT-02 | Any visitor must be able to view the details and categories of one event. |
| FR-EVENT-03 | An Organiser must be able to create an event. |
| FR-EVENT-04 | An Organiser must be able to update an event that they manage. |
| FR-EVENT-05 | An Organiser must be able to delete or cancel an event that they manage. |
| FR-EVENT-06 | An event must store enough location and route information to support race-day preparation. |

### 4.3 Category requirements

| ID | Requirement |
|---|---|
| FR-CATEGORY-01 | An Organiser must be able to add categories to an event that they manage. |
| FR-CATEGORY-02 | An Organiser must be able to update categories for an event that they manage. |
| FR-CATEGORY-03 | An Organiser must be able to remove a category when doing so does not invalidate existing enrolment records. |
| FR-CATEGORY-04 | A Participant must be able to view the categories available for an event. |

### 4.4 Enrolment requirements

| ID | Requirement |
|---|---|
| FR-ENROL-01 | An authenticated Participant must be able to enter an event by selecting an available category. |
| FR-ENROL-02 | A Participant must be able to view their own enrolments. |
| FR-ENROL-03 | An Organiser must be able to view enrolments for an event that they manage. |
| FR-ENROL-04 | The system must reject a duplicate active enrolment for the same Participant and Event. |
| FR-ENROL-05 | The selected category must belong to the selected event. |

### 4.5 Result requirements

| ID | Requirement |
|---|---|
| FR-RESULT-01 | An Organiser must be able to capture a result for a valid enrolment in an event that they manage. |
| FR-RESULT-02 | An Organiser must be able to correct a result for an event that they manage. |
| FR-RESULT-03 | A Participant must be able to view their own results. |
| FR-RESULT-04 | One enrolment may have no more than one official result. |

## 5. Business rules

### 5.1 User and role rules

1. Every user account must have exactly one role.
2. The permitted roles are Organiser and Participant.
3. Every email address must be unique and stored in a consistent form.
4. Required personal information must not be blank.
5. Passwords must never be stored as plain text. The future API will store a secure password hash.
6. A Participant may only access their own profile, enrolments and personal results.
7. An Organiser may only manage events that are assigned to them.

### 5.2 Event rules

1. Every event must have one Organiser.
2. An Organiser may manage many events.
3. An event date and time must be later than its entry closing date and time.
4. An event must have a name, date, location and route description.
5. An event status will be limited to Draft, Open, Closed, Cancelled or Completed.
6. Only events with an Open status may accept new enrolments.
7. A cancelled event must remain in the database if enrolments or results already refer to it.
8. Location details should support later weather lookup. Latitude and longitude may therefore be recorded for each event.

### 5.3 Category rules

1. Every category must belong to exactly one event.
2. An event must have at least one category before entries can open.
3. Category names must be unique within the same event.
4. Category distance must be greater than zero.
5. An entry fee must be zero or greater.
6. A category with existing enrolments should be made unavailable instead of being physically deleted.

### 5.4 Enrolment rules

1. Every enrolment must identify one Participant, one Event and one Event Category.
2. The selected Event Category must belong to the selected Event.
3. A Participant may have only one active enrolment per Event.
4. A Participant cannot enrol after entries have closed.
5. A Participant cannot enrol in an Event with a Draft, Closed, Cancelled or Completed status.
6. An enrolment status will be limited to Confirmed or Cancelled unless later requirements justify another value.
7. Cancelling an enrolment must preserve the original record for history and reporting.

### 5.5 Result rules

1. A result must refer to an existing enrolment.
2. An enrolment may have zero or one official result.
3. Only an Organiser responsible for the Event may capture or update its results.
4. A recorded finish time must be greater than zero.
5. Overall and category positions must be positive when supplied.
6. A result cannot be recorded for a cancelled enrolment.
7. A result should record when it was captured or last updated.

## 6. Data integrity requirements

- Primary keys must uniquely identify every record.
- Foreign keys must prevent orphaned records.
- Required fields must use `NOT NULL` constraints.
- Email addresses and other natural identifiers that must be unique must use `UNIQUE` constraints.
- Status, distance, fee, time and position values must use suitable validation constraints.
- Default values must be used where they provide a predictable initial state.
- Historical enrolment and result records should not be removed accidentally through cascading deletes.
- The ERD and SQL Server script must use matching entity, attribute and relationship definitions.

## 7. Security and access requirements

- Public access must be limited to information intended for event browsing.
- Authentication will be required for personal profiles, enrolments and results.
- Role checks must distinguish Organiser actions from Participant actions.
- Ownership checks must prevent one Organiser from altering another Organiser's events.
- Participants must not be able to view another Participant's private records.
- Request data must be validated before it is accepted by the future API.

## 8. Confirmed Part 1 design decisions

The following decisions will guide the ERD and SQL script. They must still be checked against any remaining functional-requirement pages:

1. A user account has one role only.
2. Participants can register themselves, while Organiser accounts are supplied or created through a controlled process.
3. A Participant may enter only one category per event.
4. Payment processing is outside the current system scope.
5. Event weather is obtained live in Part 3 rather than stored permanently in the Part 1 database.
6. Event location coordinates will be stored so that live weather can be requested later.
7. Route information will be stored in a separate EventRoutes entity for each Event Category.
8. Records referenced by enrolments or results will normally be made inactive instead of being deleted.

## 9. Session 3 data-model decisions

- The final model contains Roles, Users, Events, EventCategories, EventRoutes, Enrolments and Results.
- Roles use a separate lookup table so that permitted role values are controlled centrally.
- Each Event Category may have one EventRoutes record.
- Role identifiers use fixed `TINYINT` values. Main entity identifiers use `INT IDENTITY(1,1)` values.
- Historical and transactional foreign keys use `NO ACTION` delete behaviour.
- Event location fields include a venue, address, city, province, latitude and longitude.
- Category route fields include the start, finish, route description, map link and optional elevation gain.
- The exact columns, datatypes and constraints are defined in `RaceDay-Data-Dictionary.md`.

## References

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.
