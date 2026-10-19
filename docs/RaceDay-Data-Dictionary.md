# RaceDay Data Dictionary

## 1. Purpose

This defines RaceDay's SQL columns, keys and relationships. ERD and SQL use the same definitions. Unicode text uses NVARCHAR; keys enforce integrity (Microsoft, 2026a; 2026b; 2026c).
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
| `PasswordHash` | `NVARCHAR(255)` | No |  | Stored password hash |
| `PhoneNumber` | `NVARCHAR(20)` | Yes |  | Optional contact number |
| `DateOfBirth` | `DATE` | Yes | Check date is earlier than the current date | Participant date of birth when supplied |
| `ProfilePictureBlobName` | `NVARCHAR(255)` | Yes | Check nonblank when supplied | Azure Blob Storage object name for the Participant's profile picture |
| `IsActive` | `BIT` | No | Default 1 | Indicates whether the account may be used |
| `CreatedAt` | `DATETIME2(0)` | No | Default current UTC date and time | Account creation timestamp |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest profile update timestamp |

Table rules: Email is unique; names, email, hash and supplied blob names cannot be blank. RoleId references Roles. The API checks roles and privacy; SQL protects referenced users with NO ACTION.

Indexes:

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

Table rules: Required text and supplied blob names cannot be blank. Distance is positive; entries close before start. Types, provinces and statuses use the listed choices. The API checks the active Organiser. Cancel events with dependent history instead of deleting them.

Indexes:

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

Table rules: Names are unique within the event. The category/event composite key supports matching enrolments. Distance/capacity are positive; fee is non-negative. Optional ages are 1-120 with minimum no higher than maximum. Categories with entries are made unavailable instead of deleted.

Indexes:

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

Table rules: One route per category. Required text cannot be blank; optional elevation is non-negative. A route cannot exist without its category.

Indexes:

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

Table rules: One saved entry per Participant/event. The composite category/event link must match. Status is Confirmed or Cancelled; assigned bibs are unique within the event. The API checks role, Open status, closing time, availability, capacity and inclusive age on the UTC event date. Missing required birth dates return 400. Cancellation keeps the row; DELETE removes only eligible Confirmed entries. Reactivation reuses the Cancelled row and clears the bib if category changes.

Indexes:

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
| `FinishTimeSeconds` | `INT` | Yes | Required and greater than zero for Completed; otherwise NULL | Official elapsed time in seconds |
| `OverallPosition` | `INT` | Yes | Check greater than zero | Overall finishing position |
| `CategoryPosition` | `INT` | Yes | Check greater than zero | Finishing position within the Category |
| `Notes` | `NVARCHAR(500)` | Yes |  | Optional Result explanation |
| `RecordedAt` | `DATETIME2(0)` | No | Default current UTC date and time | Date and time the Result was first recorded |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest Result correction timestamp |

Table rules: One result per enrolment. The API checks the owning Organiser and rejects Cancelled entries. Completed requires positive, non-null seconds; other statuses require NULL. IS NOT NULL prevents an UNKNOWN CHECK result accepting a missing finish time. Supplied positions are positive (Microsoft, 2026d).

Indexes:

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

All nine foreign keys use NO ACTION on delete and update. Remove dependent records deliberately where permitted; otherwise cancel or deactivate records to preserve history (Microsoft, 2026c).
## 8. Application-level validation

The API checks roles, ownership, event status/closing times, category availability/capacity, ages and result eligibility. It also updates UpdatedAt. These rules need more than a simple foreign key or table CHECK.
## 9. Normalisation summary

Each table represents one subject. Enrolments resolves Participant/Event entries, while Results keeps outcomes separate. Shared account, event and category descriptions are not repeated in entries.
## References

The Independent Institute of Education (2026) *PROG6212 Portfolio of Evidence*. Unpublished assessment brief.

Microsoft (2026a) 'Data types (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-ca/sql/t-sql/data-types/data-types-transact-sql?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026b) 'nchar and nvarchar (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/data-types/nchar-and-nvarchar-transact-sql?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026c) 'Primary and foreign key constraints', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026d) 'Unique constraints and check constraints', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026e) 'Create filtered indexes', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/relational-databases/indexes/create-filtered-indexes?view=sql-server-ver17 (Accessed: 19 September 2026).
