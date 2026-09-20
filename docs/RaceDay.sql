/*
    RaceDay Part 1 SQL Server database script

    Purpose:
    Create the RaceDay database and the seven tables defined in the ERD and
    data dictionary, then add realistic sample data for every entity.

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
        ProfilePictureBlobName NVARCHAR(255) NULL,
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
        CONSTRAINT CK_Users_ProfilePictureBlobName_NotBlank
            CHECK
            (
                ProfilePictureBlobName IS NULL
                OR LEN(LTRIM(RTRIM(ProfilePictureBlobName))) > 0
            ),
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
        BannerImageBlobName NVARCHAR(255) NULL,
        DistanceKm DECIMAL(6,2) NOT NULL,
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
        CONSTRAINT CK_Events_BannerImageBlobName_NotBlank
            CHECK
            (
                BannerImageBlobName IS NULL
                OR LEN(LTRIM(RTRIM(BannerImageBlobName))) > 0
            ),
        CONSTRAINT CK_Events_DistanceKm
            CHECK (DistanceKm > 0),
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
        MaximumAge TINYINT NULL,
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
        CONSTRAINT CK_EventCategories_MaximumAge
            CHECK (MaximumAge IS NULL OR MaximumAge BETWEEN 1 AND 120),
        CONSTRAINT CK_EventCategories_AgeRange
            CHECK
            (
                MinimumAge IS NULL
                OR MaximumAge IS NULL
                OR MinimumAge <= MaximumAge
            ),
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

    /*
        Seed reference and core event data

        All names, contact details and events below are fictional and are used
        only to demonstrate the RaceDay database design.

        The sample password values are stored as hashes that can be verified by
        the future ASP.NET Core Identity integration (Microsoft, 2026f).
    */

    INSERT INTO dbo.Roles
    (
        RoleId,
        RoleName
    )
    VALUES
        (1, N'Organiser'),
        (2, N'Participant');

    -- Multi-row INSERT statements follow the SQL Server syntax (Microsoft, 2026e).
    INSERT INTO dbo.Users
    (
        RoleId,
        FirstName,
        LastName,
        Email,
        PasswordHash,
        PhoneNumber,
        DateOfBirth,
        IsActive,
        CreatedAt
    )
    VALUES
        (
            1,
            N'Thabo',
            N'Ndlovu',
            N'thabo.ndlovu@example.com',
            N'AQAAAAIAAYagAAAAEMJEo34Vex/Es3ymj/KGDAsh0XTg/maiNgOcLa2boUyPUM5+RCqT/0h++cgbZz9QKQ==',
            N'0825550101',
            NULL,
            1,
            '2026-09-19T12:00:00'
        ),
        (
            1,
            N'Ayesha',
            N'Khan',
            N'ayesha.khan@example.com',
            N'AQAAAAIAAYagAAAAENQbJEgO1gVHjHJ5qFByozUY2cbU8ylUaFYisC4nx4Hg/xMev27k+Ge6phAOBOsxCA==',
            N'0835550102',
            NULL,
            1,
            '2026-09-19T12:05:00'
        ),
        (
            2,
            N'Naledi',
            N'Mokoena',
            N'naledi.mokoena@example.com',
            N'AQAAAAIAAYagAAAAEDGx3Kf8pdpEdcvJgX7WsWvSff/SlnpYHBOzinfgqgYepnz19uEvaNNth2Tp67JSKQ==',
            N'0845550103',
            '1994-06-12',
            1,
            '2026-09-19T12:10:00'
        ),
        (
            2,
            N'Sipho',
            N'Dlamini',
            N'sipho.dlamini@example.com',
            N'AQAAAAIAAYagAAAAEGKZ3ZVEHVCoL6bra3uVYqs9oPOEklCO8xFdN2QUbsquTGekE1T4L64zlVfYua6tuw==',
            N'0715550104',
            '1989-11-03',
            1,
            '2026-09-19T12:15:00'
        );

    DECLARE @ThaboUserId INT =
    (
        SELECT UserId
        FROM dbo.Users
        WHERE Email = N'thabo.ndlovu@example.com'
    );

    DECLARE @AyeshaUserId INT =
    (
        SELECT UserId
        FROM dbo.Users
        WHERE Email = N'ayesha.khan@example.com'
    );

    INSERT INTO dbo.Events
    (
        OrganiserUserId,
        EventName,
        EventType,
        Description,
        DistanceKm,
        EventDateTime,
        EntryClosingDateTime,
        VenueName,
        AddressLine1,
        City,
        Province,
        Latitude,
        Longitude,
        Status,
        CreatedAt
    )
    VALUES
        (
            @ThaboUserId,
            N'Durban Sunrise 10K',
            N'Running',
            N'A coastal road race with five and ten kilometre categories.',
            10.00,
            '2026-08-16T04:30:00',
            '2026-08-09T21:59:59',
            N'Moses Mabhida Stadium',
            N'44 Isaiah Ntshangase Road',
            N'Durban',
            N'KwaZulu-Natal',
            -29.829000,
            31.030300,
            N'Completed',
            '2026-05-01T08:00:00'
        ),
        (
            @AyeshaUserId,
            N'Cape Peninsula Cycle Challenge',
            N'Cycling',
            N'A supported road cycling event with forty and eighty kilometre routes.',
            80.00,
            '2027-03-14T04:00:00',
            '2027-03-01T21:59:59',
            N'Green Point Urban Park',
            N'1 Fritz Sonnenberg Road',
            N'Cape Town',
            N'Western Cape',
            -33.905800,
            18.411900,
            N'Open',
            '2026-09-01T09:00:00'
        ),
        (
            @ThaboUserId,
            N'Soweto Heritage Walk',
            N'Walking',
            N'A community walk visiting important heritage locations in Soweto.',
            10.00,
            '2027-04-24T05:30:00',
            '2027-04-17T21:59:59',
            N'Walter Sisulu Square',
            N'1 Klipspruit Valley Road',
            N'Soweto',
            N'Gauteng',
            -26.278400,
            27.858500,
            N'Open',
            '2026-09-10T10:00:00'
        );

    DECLARE @DurbanEventId INT =
    (
        SELECT EventId
        FROM dbo.Events
        WHERE EventName = N'Durban Sunrise 10K'
    );

    DECLARE @CapeEventId INT =
    (
        SELECT EventId
        FROM dbo.Events
        WHERE EventName = N'Cape Peninsula Cycle Challenge'
    );

    DECLARE @SowetoEventId INT =
    (
        SELECT EventId
        FROM dbo.Events
        WHERE EventName = N'Soweto Heritage Walk'
    );

    INSERT INTO dbo.EventCategories
    (
        EventId,
        CategoryName,
        DistanceKm,
        MinimumAge,
        MaximumAge,
        EntryFee,
        Capacity,
        IsAvailable,
        CreatedAt
    )
    VALUES
        (@DurbanEventId, N'5 km Run', 5.00, 10, NULL, 120.00, 1800, 0, '2026-05-02T08:00:00'),
        (@DurbanEventId, N'10 km Run', 10.00, 15, NULL, 180.00, 2500, 0, '2026-05-02T08:05:00'),
        (@CapeEventId, N'40 km Cycle', 40.00, 16, NULL, 350.00, 1500, 1, '2026-09-02T09:00:00'),
        (@CapeEventId, N'80 km Cycle', 80.00, 18, NULL, 550.00, 1000, 1, '2026-09-02T09:05:00'),
        (@SowetoEventId, N'Under 20 5 km Walk', 5.00, NULL, 19, 80.00, 2000, 1, '2026-09-11T10:00:00'),
        (@SowetoEventId, N'10 km Walk', 10.00, 12, NULL, 120.00, 1200, 1, '2026-09-11T10:05:00');

    DECLARE @Durban5CategoryId INT =
    (
        SELECT EventCategoryId
        FROM dbo.EventCategories
        WHERE EventId = @DurbanEventId
          AND CategoryName = N'5 km Run'
    );

    DECLARE @Durban10CategoryId INT =
    (
        SELECT EventCategoryId
        FROM dbo.EventCategories
        WHERE EventId = @DurbanEventId
          AND CategoryName = N'10 km Run'
    );

    DECLARE @Cape40CategoryId INT =
    (
        SELECT EventCategoryId
        FROM dbo.EventCategories
        WHERE EventId = @CapeEventId
          AND CategoryName = N'40 km Cycle'
    );

    DECLARE @Cape80CategoryId INT =
    (
        SELECT EventCategoryId
        FROM dbo.EventCategories
        WHERE EventId = @CapeEventId
          AND CategoryName = N'80 km Cycle'
    );

    DECLARE @Soweto5CategoryId INT =
    (
        SELECT EventCategoryId
        FROM dbo.EventCategories
        WHERE EventId = @SowetoEventId
          AND CategoryName = N'Under 20 5 km Walk'
    );

    DECLARE @Soweto10CategoryId INT =
    (
        SELECT EventCategoryId
        FROM dbo.EventCategories
        WHERE EventId = @SowetoEventId
          AND CategoryName = N'10 km Walk'
    );

    INSERT INTO dbo.EventRoutes
    (
        EventCategoryId,
        RouteName,
        StartLocation,
        FinishLocation,
        RouteDescription,
        RouteMapUrl,
        ElevationGainMetres,
        CreatedAt
    )
    VALUES
        (
            @Durban5CategoryId,
            N'Stadium 5 km Loop',
            N'Moses Mabhida Stadium south entrance',
            N'Moses Mabhida Stadium south entrance',
            N'A flat out-and-back route along Masabalala Yengwa Avenue.',
            NULL,
            35,
            '2026-05-03T08:00:00'
        ),
        (
            @Durban10CategoryId,
            N'Stadium Coastal Loop',
            N'Moses Mabhida Stadium south entrance',
            N'Moses Mabhida Stadium south entrance',
            N'A coastal loop passing the beachfront before returning to the stadium.',
            NULL,
            85,
            '2026-05-03T08:05:00'
        ),
        (
            @Cape40CategoryId,
            N'Peninsula Short Route',
            N'Green Point Urban Park',
            N'Green Point Urban Park',
            N'A forty kilometre road route through the Atlantic Seaboard.',
            NULL,
            420,
            '2026-09-03T09:00:00'
        ),
        (
            @Cape80CategoryId,
            N'Peninsula Long Route',
            N'Green Point Urban Park',
            N'Green Point Urban Park',
            N'An eighty kilometre endurance route through the southern peninsula.',
            NULL,
            980,
            '2026-09-03T09:05:00'
        ),
        (
            @Soweto5CategoryId,
            N'Kliptown Heritage Route',
            N'Walter Sisulu Square',
            N'Walter Sisulu Square',
            N'A five kilometre community route through Kliptown.',
            NULL,
            45,
            '2026-09-12T10:00:00'
        ),
        (
            @Soweto10CategoryId,
            N'Soweto Heritage Route',
            N'Walter Sisulu Square',
            N'Walter Sisulu Square',
            N'A ten kilometre walking route connecting several heritage locations.',
            NULL,
            110,
            '2026-09-12T10:05:00'
        );

    DECLARE @NalediUserId INT =
    (
        SELECT UserId
        FROM dbo.Users
        WHERE Email = N'naledi.mokoena@example.com'
    );

    DECLARE @SiphoUserId INT =
    (
        SELECT UserId
        FROM dbo.Users
        WHERE Email = N'sipho.dlamini@example.com'
    );

    INSERT INTO dbo.Enrolments
    (
        ParticipantUserId,
        EventId,
        EventCategoryId,
        EnrolmentDate,
        Status,
        BibNumber,
        UpdatedAt
    )
    VALUES
        (
            @NalediUserId,
            @DurbanEventId,
            @Durban10CategoryId,
            '2026-06-20T09:15:00',
            N'Confirmed',
            N'D1001',
            NULL
        ),
        (
            @SiphoUserId,
            @DurbanEventId,
            @Durban5CategoryId,
            '2026-06-22T14:30:00',
            N'Confirmed',
            N'D0501',
            NULL
        ),
        (
            @NalediUserId,
            @CapeEventId,
            @Cape40CategoryId,
            '2026-09-18T11:20:00',
            N'Confirmed',
            N'C4001',
            NULL
        ),
        (
            @SiphoUserId,
            @SowetoEventId,
            @Soweto10CategoryId,
            '2026-09-16T15:45:00',
            N'Cancelled',
            NULL,
            '2026-09-18T08:10:00'
        );

    DECLARE @NalediDurbanEnrolmentId INT =
    (
        SELECT EnrolmentId
        FROM dbo.Enrolments
        WHERE ParticipantUserId = @NalediUserId
          AND EventId = @DurbanEventId
    );

    DECLARE @SiphoDurbanEnrolmentId INT =
    (
        SELECT EnrolmentId
        FROM dbo.Enrolments
        WHERE ParticipantUserId = @SiphoUserId
          AND EventId = @DurbanEventId
    );

    INSERT INTO dbo.Results
    (
        EnrolmentId,
        RecordedByUserId,
        ResultStatus,
        FinishTimeSeconds,
        OverallPosition,
        CategoryPosition,
        Notes,
        RecordedAt,
        UpdatedAt
    )
    VALUES
        (
            @NalediDurbanEnrolmentId,
            @ThaboUserId,
            N'Completed',
            2874,
            42,
            10,
            NULL,
            '2026-08-16T06:15:00',
            NULL
        ),
        (
            @SiphoDurbanEnrolmentId,
            @ThaboUserId,
            N'DidNotFinish',
            NULL,
            NULL,
            NULL,
            N'Participant withdrew at the three kilometre water point.',
            '2026-08-16T06:20:00',
            NULL
        );

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

