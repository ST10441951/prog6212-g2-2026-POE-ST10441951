# RaceDay ERD Design Notes

## 1. Purpose

The RaceDay ERD shows seven tables, 71 columns and nine relationships. Table names, column names, datatypes and NULL rules match `RaceDay.sql` and the data dictionary. Relationships use the UML multiplicities requested in the lecturer's feedback (The Independent Institute of Education, 2026a; 2026b).

The diagram files are:

- `RaceDay-ERD.drawio`, which is the editable draw.io source file.
- `RaceDay-ERD.png`, which is the submission image.
- `RaceDay-ERD.svg`, which is the scalable export.
- `RaceDay-Data-Dictionary.md`, which contains the detailed constraints and column descriptions.

The `.drawio` file is editable in draw.io. The SVG and PNG show the same model (JGraph Ltd, 2026).

## 2. Entity summary

| Entity | Main responsibility |
|---|---|
| `Roles` | Controls the Organiser and Participant role values |
| `Users` | Stores account and profile information |
| `Events` | Stores events managed by Organisers |
| `EventCategories` | Stores the entry choices offered by Events |
| `EventRoutes` | Stores route details for individual Event Categories |
| `Enrolments` | Records Participant entries and selected Categories |
| `Results` | Stores official outcomes for Enrolments |

## 3. Relationship and cardinality explanation

| Relationship | Parent cardinality | Child cardinality | Explanation |
|---|---:|---:|---|
| Roles to Users | 1 | 0..* | Every User has one Role, while one Role may be assigned to many Users |
| Users to Events | 1 | 0..* | Every Event has one Organiser, while one Organiser may manage many Events |
| Events to EventCategories | 1 | 0..* | Every Category belongs to one Event, while a draft Event may initially have no Categories |
| EventCategories to EventRoutes | 1 | 0..1 | A Category may have one Route record, while every Route belongs to one Category |
| Users to Enrolments | 1 | 0..* | Every Enrolment belongs to one Participant, while a Participant may have many Enrolments |
| Events to Enrolments | 1 | 0..* | Every Enrolment refers to one Event, while an Event may have many Enrolments |
| EventCategories to Enrolments | 1 | 0..* | Every Enrolment selects one Category, while a Category may be selected many times |
| Enrolments to Results | 1 | 0..1 | An Enrolment may receive one official Result, while every Result belongs to one Enrolment |
| Users to Results | 1 | 0..* | Every Result records one Organiser, while an Organiser may record many Results |

## 4. Many-to-many resolution

Participants and Events have a logical many-to-many relationship. A Participant may enter many Events, and an Event may receive many Participants. The `Enrolments` entity resolves this relationship and records the Event Category selected for each entry.

`(ParticipantUserId, EventId)` is unique. A Participant can have only one saved Enrolment for an Event, including a Cancelled entry. Reactivation updates that entry instead of adding another row.

## 5. Composite relationship rule

`Enrolments(EventCategoryId, EventId)` references the alternate key `EventCategories(EventCategoryId, EventId)`. Both columns are used together, in that order.

This rule prevents an Enrolment from selecting a Category that belongs to a different Event.

## 6. Key notation

| Notation | Meaning |
|---|---|
| `PK` | Primary key |
| `FK` | Foreign key |
| `UQ` | Single-column unique key |
| `FK UQ` | Column is both a foreign key and unique |
| `UQ1`, `UQ2` | Separate composite unique keys listed below the columns |
| `UQ*`, `UQ2*` | Filtered unique index; the full column combination and filter are listed below the columns |
| `NULL` | Optional column |
| `NOT NULL` | Required column |

Category names are unique within an Event through `(EventId, CategoryName)`. They do not have to be unique across all Events.

Assigned bib numbers are unique within an Event through `(EventId, BibNumber) WHERE BibNumber IS NOT NULL`. Multiple entries may have no bib number. A bib number may be reused at a different Event.

`FinishTimeSeconds` is nullable because non-completed Results have no finish time. A Completed Result must have a non-null value greater than zero. All nine foreign keys use `NO ACTION` on delete and update.

## 7. Part 2 requirements cross-check

The ERD was checked against the supplied Part 2 functional requirements. `Events.DistanceKm` records the advertised Event distance required by the brief. `EventCategories.MinimumAge` and `EventCategories.MaximumAge` allow an Organiser to define age categories such as Under 20 or Senior, while `EventCategories.DistanceKm` supports distance categories such as 10 km or 21 km.

## 8. Part 3 requirements cross-check

The ERD was checked against the supplied Part 3 requirements. `Users.ProfilePictureBlobName` stores the Azure Blob Storage object name for an optional Participant profile picture. `Events.BannerImageBlobName` stores the object name for an optional Event banner. The actual image data remains in Azure Blob Storage and is not stored in SQL Server.

## 9. ERD validation checklist

- [x] At least six entities are present.
- [x] Seven entities are included.
- [x] Every entity displays its attributes.
- [x] Every entity has a primary key.
- [x] Foreign keys are identified.
- [x] SQL Server datatypes are displayed.
- [x] Optional and required attributes are identified.
- [x] Every relationship displays cardinality.
- [x] Participant and Event many-to-many activity is resolved through Enrolments.
- [x] A Category cannot be linked to the wrong Event through an Enrolment.
- [x] An Enrolment may have no more than one official Result.
- [x] The entity and attribute names match the current data dictionary.
- [x] The editable draw.io source is available in the `/docs` folder.
- [x] The diagram is available as a PNG in the `/docs` folder.

The corrected schema was checked on a separate SQL Server LocalDB database on 17 October 2026. All 71 column definitions matched the ERD and data dictionary. See [Part 1 database corrections](Part-1-Database-Corrections.md) for the checks.

## References

JGraph Ltd (2026) *draw.io*. Available at: https://www.drawio.com/ (Accessed: 20 September 2026).

The Independent Institute of Education (2026a) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.

The Independent Institute of Education (2026b) *POE Part I General Feedback (HE)*. Lecturer-provided ERD and endpoint feedback.
