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
| `IsActive` | `BIT` | No | Default 1 | Indicates whether the account may be used |
| `CreatedAt` | `DATETIME2(0)` | No | Default current UTC date and time | Account creation timestamp |
| `UpdatedAt` | `DATETIME2(0)` | Yes |  | Latest profile update timestamp |

Table rules:

- `Email` must be unique.
- `FirstName`, `LastName`, `Email` and `PasswordHash` may not be empty strings.
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
- `EntryFee` must be zero or greater.
- `Capacity` must be greater than zero when supplied.
- A Category with Enrolments should be made unavailable instead of deleted.

Planned indexes:

- Unique index on `EventId` and `CategoryName`.
- Unique index on `EventCategoryId` and `EventId` for composite referential integrity.
- Nonclustered index on `EventId`.

## References

Microsoft (2026a) 'Data types (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-ca/sql/t-sql/data-types/data-types-transact-sql?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026b) 'nchar and nvarchar (Transact-SQL)', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/t-sql/data-types/nchar-and-nvarchar-transact-sql?view=sql-server-ver17 (Accessed: 19 September 2026).

Microsoft (2026c) 'Primary and foreign key constraints', *Microsoft Learn*. Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints?view=sql-server-ver17 (Accessed: 19 September 2026).

