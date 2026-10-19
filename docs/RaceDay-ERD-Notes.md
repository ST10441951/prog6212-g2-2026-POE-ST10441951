# RaceDay ERD Design Notes

## 1. Model

The ERD contains seven tables, 71 columns and nine relationships. Names, types and NULL rules match SQL and the data dictionary. UML multiplicities follow the lecturer's feedback (The Independent Institute of Education, 2026a; 2026b).

| File | Use |
|---|---|
| `RaceDay-ERD.drawio` | Editable diagram |
| `RaceDay-ERD.png` | Submission image |
| `RaceDay-ERD.svg` | Scalable export |
| `RaceDay-Data-Dictionary.md` | Columns and constraints |

The exports use the same draw.io model (JGraph Ltd, 2026).

## 2. Relationships

| Parent | Child | Rule |
|---|---|---|
| Roles | Users | One Role to zero/many Users; each User has one Role |
| Users | Events | One Organiser to zero/many Events; each Event has one owner |
| Events | EventCategories | One Event to zero/many Categories; Draft may have none |
| EventCategories | EventRoutes | One Category to zero/one Route |
| Users | Enrolments | One Participant to zero/many entries |
| Events | Enrolments | One Event to zero/many entries |
| EventCategories | Enrolments | One Category to zero/many entries |
| Enrolments | Results | One entry to zero/one official Result |
| Users | Results | One Organiser to zero/many recorded Results |

## 3. Keys and integrity

Enrolments resolves Participants and Events. `(ParticipantUserId, EventId)` is unique, including Cancelled history; reactivation updates that row.

`Enrolments(EventCategoryId, EventId)` references the matching composite key in EventCategories, preventing selection of another event's category.

| Label | Meaning |
|---|---|
| PK / FK | Primary / foreign key |
| UQ / FK UQ | Unique key / unique foreign key |
| UQ1, UQ2 | Separate composite unique keys |
| UQ*, UQ2* | Filtered unique index; full rule shown below columns |
| NULL / NOT NULL | Optional / required |

Category names are unique within an event. Assigned bibs are unique per event; NULL bibs and reuse at other events are allowed. Completed results need positive, non-null seconds; other outcomes have no finish time. All foreign keys use NO ACTION.

## 4. Requirement checks

Event distance and category age/distance fields cover Part 2. Optional User/Event blob-name columns support images; binaries stay outside SQL. Part 3 MVC uses the API for uploads.

The [17 October corrections](Part-1-Database-Corrections.md) verified all 71 columns against an isolated SQL database. Keys, nullability, nine relationships and matching PNG/SVG/draw.io exports are included.

## References

JGraph Ltd (2026) *draw.io*. Available at: https://www.drawio.com/ (Accessed: 20 September 2026).

The Independent Institute of Education (2026a) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.

The Independent Institute of Education (2026b) *POE Part I General Feedback (HE)*. Lecturer-provided ERD and endpoint feedback.
