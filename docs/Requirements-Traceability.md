# RaceDay Requirements Traceability

## 1. Purpose

This table connects each confirmed requirement to the planned database area, API resource and Part 1 evidence. It will be updated when the ERD, endpoint plan and SQL Server script are completed. The requirements originate from the supplied Portfolio of Evidence brief and the business rules documented for RaceDay (The Independent Institute of Education, 2026).

## 2. Functional requirement traceability

| Requirement ID | Requirement summary | Role or access | Planned data area | Planned API area | Part 1 evidence |
|---|---|---|---|---|---|
| FR-AUTH-01 | Register a Participant account | Public | Users and Roles | Authentication | Endpoint plan and SQL constraints |
| FR-AUTH-02 | Sign in to an existing account | Public | Users and Roles | Authentication | Endpoint plan |
| FR-AUTH-03 | Prevent duplicate email addresses | System | Users | Authentication | Unique SQL constraint |
| FR-PROFILE-01 | View own profile | Authenticated user | Users | User Profile | Endpoint role and ownership rule |
| FR-PROFILE-02 | Update permitted own-profile fields | Authenticated user | Users | User Profile | Endpoint body and ownership rule |
| FR-EVENT-01 | Browse upcoming events | Public | Events | Events | Public GET endpoint |
| FR-EVENT-02 | View one event and its categories | Public | Events and Event Categories | Events and Categories | Public GET endpoint |
| FR-EVENT-03 | Create an event | Organiser | Events | Events | Organiser POST endpoint |
| FR-EVENT-04 | Update an owned event | Organiser | Events | Events | Organiser PUT endpoint and ownership rule |
| FR-EVENT-05 | Delete or cancel an owned event | Organiser | Events | Events | Organiser DELETE or status-update endpoint |
| FR-EVENT-06 | Store event location and route details | Organiser | Events or Route | Events | ERD attributes and endpoint fields |
| FR-CATEGORY-01 | Add an Event Category | Organiser | Event Categories | Categories | Organiser POST endpoint |
| FR-CATEGORY-02 | Update an Event Category | Organiser | Event Categories | Categories | Organiser PUT endpoint |
| FR-CATEGORY-03 | Remove or deactivate an Event Category safely | Organiser | Event Categories and Enrolments | Categories | Delete conflict response and database relationship |
| FR-CATEGORY-04 | View categories for an Event | Public | Event Categories | Categories | Public GET endpoint |
| FR-ENROL-01 | Enter an Event through a selected Category | Participant | Enrolments and Event Categories | Enrolments | Participant POST endpoint |
| FR-ENROL-02 | View own Enrolments | Participant | Enrolments | Enrolments | Participant GET endpoint and ownership rule |
| FR-ENROL-03 | View Enrolments for an owned Event | Organiser | Events and Enrolments | Enrolments | Organiser GET endpoint and ownership rule |
| FR-ENROL-04 | Reject duplicate active Event entry | System | Enrolments | Enrolments | Unique rule and conflict response |
| FR-ENROL-05 | Ensure the Category belongs to the selected Event | System | Events and Event Categories | Enrolments | Foreign-key design and validation response |
| FR-RESULT-01 | Capture a Result for a valid Enrolment | Organiser | Results and Enrolments | Results | Organiser POST endpoint |
| FR-RESULT-02 | Correct an existing Result | Organiser | Results | Results | Organiser PUT endpoint |
| FR-RESULT-03 | View personal Results | Participant | Results and Enrolments | Results | Participant GET endpoint and ownership rule |
| FR-RESULT-04 | Allow no more than one official Result per Enrolment | System | Results | Results | Unique SQL constraint and conflict response |

## 3. Role traceability

| Role requirement | Database support | Endpoint-plan support | Access rule |
|---|---|---|---|
| Organiser creates and manages Events | Event record refers to its Organiser | Organiser-only Event write endpoints | Organiser may alter only Events assigned to them |
| Organiser manages Event Categories | Category record refers to an Event | Organiser-only Category write endpoints | Event ownership must be checked |
| Organiser views Event Enrolments | Enrolment refers to Event and Participant | Organiser Event-enrolment endpoint | Event ownership must be checked |
| Organiser captures Results | Result refers to a valid Enrolment | Organiser-only Result write endpoints | Enrolment must belong to an owned Event |
| Participant creates an account | User record uses the Participant role | Public registration endpoint | Self-registration creates Participants only |
| Participant browses Events | Event and Category information supports browsing | Public Event read endpoints | No authentication required for public Event information |
| Participant enters an Event | Enrolment links Participant, Event and Category | Participant-only enrolment endpoint | Participant may enrol only themselves |
| Participant views personal Enrolments | Enrolment refers to its Participant | Participant enrolment-list endpoint | Query is restricted to the signed-in Participant |
| Participant tracks personal Results | Result is linked through Enrolment | Participant Result-history endpoint | Query is restricted to the signed-in Participant |

## 4. Submission requirement traceability

| ID | Submission requirement | Planned evidence | Status |
|---|---|---|---|
| SUB-01 | ERD with at least six entities, attributes, keys and cardinalities | `/docs/RaceDay-ERD.png` and `/docs/RaceDay-ERD-Notes.md` | Complete |
| SUB-02 | Complete six-column API endpoint plan | `/docs/API-Endpoint-Plan.md` | In progress: all named resources planned; Part 2 requirements still to review |
| SUB-03 | SQL Server schema and seed script | `/docs/RaceDay.sql` | Complete |
| SUB-04 | SQL script runs cleanly in SSMS | Demonstration and final test record | In progress: full script tested on clean LocalDB; SSMS walkthrough pending |
| SUB-05 | Required documents stored in `/docs` | Repository structure | Complete |
| SUB-06 | At least 20 meaningful commits | Git history | Complete |
| SUB-07 | Successful GitHub Actions validation | Workflow run and README screenshot | Complete |
| SUB-08 | README describes system and roles | `/README.md` | Complete; video link tracked separately under SUB-09 |
| SUB-09 | Unlisted video explains planning and SQL execution | README video link | Not started |
| SUB-10 | GitHub repository link submitted on ARC | ARC submission | Not started |

## 5. Open traceability items

The following items cannot be closed until the remaining referenced material is available:

1. Confirm whether the Part 2 functional-requirement pages introduce additional resources or endpoints.
2. Confirm whether enrolment cancellation is required or only recommended as a record-preservation rule.
3. Confirm whether event deletion must be physical deletion or status-based cancellation.

## 6. Closed Session 3 design items

| Design item | Decision | Evidence |
|---|---|---|
| Final entity list | Seven entities selected | `RaceDay-Data-Dictionary.md` |
| User roles | Separate Roles table | `RaceDay-Data-Dictionary.md` |
| Route storage | Separate EventRoutes table linked to EventCategories | `RaceDay-Data-Dictionary.md` |
| Weather storage | Live weather is not stored in the Part 1 database | Requirements and business rules |
| Location fields | Venue, address, city, province, latitude and longitude | Events data dictionary |
| Primary-key strategy | Fixed Role key and identity keys for main entities | Data dictionary conventions |
| Delete behaviour | `NO ACTION` for historical and transactional relationships | Data dictionary integrity plan |

## References

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.