PRINT N'RaceDay database schema and sample data created successfully.';
GO

SELECT N'Roles' AS EntityName, COUNT(*) AS RecordCount
FROM dbo.Roles
UNION ALL
SELECT N'Users', COUNT(*)
FROM dbo.Users
UNION ALL
SELECT N'Events', COUNT(*)
FROM dbo.Events
UNION ALL
SELECT N'EventCategories', COUNT(*)
FROM dbo.EventCategories
UNION ALL
SELECT N'EventRoutes', COUNT(*)
FROM dbo.EventRoutes
UNION ALL
SELECT N'Enrolments', COUNT(*)
FROM dbo.Enrolments
UNION ALL
SELECT N'Results', COUNT(*)
FROM dbo.Results;
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

    Microsoft (2026e) 'INSERT (Transact-SQL)', Microsoft Learn.
    Available at: https://learn.microsoft.com/sql/t-sql/statements/insert-transact-sql?view=sql-server-ver16
    (Accessed: 19 September 2026).

    Microsoft (2026f) 'PasswordHasher<TUser>.HashPassword(TUser, String) Method', Microsoft Learn.
    Available at: https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1.hashpassword?view=aspnetcore-10.0
    (Accessed: 19 September 2026).

    The Independent Institute of Education (2026) PROG6212 Portfolio of Evidence.
    Unpublished assessment brief.
*/
