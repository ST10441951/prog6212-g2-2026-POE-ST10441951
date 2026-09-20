# RaceDay Data Dictionary

## 1. Purpose

This data dictionary defines the proposed SQL Server structure for RaceDay. The same table names, columns, keys and relationships must be used in the ERD and SQL script. Any later change must be applied consistently to all three artefacts.

The datatype choices use the SQL Server datatype definitions documented by Microsoft (2026a). Variable-length Unicode text uses `NVARCHAR` so that names and locations can support a broad range of characters (Microsoft, 2026b). Primary and foreign keys are used to enforce entity and referential integrity (Microsoft, 2026c).

## 2. Naming and datatype conventions

| Convention | Decision |
|---|---|
| Table names | Plural PascalCase names, for example `Users` and `Events` |
| Primary keys | Singular entity name followed by `Id` |
| Foreign keys | The referenced primary-key name is reused |
| Main identifiers | `INT IDENTITY(1,1)` unless a smaller fixed lookup key is more suitable |
| Short text | `NVARCHAR(n)` with a stated maximum length |
| Money values | `DECIMAL(10,2)` rather than approximate numeric types |
| Geographic coordinates | `DECIMAL(9,6)` for latitude and longitude |
| Dates only | `DATE` |
| Timestamps | `DATETIME2(0)` stored in UTC where applicable |
| True or false values | `BIT` |
| Status values | `NVARCHAR` columns restricted by `CHECK` constraints |
| Delete behaviour | `NO ACTION` for historical and transactional relationships |

## 3. Final entity list

| Entity | Purpose |
|---|---|
| `Roles` | Stores the two permitted user roles |
| `Users` | Stores Organiser and Participant accounts and profile information |
| `Events` | Stores events created and managed by Organisers |
| `EventCategories` | Stores the entry categories available for each Event |
| `EventRoutes` | Stores route information for an Event Category |
| `Enrolments` | Records a Participant's entry into an Event and selected Category |
| `Results` | Stores the official outcome for an Enrolment |

## 4. Relationship summary

| Parent entity | Relationship | Child entity | Rule |
|---|---|---|---|
| `Roles` | One to many | `Users` | Every User has one Role |
| `Users` | One to many | `Events` | One Organiser may manage many Events |
| `Events` | One to many | `EventCategories` | Every Category belongs to one Event |
| `EventCategories` | One to zero or one | `EventRoutes` | A Category may have one Route record |
| `Users` | One to many | `Enrolments` | One Participant may have many Enrolments |
| `Events` | One to many | `Enrolments` | One Event may have many Enrolments |
| `EventCategories` | One to many | `Enrolments` | One Category may be selected by many Enrolments |
| `Enrolments` | One to zero or one | `Results` | An Enrolment may have one official Result |
| `Users` | One to many | `Results` | One Organiser may record many Results |

## 5. Entity definitions

### 5.1 Roles

The `Roles` table restricts accounts to the Organiser and Participant roles.

| Column | SQL Server datatype | Null | Key or constraint | Description |
|---|---|---:|---|---|
| `RoleId` | `TINYINT` | No | Primary key | Fixed identifier for a Role |
| `RoleName` | `NVARCHAR(20)` | No | Unique | Role name, limited to Organiser or Participant |

Table rules:

- Seed `RoleId` 1 as Organiser.
- Seed `RoleId` 2 as Participant.
- Restrict `RoleName` to `Organiser` or `Participant`.
- Role records may not be deleted while Users refer to them.

### 5.2 Users

The `Users` table stores account and profile information for Organisers and Participants.

