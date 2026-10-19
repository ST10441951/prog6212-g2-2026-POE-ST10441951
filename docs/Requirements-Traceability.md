# RaceDay Requirements Traceability

## 1. Purpose

This links the brief and lecturer's feedback to the database and endpoint plan (The Independent Institute of Education, 2026; 2026b). All 40 business endpoints are implemented; the [mapping](API-Endpoint-Plan.md#141-required-operations) covers all 25 required operations.

The [rubric review](Part-2-Rubric-Review.md) records the passing 810-test GitHub and local runs, screenshots and outstanding submission tasks. [Feature notes](../README.md#project-files) contain detailed rules and screenshots.

## 2. Functional requirement traceability

| Requirement ID | Requirement summary | Role or access | Planned data area | Planned API area | Part 1 evidence |
|---|---|---|---|---|---|
| FR-AUTH-01 | Register an Organiser or Participant account | Public | Users and Roles | Authentication | Endpoint role field and SQL role constraint |
| FR-AUTH-02 | Sign in to an existing account | Public | Users and Roles | Authentication | Endpoint plan |
| FR-AUTH-03 | Prevent duplicate email addresses | System | Users | Authentication | Unique SQL constraint |
| FR-AUTH-04 | Maintain identity and role in a session | Authenticated user | Users and Roles | Authentication | Login, logout and session rules |
| FR-AUTH-05 | Reject unauthenticated or incorrect-role access | Protected by role | Users and Roles | All protected resources | Endpoint roles and failure responses |
| FR-PROFILE-01 | View own profile | Authenticated user | Users | User Profile | Endpoint role and ownership rule |
| FR-PROFILE-02 | Update permitted own-profile fields | Authenticated user | Users | User Profile | Endpoint body and ownership rule |
| FR-PROFILE-03 | Upload or remove own profile picture | Participant | Users | Profile picture | Blob-name column and upload endpoints |
| FR-EVENT-01 | Browse upcoming events | Public | Events | Events | Public GET endpoint |
| FR-EVENT-02 | View one event and its categories | Public | Events and Event Categories | Events and Categories | Public GET endpoint |
| FR-EVENT-03 | Create an event | Organiser | Events | Events | Organiser POST endpoint |
| FR-EVENT-04 | Update an owned event | Organiser | Events | Events | Organiser PUT endpoint and ownership rule |
| FR-EVENT-05 | Delete or cancel an owned event | Organiser | Events | Events | Organiser DELETE or status-update endpoint |
| FR-EVENT-06 | Store event name, description, date, location, distance and type | Organiser | Events | Events | ERD attributes, SQL columns and endpoint fields |
| FR-EVENT-07 | Upload or remove an Event banner | Organiser | Events | Event banner | Blob-name column and upload endpoints |
| FR-EVENT-08 | View dashboard event and enrolment summaries | Organiser | Events and Enrolments | Organiser dashboard | Summary endpoint |
| FR-CATEGORY-01 | Add an Event Category | Organiser | Event Categories | Categories | Organiser POST endpoint |
| FR-CATEGORY-02 | Update an Event Category | Organiser | Event Categories | Categories | Organiser PUT endpoint |
| FR-CATEGORY-03 | Remove or deactivate an Event Category safely | Organiser | Event Categories and Enrolments | Categories | Delete conflict response and database relationship |
| FR-CATEGORY-04 | View categories for an Event | Public | Event Categories | Categories | Public GET endpoint |
| FR-CATEGORY-05 | Define age or distance Categories | Organiser | Event Categories | Categories | Distance, minimum-age and maximum-age fields |
| FR-ENROL-01 | Enter an Event through a selected Category | Participant | Enrolments and Event Categories | Enrolments | Participant POST endpoint |
| FR-ENROL-02 | View own Enrolments | Participant | Enrolments | Enrolments | Participant GET endpoint and ownership rule |
| FR-ENROL-03 | View Enrolments for an owned Event | Organiser | Events and Enrolments | Enrolments | Organiser GET endpoint and ownership rule |
| FR-ENROL-04 | Reject a second saved Event entry | System | Enrolments | Enrolments | Unique rule, 409 response and reactivation of Cancelled rows |
| FR-ENROL-05 | Ensure the Category belongs to the selected Event | System | Events and Event Categories | Enrolments | Foreign-key design and validation response |
| FR-ENROL-06 | Cancel own Confirmed entry and keep its history | Participant owner | Enrolments | PUT enrolment cancellation | Open-event, closing-time and result checks |
| FR-ENROL-07 | Reactivate own Cancelled entry | Participant owner | Enrolments and Event Categories | PUT enrolment reactivation | Same row ID; repeated category, age and capacity checks |
| FR-ENROL-08 | Delete own eligible Confirmed entry | Participant owner | Enrolments and Results | DELETE enrolment | Open-event, closing-time and result checks; Cancelled history retained |
| FR-ENROL-09 | Check both age limits and required date of birth | Participant | Users, Events and Event Categories | Enrolment creation and reactivation | Age on the event date; inclusive limits; 400 for invalid or missing age data |
| FR-RESULT-01 | Capture a Result for a valid Enrolment | Organiser | Results and Enrolments | Results | Organiser POST endpoint |
| FR-RESULT-02 | Correct an existing Result | Organiser | Results | Results | Organiser PUT endpoint |
| FR-RESULT-03 | View personal Results | Participant | Results and Enrolments | Results | Participant GET endpoint and ownership rule |
| FR-RESULT-04 | Allow no more than one official Result per Enrolment | System | Results | Results | Unique SQL constraint and conflict response |

## 4. Part 1 submission requirement traceability

| ID | Submission requirement | Planned evidence | Status |
|---|---|---|---|
| SUB-01 | ERD with at least six entities, attributes, keys and cardinalities | `/docs/RaceDay-ERD.drawio`, `/docs/RaceDay-ERD.png` and `/docs/RaceDay-ERD-Notes.md` | Corrected 17 October: seven tables, 71 matching column definitions and nine relationships |
| SUB-02 | Complete six-column API endpoint plan | `/docs/API-Endpoint-Plan.md` | Updated 17 October: 40 endpoints and a mapping of the 25 required operations |
| SUB-03 | SQL Server schema and seed script | `/docs/RaceDay.sql` and [database correction notes](Part-1-Database-Corrections.md) | Corrected 17 October: finish-time constraint, database guard and success reporting; 21 local checks passed |
| SUB-04 | SQL script runs cleanly in SSMS | Original screenshot plus [fresh counts](evidence/ssms-corrected-database-counts.png) and [success message](evidence/ssms-corrected-database-success.png) | Corrected script ran successfully on a separate empty LocalDB database in SSMS on 17 October; original evidence retained |
| SUB-05 | Required documents stored in `/docs` | Repository structure | Complete |
| SUB-06 | At least 20 meaningful commits | Git history | Complete |
| SUB-07 | Successful GitHub Actions validation | Original screenshot and [C# run for 4560c17](https://github.com/ST10441951/prog6212-g2-2026-POE-ST10441951/actions/runs/39063690485) | C# planning checker passed locally and on GitHub |
| SUB-08 | README describes system and roles | `/README-Part1.md` | Original Part 1 README preserved; root README now covers Part 2 |
| SUB-09 | Unlisted video explains planning and SQL execution | [Part 1 README](../README-Part1.md#video-presentation) | Original walkthrough link present |
| SUB-10 | GitHub repository link submitted on ARC | Part 1 requirements checklist | Recorded as submitted in the original checklist |

## 5. Part 2 and Part 3

Part 2 implements authentication, sessions/roles, profiles, events, categories, routes, enrolments, results and image endpoints. Swagger and tests check the plan. See the [Part 2 checklist](Part-2-Requirements-Checklist.md).

Part 3 adds MVC, role-based navigation, live weather, image-upload pages and Docker. MVC uses the API for data and uploads; it has no direct database or storage access. Its workflow will build MVC, API and tests.

The [data dictionary](RaceDay-Data-Dictionary.md) and [ERD notes](RaceDay-ERD-Notes.md) record keys, relationships, locations, route storage and NO ACTION deletion rules.

## References

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.

The Independent Institute of Education (2026b) *POE Part I General Feedback (HE)*. Lecturer-provided ERD and endpoint feedback.
