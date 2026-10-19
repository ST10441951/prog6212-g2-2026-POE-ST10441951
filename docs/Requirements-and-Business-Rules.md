# RaceDay Requirements and Business Rules

## 1. Purpose

RaceDay manages South African running, walking and cycling events. These rules support the ERD, SQL and API plan (The Independent Institute of Education, 2026). The [endpoint mapping](API-Endpoint-Plan.md#141-required-operations) covers the lecturer's 25 required operations (The Independent Institute of Education, 2026b).

## 2. Scope

| Part | Work |
|---|---|
| 1 | ERD, endpoint plan, SQL/seed script, validation workflow and planning video |
| 2 | C# API, SQL Server/EF Core, sessions/roles, all 40 endpoints, Swagger, tests and CI |
| 3 | MVC consuming the API, live weather, image-upload pages and Docker |

Payments and timing-hardware integration are outside scope.

## 3. Roles

Organisers manage their own events, categories, routes, banners, rosters, bibs and results. Participants browse, enter categories and manage their own profile, picture, entries and results. Both roles register and log in; private records require role and ownership checks.

## 4. Functional requirements

### 4.1 Authentication and profile requirements

| ID | Requirement |
|---|---|
| FR-AUTH-01 | Register as Organiser or Participant. |
| FR-AUTH-02 | Log in with either role. |
| FR-AUTH-03 | Reject duplicate email addresses. |
| FR-AUTH-04 | A successful login must establish a session that maintains the user's identity and role for later requests. |
| FR-AUTH-05 | Protected functionality must reject unauthenticated users and users with the wrong role. |
| FR-PROFILE-01 | Signed-in users can view their own profile. |
| FR-PROFILE-02 | Signed-in users can update permitted fields on their own profile. |
| FR-PROFILE-03 | Participants can upload, replace and remove their own profile picture through the API. |

### 4.2 Event requirements

| ID | Requirement |
|---|---|
| FR-EVENT-01 | Visitors can browse upcoming events. |
| FR-EVENT-02 | Visitors can view the details and categories of one event. |
| FR-EVENT-03 | Organisers can create an event. |
| FR-EVENT-04 | Organisers can update an event that they manage. |
| FR-EVENT-05 | Organisers can delete or cancel an event that they manage. |
| FR-EVENT-06 | An event must store its name, description, date and time, location, distance and event type. |
| FR-EVENT-07 | Organisers can upload, replace and remove a banner image for an event that they manage. |
| FR-EVENT-08 | Organisers can view a dashboard summary of their events, enrolment totals and upcoming dates. |

### 4.3 Category requirements

| ID | Requirement |
|---|---|
| FR-CATEGORY-01 | Organisers can add categories to an event that they manage. |
| FR-CATEGORY-02 | Organisers can update categories for an event that they manage. |
| FR-CATEGORY-03 | Organisers can remove a category when doing so does not invalidate existing enrolment records. |
| FR-CATEGORY-04 | Participants can view the categories available for an event. |
| FR-CATEGORY-05 | A category must be able to describe an age grouping, a distance grouping or both. |

### 4.4 Enrolment requirements

| ID | Requirement |
|---|---|
| FR-ENROL-01 | Signed-in Participants can enter an event by selecting an available category. |
| FR-ENROL-02 | Participants can view their own enrolments. |
| FR-ENROL-03 | Organisers can view enrolments for an event that they manage. |
| FR-ENROL-04 | The system must reject a second saved enrolment for the same Participant and Event, including when the existing entry is Cancelled. |
| FR-ENROL-05 | The selected category must belong to the selected event. |
| FR-ENROL-06 | A Participant may cancel their own Confirmed entry while the event is Open and entries have not closed, provided no result exists. The row is kept. |
| FR-ENROL-07 | A Participant may reactivate their own Cancelled entry after the event, category, age and capacity checks pass again. |
| FR-ENROL-08 | A Participant may delete their own Confirmed entry while the event is Open and entries have not closed, provided no result exists. Cancelled and historical entries are retained. |
| FR-ENROL-09 | New entries and reactivations must meet both category age limits. A date of birth is required for an age-restricted category. |

### 4.5 Result requirements

| ID | Requirement |
|---|---|
| FR-RESULT-01 | Organisers can capture a result for a valid enrolment in an event that they manage. |
| FR-RESULT-02 | Organisers can correct a result for an event that they manage. |
| FR-RESULT-03 | Participants can view their own results. |
| FR-RESULT-04 | One enrolment may have no more than one official result. |

## 5. Business rules

| Area | Rules |
|---|---|
| Accounts | One role per account: Organiser or Participant. Unique normalised email, nonblank required fields and hashed passwords. Identity/role comes from the server-side session. |
| Events | One Organiser; required name, description, dates, location, distance and type. Closing precedes start. Status is Draft, Open, Closed, Cancelled or Completed. Only Open events accept entries before closing. |
| Categories | Belong to one event; names unique within it. Positive distance, non-negative fee, optional positive capacity and inclusive ages 1-120. Minimum cannot exceed maximum. At least one available category is needed before opening. |
| Routes | At most one route per category, with start, finish, instructions and optional map/elevation. |
| Enrolments | One saved entry per Participant/event. Category must belong to the event. Check status, closing time, availability, capacity and age on the UTC event date. Missing required birth dates or ineligible ages return 400. |
| Cancellation | Own Confirmed entry, Open event, entries not closed and no result. Keep the row and bib. |
| Reactivation | Recheck entry rules on the same Cancelled row. Changing category clears the bib. Cancelled entries do not use capacity. |
| Entry DELETE | Same eligibility as cancellation, but removes only the Confirmed entry. Cancelled and historical rows stay. |
| Results | One official result per Confirmed entry. Only the event owner captures/corrects it after the event starts. Completed requires positive seconds; other statuses have no finish time or positions. Supplied finishing positions are positive. |
| History | Preserve referenced records through NO ACTION foreign keys. Cancel events or make categories unavailable when deletion would remove history. |
| Images | SQL stores optional generated blob names. Files stay in Blob Storage; only the API accesses storage and generates image URLs. |

## 6. Data and access

- Keep table/column names, datatypes, keys and relationships consistent across ERD, dictionary and SQL.
- Use primary/foreign keys, NOT NULL, UNIQUE, DEFAULT and CHECK constraints.
- API validation checks roles, ownership and cross-record business rules; foreign keys alone cannot verify roles.
- Validate input and set UpdatedAt on changes. Never expose another user's private profile, entries or results.
- Keep real credentials out of Git. MVC uses the API rather than accessing SQL/storage directly.
- Live weather is obtained in Part 3, using saved event locations; it is not stored in the Part 1 schema.

The seven tables are Roles, Users, Events, EventCategories, EventRoutes, Enrolments and Results. See the [data dictionary](RaceDay-Data-Dictionary.md) for exact fields and constraints and the [endpoint plan](API-Endpoint-Plan.md) for implemented API rules.

## References

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.

The Independent Institute of Education (2026b) *POE Part I General Feedback (HE)*. Lecturer-provided ERD and endpoint feedback.