| Column | SQL Server datatype | Null | Key or constraint | Description |
|---|---|---:|---|---|
| `UserId` | `INT IDENTITY(1,1)` | No | Primary key | Unique User identifier |
| `RoleId` | `TINYINT` | No | Foreign key to `Roles.RoleId` | Assigned account Role |
| `FirstName` | `NVARCHAR(50)` | No |  | User's first name |
| `LastName` | `NVARCHAR(50)` | No |  | User's surname |
| `Email` | `NVARCHAR(254)` | No | Unique | Normalised sign-in email address |
| `PasswordHash` | `NVARCHAR(255)` | No |  | Password hash produced by the future API |
| `PhoneNumber` | `NVARCHAR(20)` | Yes |  | Optional contact number |
| `DateOfBirth` | `DATE` | Yes | Check date is earlier than the current date | Participant date of birth when supplied |
| `ProfilePictureBlobName` | `NVARCHAR(255)` | Yes | Check nonblank when supplied | Azure Blob Storage object name for the Participant's profile picture |
| `IsActive` | `BIT` | No | Default 1 | Indicates whether the account may be used |
| `CreatedAt` | `DATETIME2(0)` | No | Default current UTC date and time | Account creation timestamp |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest profile update timestamp |

Table rules:

- `Email` must be unique.
- `FirstName`, `LastName`, `Email` and `PasswordHash` may not be empty strings.
- `ProfilePictureBlobName` is optional, but may not be an empty string when supplied.
- `RoleId` must refer to an existing Role.
- User records referenced by Events, Enrolments or Results use `NO ACTION` delete behaviour.
- Organiser ownership and Participant privacy are enforced by the future API because a foreign key alone cannot verify a User's Role.

Planned indexes:

- Unique index on `Email`.
- Nonclustered index on `RoleId`.

### 5.3 Events

The `Events` table stores the event information managed by an Organiser.

| Column | SQL Server datatype | Null | Key or constraint | Description |
|---|---|---:|---|---|
| `EventId` | `INT IDENTITY(1,1)` | No | Primary key | Unique Event identifier |
| `OrganiserUserId` | `INT` | No | Foreign key to `Users.UserId` | Organiser responsible for the Event |
| `EventName` | `NVARCHAR(120)` | No |  | Public Event name |
| `EventType` | `NVARCHAR(20)` | No | Check constraint | Running, Walking or Cycling |
| `Description` | `NVARCHAR(1000)` | No |  | Public Event description |
| `BannerImageBlobName` | `NVARCHAR(255)` | Yes | Check nonblank when supplied | Azure Blob Storage object name for the Event banner image |
| `DistanceKm` | `DECIMAL(6,2)` | No | Check greater than zero | Advertised Event distance in kilometres |
| `EventDateTime` | `DATETIME2(0)` | No |  | Scheduled start date and time |
| `EntryClosingDateTime` | `DATETIME2(0)` | No | Check before `EventDateTime` | Final date and time for entries |
| `VenueName` | `NVARCHAR(120)` | No |  | Starting venue or recognised location |
| `AddressLine1` | `NVARCHAR(120)` | No |  | Main street address |
| `City` | `NVARCHAR(80)` | No |  | City or town |
| `Province` | `NVARCHAR(30)` | No | Check constraint | South African province |
| `Latitude` | `DECIMAL(9,6)` | Yes | Check from -90 to 90 | Coordinate used for location and weather lookup |
| `Longitude` | `DECIMAL(9,6)` | Yes | Check from -180 to 180 | Coordinate used for location and weather lookup |
| `Status` | `NVARCHAR(20)` | No | Default Draft and check constraint | Draft, Open, Closed, Cancelled or Completed |
| `CreatedAt` | `DATETIME2(0)` | No | Default current UTC date and time | Event creation timestamp |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest Event update timestamp |

Table rules:

- `EventName`, `Description`, `VenueName`, `AddressLine1` and `City` may not be empty strings.
- `BannerImageBlobName` is optional, but may not be an empty string when supplied.
- `DistanceKm` must be greater than zero.
- `EntryClosingDateTime` must be earlier than `EventDateTime`.
- `EventType` is limited to Running, Walking or Cycling.
- `Province` is limited to the nine South African provinces.
- `Status` is limited to Draft, Open, Closed, Cancelled or Completed.
- `OrganiserUserId` must refer to an active Organiser before the future API accepts the request.
- An Event with dependent Categories, Enrolments or Results should be cancelled instead of deleted.

