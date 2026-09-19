/*
    RaceDay Part 1 SQL Server database script

    Purpose:
    Create the RaceDay database and the seven tables defined in the ERD and
    data dictionary. Seed data will be added in the next database session.

    Execution:
    Open this file in SQL Server Management Studio and run it while connected
    to a SQL Server instance with permission to create a database.

    Design source:
    The Independent Institute of Education (2026) and the RaceDay planning
    documents in the /docs folder.
*/

USE [master];
GO

-- CREATE DATABASE must execute in its own batch (Microsoft, 2025).
IF DB_ID(N'RaceDay') IS NULL
BEGIN
    EXEC (N'CREATE DATABASE [RaceDay];');
END;
GO

USE [RaceDay];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.tables
    WHERE schema_id = SCHEMA_ID(N'dbo')
      AND name IN
      (
          N'Roles',
          N'Users',
          N'Events',
          N'EventCategories',
          N'EventRoutes',
          N'Enrolments',
          N'Results'
      )
)
BEGIN
    ;THROW 50001, 'RaceDay tables already exist. Run this script against a clean RaceDay database.', 1;
END;
GO

BEGIN TRY
    BEGIN TRANSACTION;

    CREATE TABLE dbo.Roles
    (
        RoleId TINYINT NOT NULL,
        RoleName NVARCHAR(20) NOT NULL,

        CONSTRAINT PK_Roles
            PRIMARY KEY (RoleId),
        CONSTRAINT UQ_Roles_RoleName
            UNIQUE (RoleName),
        CONSTRAINT CK_Roles_RoleName
            CHECK (RoleName IN (N'Organiser', N'Participant'))
    );

    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1,1) NOT NULL,
        RoleId TINYINT NOT NULL,
        FirstName NVARCHAR(50) NOT NULL,
        LastName NVARCHAR(50) NOT NULL,
        Email NVARCHAR(254) NOT NULL,
        PasswordHash NVARCHAR(255) NOT NULL,
        PhoneNumber NVARCHAR(20) NULL,
        DateOfBirth DATE NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,

        CONSTRAINT PK_Users
            PRIMARY KEY (UserId),
        CONSTRAINT UQ_Users_Email
            UNIQUE (Email),
        CONSTRAINT FK_Users_Roles
            FOREIGN KEY (RoleId)
            REFERENCES dbo.Roles (RoleId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT CK_Users_FirstName_NotBlank
            CHECK (LEN(LTRIM(RTRIM(FirstName))) > 0),
        CONSTRAINT CK_Users_LastName_NotBlank
            CHECK (LEN(LTRIM(RTRIM(LastName))) > 0),
        CONSTRAINT CK_Users_Email_NotBlank
            CHECK (LEN(LTRIM(RTRIM(Email))) > 0),
        CONSTRAINT CK_Users_PasswordHash_NotBlank
            CHECK (LEN(LTRIM(RTRIM(PasswordHash))) > 0),
        CONSTRAINT CK_Users_DateOfBirth
            CHECK
            (
                DateOfBirth IS NULL
                OR DateOfBirth < CONVERT(DATE, SYSUTCDATETIME())
            )
    );

    CREATE TABLE dbo.Events
    (
        EventId INT IDENTITY(1,1) NOT NULL,
        OrganiserUserId INT NOT NULL,
        EventName NVARCHAR(120) NOT NULL,
        EventType NVARCHAR(20) NOT NULL,
        Description NVARCHAR(1000) NOT NULL,
        EventDateTime DATETIME2(0) NOT NULL,
        EntryClosingDateTime DATETIME2(0) NOT NULL,
        VenueName NVARCHAR(120) NOT NULL,
        AddressLine1 NVARCHAR(120) NOT NULL,
        City NVARCHAR(80) NOT NULL,
        Province NVARCHAR(30) NOT NULL,
        Latitude DECIMAL(9,6) NULL,
        Longitude DECIMAL(9,6) NULL,
        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Events_Status DEFAULT (N'Draft'),
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_Events_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,

        CONSTRAINT PK_Events
            PRIMARY KEY (EventId),
        CONSTRAINT FK_Events_Users
            FOREIGN KEY (OrganiserUserId)
            REFERENCES dbo.Users (UserId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT CK_Events_EventName_NotBlank
            CHECK (LEN(LTRIM(RTRIM(EventName))) > 0),
        CONSTRAINT CK_Events_EventType
            CHECK (EventType IN (N'Running', N'Walking', N'Cycling')),
        CONSTRAINT CK_Events_Description_NotBlank
            CHECK (LEN(LTRIM(RTRIM(Description))) > 0),
        CONSTRAINT CK_Events_EntryClosingDateTime
            CHECK (EntryClosingDateTime < EventDateTime),
        CONSTRAINT CK_Events_VenueName_NotBlank
            CHECK (LEN(LTRIM(RTRIM(VenueName))) > 0),
        CONSTRAINT CK_Events_AddressLine1_NotBlank
            CHECK (LEN(LTRIM(RTRIM(AddressLine1))) > 0),
        CONSTRAINT CK_Events_City_NotBlank
            CHECK (LEN(LTRIM(RTRIM(City))) > 0),
        CONSTRAINT CK_Events_Province
            CHECK
            (
                Province IN
                (
                    N'Eastern Cape',
                    N'Free State',
                    N'Gauteng',
                    N'KwaZulu-Natal',
                    N'Limpopo',
                    N'Mpumalanga',
                    N'North West',
                    N'Northern Cape',
                    N'Western Cape'
                )
            ),
        CONSTRAINT CK_Events_Latitude
            CHECK (Latitude IS NULL OR Latitude BETWEEN -90 AND 90),
        CONSTRAINT CK_Events_Longitude
            CHECK (Longitude IS NULL OR Longitude BETWEEN -180 AND 180),
        CONSTRAINT CK_Events_Status
            CHECK (Status IN (N'Draft', N'Open', N'Closed', N'Cancelled', N'Completed'))
    );

    CREATE TABLE dbo.EventCategories
    (
        EventCategoryId INT IDENTITY(1,1) NOT NULL,
        EventId INT NOT NULL,
        CategoryName NVARCHAR(80) NOT NULL,
        DistanceKm DECIMAL(6,2) NOT NULL,
        MinimumAge TINYINT NULL,
        EntryFee DECIMAL(10,2) NOT NULL
            CONSTRAINT DF_EventCategories_EntryFee DEFAULT (0),
        Capacity INT NULL,
        IsAvailable BIT NOT NULL
            CONSTRAINT DF_EventCategories_IsAvailable DEFAULT (1),
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_EventCategories_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,

        CONSTRAINT PK_EventCategories
            PRIMARY KEY (EventCategoryId),
        CONSTRAINT UQ_EventCategories_Event_CategoryName
            UNIQUE (EventId, CategoryName),
        CONSTRAINT UQ_EventCategories_Category_Event
            UNIQUE (EventCategoryId, EventId),
        CONSTRAINT FK_EventCategories_Events
            FOREIGN KEY (EventId)
            REFERENCES dbo.Events (EventId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT CK_EventCategories_DistanceKm
            CHECK (DistanceKm > 0),
        CONSTRAINT CK_EventCategories_MinimumAge
            CHECK (MinimumAge IS NULL OR MinimumAge BETWEEN 1 AND 120),
        CONSTRAINT CK_EventCategories_EntryFee
            CHECK (EntryFee >= 0),
        CONSTRAINT CK_EventCategories_Capacity
            CHECK (Capacity IS NULL OR Capacity > 0)
    );

    CREATE TABLE dbo.EventRoutes
    (
        RouteId INT IDENTITY(1,1) NOT NULL,
        EventCategoryId INT NOT NULL,
        RouteName NVARCHAR(120) NOT NULL,
        StartLocation NVARCHAR(200) NOT NULL,
        FinishLocation NVARCHAR(200) NOT NULL,
        RouteDescription NVARCHAR(1000) NOT NULL,
        RouteMapUrl NVARCHAR(500) NULL,
        ElevationGainMetres INT NULL,
        CreatedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_EventRoutes_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,

        CONSTRAINT PK_EventRoutes
            PRIMARY KEY (RouteId),
        CONSTRAINT UQ_EventRoutes_EventCategoryId
            UNIQUE (EventCategoryId),
        CONSTRAINT FK_EventRoutes_EventCategories
            FOREIGN KEY (EventCategoryId)
            REFERENCES dbo.EventCategories (EventCategoryId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT CK_EventRoutes_RouteName_NotBlank
            CHECK (LEN(LTRIM(RTRIM(RouteName))) > 0),
        CONSTRAINT CK_EventRoutes_StartLocation_NotBlank
            CHECK (LEN(LTRIM(RTRIM(StartLocation))) > 0),
        CONSTRAINT CK_EventRoutes_FinishLocation_NotBlank
            CHECK (LEN(LTRIM(RTRIM(FinishLocation))) > 0),
        CONSTRAINT CK_EventRoutes_RouteDescription_NotBlank
            CHECK (LEN(LTRIM(RTRIM(RouteDescription))) > 0),
        CONSTRAINT CK_EventRoutes_ElevationGainMetres
            CHECK (ElevationGainMetres IS NULL OR ElevationGainMetres >= 0)
    );

    CREATE TABLE dbo.Enrolments
    (
        EnrolmentId INT IDENTITY(1,1) NOT NULL,
        ParticipantUserId INT NOT NULL,
        EventId INT NOT NULL,
        EventCategoryId INT NOT NULL,
        EnrolmentDate DATETIME2(0) NOT NULL
            CONSTRAINT DF_Enrolments_EnrolmentDate DEFAULT (SYSUTCDATETIME()),
        Status NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Enrolments_Status DEFAULT (N'Confirmed'),
        BibNumber NVARCHAR(20) NULL,
        UpdatedAt DATETIME2(0) NULL,

        CONSTRAINT PK_Enrolments
            PRIMARY KEY (EnrolmentId),
        CONSTRAINT UQ_Enrolments_Participant_Event
            UNIQUE (ParticipantUserId, EventId),
        CONSTRAINT FK_Enrolments_Users
            FOREIGN KEY (ParticipantUserId)
            REFERENCES dbo.Users (UserId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT FK_Enrolments_Events
            FOREIGN KEY (EventId)
            REFERENCES dbo.Events (EventId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT FK_Enrolments_EventCategories
            FOREIGN KEY (EventCategoryId, EventId)
            REFERENCES dbo.EventCategories (EventCategoryId, EventId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT CK_Enrolments_Status
            CHECK (Status IN (N'Confirmed', N'Cancelled'))
    );

    CREATE TABLE dbo.Results
    (
        ResultId INT IDENTITY(1,1) NOT NULL,
        EnrolmentId INT NOT NULL,
        RecordedByUserId INT NOT NULL,
        ResultStatus NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Results_ResultStatus DEFAULT (N'Completed'),
        FinishTimeSeconds INT NULL,
        OverallPosition INT NULL,
        CategoryPosition INT NULL,
        Notes NVARCHAR(500) NULL,
        RecordedAt DATETIME2(0) NOT NULL
            CONSTRAINT DF_Results_RecordedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt DATETIME2(0) NULL,

        CONSTRAINT PK_Results
            PRIMARY KEY (ResultId),
        CONSTRAINT UQ_Results_EnrolmentId
            UNIQUE (EnrolmentId),
        CONSTRAINT FK_Results_Enrolments
            FOREIGN KEY (EnrolmentId)
            REFERENCES dbo.Enrolments (EnrolmentId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT FK_Results_Users
            FOREIGN KEY (RecordedByUserId)
            REFERENCES dbo.Users (UserId)
            ON DELETE NO ACTION
            ON UPDATE NO ACTION,
        CONSTRAINT CK_Results_ResultStatus
            CHECK
            (
                ResultStatus IN
                (
                    N'Completed',
                    N'DidNotFinish',
                    N'Disqualified',
                    N'DidNotStart'
                )
            ),
        CONSTRAINT CK_Results_FinishTime
            CHECK
            (
                (ResultStatus = N'Completed' AND FinishTimeSeconds > 0)
                OR
                (ResultStatus <> N'Completed' AND FinishTimeSeconds IS NULL)
            ),
        CONSTRAINT CK_Results_OverallPosition
            CHECK (OverallPosition IS NULL OR OverallPosition > 0),
        CONSTRAINT CK_Results_CategoryPosition
            CHECK (CategoryPosition IS NULL OR CategoryPosition > 0)
    );

    CREATE NONCLUSTERED INDEX IX_Users_RoleId
        ON dbo.Users (RoleId);

    CREATE NONCLUSTERED INDEX IX_Events_OrganiserUserId
        ON dbo.Events (OrganiserUserId);

    CREATE NONCLUSTERED INDEX IX_Events_EventDateTime_Status
        ON dbo.Events (EventDateTime, Status);

    CREATE NONCLUSTERED INDEX IX_EventCategories_EventId
        ON dbo.EventCategories (EventId);

    CREATE NONCLUSTERED INDEX IX_Enrolments_EventId_Status
        ON dbo.Enrolments (EventId, Status);

    CREATE NONCLUSTERED INDEX IX_Enrolments_EventCategoryId
        ON dbo.Enrolments (EventCategoryId);

    -- The filter permits multiple unassigned NULL values (Microsoft, 2026d).
    CREATE UNIQUE NONCLUSTERED INDEX UX_Enrolments_EventId_BibNumber
        ON dbo.Enrolments (EventId, BibNumber)
        WHERE BibNumber IS NOT NULL;

    CREATE NONCLUSTERED INDEX IX_Results_RecordedByUserId
        ON dbo.Results (RecordedByUserId);

    CREATE NONCLUSTERED INDEX IX_Results_ResultStatus
        ON dbo.Results (ResultStatus);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO

PRINT N'RaceDay database tables and constraints created successfully.';
GO

/*
    References

    Microsoft (2025) 'CREATE DATABASE (Transact-SQL)', Microsoft Learn.
    Available at: https://learn.microsoft.com/sql/t-sql/statements/create-database-transact-sql?view=sql-server-ver16
    (Accessed: 19 September 2026).

    Microsoft (2026a) 'Data types (Transact-SQL)', Microsoft Learn.
    Available at: https://learn.microsoft.com/en-ca/sql/t-sql/data-types/data-types-transact-sql?view=sql-server-ver17
    (Accessed: 19 September 2026).

    Microsoft (2026b) 'Primary and foreign key constraints', Microsoft Learn.
    Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/primary-and-foreign-key-constraints?view=sql-server-ver17
    (Accessed: 19 September 2026).

    Microsoft (2026c) 'Unique constraints and check constraints', Microsoft Learn.
    Available at: https://learn.microsoft.com/en-us/sql/relational-databases/tables/unique-constraints-and-check-constraints?view=sql-server-ver17
    (Accessed: 19 September 2026).

    Microsoft (2026d) 'Create filtered indexes', Microsoft Learn.
    Available at: https://learn.microsoft.com/en-us/sql/relational-databases/indexes/create-filtered-indexes?view=sql-server-ver17
    (Accessed: 19 September 2026).

    The Independent Institute of Education (2026) PROG6212 Portfolio of Evidence.
    Unpublished assessment brief.
*/
