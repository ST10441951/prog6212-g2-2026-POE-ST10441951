# RaceDay ERD Design Notes

## 1. Purpose

The RaceDay Entity Relationship Diagram represents the Part 1 relational database design. It contains seven entities, their attributes, SQL Server datatypes, primary keys, foreign keys and relationship cardinalities. The diagram follows the requirements in the supplied Portfolio of Evidence brief (The Independent Institute of Education, 2026).

The diagram files are:

- `RaceDay-ERD.drawio`, which is the editable draw.io source file.
- `RaceDay-ERD.png`, which is the submission image.
- `RaceDay-ERD.svg`, which is the scalable export.
- `RaceDay-Data-Dictionary.md`, which contains the detailed constraints and column descriptions.

The diagram was created and exported with draw.io (JGraph Ltd, 2026).

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

The combination of `ParticipantUserId` and `EventId` will be unique. This prevents more than one Enrolment record for the same Participant and Event.

## 5. Composite relationship rule

`Enrolments` stores both `EventId` and `EventCategoryId`. These columns form a composite foreign key to the alternate key on `EventCategories.EventCategoryId` and `EventCategories.EventId`.

This rule prevents an Enrolment from selecting a Category that belongs to a different Event.

## 6. Key notation

| Notation | Meaning |
|---|---|
| `PK` | Primary key |
| `FK` | Foreign key |
| `UQ` | Unique value or unique combination |
| `FK UQ` | Column is both a foreign key and unique |
| `UQ*` | Uniqueness will be implemented with a filtered unique index |
| `NULL` | Optional column |
| `NOT NULL` | Required column |

## 7. ERD validation checklist

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

## References

JGraph Ltd (2026) *draw.io*. Available at: https://www.drawio.com/ (Accessed: 20 September 2026).

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.