Planned indexes:

- Nonclustered index on `OrganiserUserId`.
- Nonclustered index on `EventDateTime` and `Status` to support upcoming-event browsing.

### 5.4 EventCategories

The `EventCategories` table stores the entry choices available for an Event.

| Column | SQL Server datatype | Null | Key or constraint | Description |
|---|---|---:|---|---|
| `EventCategoryId` | `INT IDENTITY(1,1)` | No | Primary key | Unique Category identifier |
| `EventId` | `INT` | No | Foreign key to `Events.EventId` | Event that owns the Category |
| `CategoryName` | `NVARCHAR(80)` | No | Unique within an Event | Public Category name |
| `DistanceKm` | `DECIMAL(6,2)` | No | Check greater than zero | Category distance in kilometres |
| `MinimumAge` | `TINYINT` | Yes | Check from 1 to 120 | Minimum permitted age when applicable |
| `MaximumAge` | `TINYINT` | Yes | Check from 1 to 120 | Maximum permitted age when applicable |
| `EntryFee` | `DECIMAL(10,2)` | No | Default 0 and check at least zero | Entry fee in South African rand |
| `Capacity` | `INT` | Yes | Check greater than zero | Optional maximum number of entries |
| `IsAvailable` | `BIT` | No | Default 1 | Indicates whether new entries may select the Category |
| `CreatedAt` | `DATETIME2(0)` | No | Default current UTC date and time | Category creation timestamp |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest Category update timestamp |

Table rules:

- The combination of `EventId` and `CategoryName` must be unique.
- The combination of `EventCategoryId` and `EventId` will be an alternate key used by the Enrolments composite foreign key.
- `DistanceKm` must be greater than zero.
- `MinimumAge` must be between 1 and 120 when supplied.
- `MaximumAge` must be between 1 and 120 when supplied.
- `MinimumAge` cannot be greater than `MaximumAge` when both are supplied.
- `EntryFee` must be zero or greater.
- `Capacity` must be greater than zero when supplied.
- A Category with Enrolments should be made unavailable instead of deleted.

Planned indexes:

- Unique index on `EventId` and `CategoryName`.
- Unique index on `EventCategoryId` and `EventId` for composite referential integrity.
- Nonclustered index on `EventId`.

### 5.5 EventRoutes

The `EventRoutes` table stores route details for an Event Category. Keeping route information at Category level supports Events that offer different distances and routes.

| Column | SQL Server datatype | Null | Key or constraint | Description |
|---|---|---:|---|---|
| `RouteId` | `INT IDENTITY(1,1)` | No | Primary key | Unique Route identifier |
| `EventCategoryId` | `INT` | No | Unique foreign key to `EventCategories.EventCategoryId` | Category that uses the Route |
| `RouteName` | `NVARCHAR(120)` | No |  | Public Route name |
| `StartLocation` | `NVARCHAR(200)` | No |  | Description of the starting point |
| `FinishLocation` | `NVARCHAR(200)` | No |  | Description of the finishing point |
| `RouteDescription` | `NVARCHAR(1000)` | No |  | Instructions and important Route information |
| `RouteMapUrl` | `NVARCHAR(500)` | Yes |  | Optional link to a Route map |
| `ElevationGainMetres` | `INT` | Yes | Check at least zero | Optional total elevation gain |
| `CreatedAt` | `DATETIME2(0)` | No | Default current UTC date and time | Route creation timestamp |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest Route update timestamp |

Table rules:

- `EventCategoryId` must be unique so that a Category has no more than one Route.
- `RouteName`, `StartLocation`, `FinishLocation` and `RouteDescription` may not be empty strings.
- `ElevationGainMetres` must be zero or greater when supplied.
- A Route record cannot exist without its Event Category.

Planned indexes:

- Unique index on `EventCategoryId`.

### 5.6 Enrolments

The `Enrolments` table records a Participant's entry into an Event and the Category selected for that entry.

| Column | SQL Server datatype | Null | Key or constraint | Description |
|---|---|---:|---|---|
| `EnrolmentId` | `INT IDENTITY(1,1)` | No | Primary key | Unique Enrolment identifier |
| `ParticipantUserId` | `INT` | No | Foreign key to `Users.UserId` | Participant who entered the Event |
| `EventId` | `INT` | No | Foreign key to `Events.EventId` | Event entered by the Participant |
| `EventCategoryId` | `INT` | No | Part of composite foreign key | Category selected by the Participant |
| `EnrolmentDate` | `DATETIME2(0)` | No | Default current UTC date and time | Date and time the entry was created |
| `Status` | `NVARCHAR(20)` | No | Default Confirmed and check constraint | Confirmed or Cancelled |
| `BibNumber` | `NVARCHAR(20)` | Yes | Unique within an Event when supplied | Race number allocated to the Participant |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest Enrolment update timestamp |

Table rules:

- `ParticipantUserId` must refer to an existing User.
- The future API must verify that `ParticipantUserId` belongs to a Participant.
- `EventId` must refer to an existing Event.
- The combination of `EventCategoryId` and `EventId` must refer to the matching alternate key in `EventCategories`.
- The combination of `ParticipantUserId` and `EventId` must be unique. A cancelled entry may be reactivated or updated instead of creating another row.
- `Status` is limited to Confirmed or Cancelled.
- `BibNumber` must be unique within an Event when supplied.
- The future API must check Event status, closing date, Category availability and capacity before creating an Enrolment.

Planned indexes:

- Unique index on `ParticipantUserId` and `EventId`.
- Nonclustered index on `EventId` and `Status` for Organiser enrolment lists.
- Nonclustered index on `EventCategoryId`.
- Unique filtered index on `EventId` and `BibNumber` where `BibNumber` is not null. The filtered index allows multiple records without allocated bib numbers while preserving uniqueness for assigned numbers (Microsoft, 2026e).

### 5.7 Results

The `Results` table stores the official outcome for an Enrolment.

| Column | SQL Server datatype | Null | Key or constraint | Description |
|---|---|---:|---|---|
| `ResultId` | `INT IDENTITY(1,1)` | No | Primary key | Unique Result identifier |
| `EnrolmentId` | `INT` | No | Unique foreign key to `Enrolments.EnrolmentId` | Enrolment receiving the Result |
| `RecordedByUserId` | `INT` | No | Foreign key to `Users.UserId` | Organiser who recorded the Result |
| `ResultStatus` | `NVARCHAR(20)` | No | Default Completed and check constraint | Completed, DidNotFinish, Disqualified or DidNotStart |
| `FinishTimeSeconds` | `INT` | Yes | Check greater than zero when completed | Official elapsed time in seconds |
| `OverallPosition` | `INT` | Yes | Check greater than zero | Overall finishing position |
| `CategoryPosition` | `INT` | Yes | Check greater than zero | Finishing position within the Category |
| `Notes` | `NVARCHAR(500)` | Yes |  | Optional Result explanation |
| `RecordedAt` | `DATETIME2(0)` | No | Default current UTC date and time | Date and time the Result was first recorded |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest Result correction timestamp |

Table rules:

- `EnrolmentId` must be unique so that an Enrolment has no more than one official Result.
- `RecordedByUserId` must refer to an existing User.
- The future API must verify that `RecordedByUserId` belongs to the Organiser responsible for the related Event.
- `ResultStatus` is limited to Completed, DidNotFinish, Disqualified or DidNotStart.
- A Completed Result must have a positive `FinishTimeSeconds` value.
- A non-completed Result will not have an official finish time.
- `OverallPosition` and `CategoryPosition` must be positive when supplied.
- The future API must reject a Result for a cancelled Enrolment.

Planned indexes:

- Unique index on `EnrolmentId`.
- Nonclustered index on `RecordedByUserId`.
- Nonclustered index on `ResultStatus`.

## 6. Cross-table integrity plan

`UNIQUE` and `CHECK` constraints are used where values or combinations must be controlled by the database (Microsoft, 2026d).

| Integrity rule | Planned database implementation |
|---|---|
| Every User has one valid Role | Foreign key from `Users.RoleId` to `Roles.RoleId` |
| Every Event has one valid Organiser account record | Foreign key from `Events.OrganiserUserId` to `Users.UserId` |
| Category names do not repeat within an Event | Unique constraint on `EventCategories.EventId` and `CategoryName` |
| An Enrolment's Category belongs to its Event | Composite foreign key from `Enrolments.EventCategoryId, EventId` to `EventCategories.EventCategoryId, EventId` |
| A Participant has one Enrolment record per Event | Unique constraint on `Enrolments.ParticipantUserId, EventId` |
| A Category has no more than one Route | Unique constraint on `EventRoutes.EventCategoryId` |
| An Enrolment has no more than one Result | Unique constraint on `Results.EnrolmentId` |
| Assigned bib numbers do not repeat within an Event | Unique filtered index on `Enrolments.EventId, BibNumber` |
| Status and range values remain valid | Named `CHECK` constraints on the relevant tables |

## 7. Delete and update behaviour

Foreign keys protect related data from becoming orphaned. SQL Server supports several referential actions, including `NO ACTION`, `CASCADE`, `SET NULL` and `SET DEFAULT` (Microsoft, 2026c). RaceDay will use the following approach:

| Relationship | Delete behaviour | Reason |
|---|---|---|
| Roles to Users | `NO ACTION` | A Role in use must be retained |
| Users to Events | `NO ACTION` | Event ownership history must be retained |
| Events to EventCategories | `NO ACTION` | Existing Event structure must not be removed accidentally |
| EventCategories to EventRoutes | `NO ACTION` | Route removal must be deliberate |
| Users to Enrolments | `NO ACTION` | Participation history must be retained |
| Events to Enrolments | `NO ACTION` | Event-entry history must be retained |
| EventCategories to Enrolments | `NO ACTION` | The selected Category must remain traceable |
| Enrolments to Results | `NO ACTION` | Official Result history must be retained |
| Users to Results | `NO ACTION` | The recording Organiser must remain traceable |

Records that already have dependent transactional data should generally be made inactive or cancelled instead of being deleted.

## 8. Application-level validation

Some rules require information from more than one row or table and will be enforced by the future API rather than by simple table constraints:

- Confirm that an Event owner has the Organiser Role.
- Confirm that an Enrolment user has the Participant Role.
- Confirm that the Event is Open and entries have not closed.
- Confirm that an Event Category is available and has capacity.
- Confirm that a Participant satisfies the Category's minimum and maximum age rules when those values are supplied.
- Confirm that a Result is captured by the Organiser responsible for the related Event.
- Confirm that a cancelled Enrolment cannot receive a Result.
- Update `UpdatedAt` when a record changes.

## 9. Normalisation summary

The model separates Roles, Users, Events, Categories, Routes, Enrolments and Results so that each table represents one main subject. Repeating groups are avoided, descriptive values are stored once where practical and many-to-many activity between Participants and Events is resolved through Enrolments. The separate Results table prevents result details from being repeated in user or event records.

## References

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.

Microsoft (2026a) 'Data types (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-ca/sql/t-sql/data-types/data-types-transact-sql?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026b) 'nchar and nvarchar (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/data-types/nchar-and-nvarchar-transact-sql?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026c) 'Primary and foreign key constraints', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026d) 'Unique constraints and check constraints', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026e) 'Create filtered indexes', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/relational-databases/indexes/create-filtered-indexes?view=sql-server-ver17 (Accessed: 19 September 2026).
