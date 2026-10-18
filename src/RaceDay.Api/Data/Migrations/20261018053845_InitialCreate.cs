using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "dbo",
                columns: table => new
                {
                    RoleId = table.Column<byte>(type: "tinyint", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                    table.UniqueConstraint("UQ_Roles_RoleName", x => x.RoleName);
                    table.CheckConstraint("CK_Roles_RoleName", "RoleName IN (N'Organiser', N'Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<byte>(type: "tinyint", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(254)", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    ProfilePictureBlobName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Users_IsActive"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Users_CreatedAt"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                    table.UniqueConstraint("UQ_Users_Email", x => x.Email);
                    table.CheckConstraint("CK_Users_DateOfBirth", "DateOfBirth IS NULL OR DateOfBirth < CONVERT(DATE, SYSUTCDATETIME())");
                    table.CheckConstraint("CK_Users_Email_NotBlank", "LEN(LTRIM(RTRIM(Email))) > 0");
                    table.CheckConstraint("CK_Users_FirstName_NotBlank", "LEN(LTRIM(RTRIM(FirstName))) > 0");
                    table.CheckConstraint("CK_Users_LastName_NotBlank", "LEN(LTRIM(RTRIM(LastName))) > 0");
                    table.CheckConstraint("CK_Users_PasswordHash_NotBlank", "LEN(LTRIM(RTRIM(PasswordHash))) > 0");
                    table.CheckConstraint("CK_Users_ProfilePictureBlobName_NotBlank", "ProfilePictureBlobName IS NULL OR LEN(LTRIM(RTRIM(ProfilePictureBlobName))) > 0");
                    table.ForeignKey(
                        name: "FK_Users_Roles",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "Roles",
                        principalColumn: "RoleId");
                });

            migrationBuilder.CreateTable(
                name: "Events",
                schema: "dbo",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserUserId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BannerImageBlobName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    EventDateTime = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    EntryClosingDateTime = table.Column<DateTime>(type: "datetime2(0)", nullable: false),
                    VenueName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    City = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(9,6)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft")
                        .Annotation("Relational:DefaultConstraintName", "DF_Events_Status"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Events_CreatedAt"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventId);
                    table.CheckConstraint("CK_Events_AddressLine1_NotBlank", "LEN(LTRIM(RTRIM(AddressLine1))) > 0");
                    table.CheckConstraint("CK_Events_BannerImageBlobName_NotBlank", "BannerImageBlobName IS NULL OR LEN(LTRIM(RTRIM(BannerImageBlobName))) > 0");
                    table.CheckConstraint("CK_Events_City_NotBlank", "LEN(LTRIM(RTRIM(City))) > 0");
                    table.CheckConstraint("CK_Events_Description_NotBlank", "LEN(LTRIM(RTRIM(Description))) > 0");
                    table.CheckConstraint("CK_Events_DistanceKm", "DistanceKm > 0");
                    table.CheckConstraint("CK_Events_EntryClosingDateTime", "EntryClosingDateTime < EventDateTime");
                    table.CheckConstraint("CK_Events_EventName_NotBlank", "LEN(LTRIM(RTRIM(EventName))) > 0");
                    table.CheckConstraint("CK_Events_EventType", "EventType IN (N'Running', N'Walking', N'Cycling')");
                    table.CheckConstraint("CK_Events_Latitude", "Latitude IS NULL OR Latitude BETWEEN -90 AND 90");
                    table.CheckConstraint("CK_Events_Longitude", "Longitude IS NULL OR Longitude BETWEEN -180 AND 180");
                    table.CheckConstraint("CK_Events_Province", "Province IN ( N'Eastern Cape', N'Free State', N'Gauteng', N'KwaZulu-Natal', N'Limpopo', N'Mpumalanga', N'North West', N'Northern Cape', N'Western Cape' )");
                    table.CheckConstraint("CK_Events_Status", "Status IN (N'Draft', N'Open', N'Closed', N'Cancelled', N'Completed')");
                    table.CheckConstraint("CK_Events_VenueName_NotBlank", "LEN(LTRIM(RTRIM(VenueName))) > 0");
                    table.ForeignKey(
                        name: "FK_Events_Users",
                        column: x => x.OrganiserUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "EventCategories",
                schema: "dbo",
                columns: table => new
                {
                    EventCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(6,2)", nullable: false),
                    MinimumAge = table.Column<byte>(type: "tinyint", nullable: true),
                    MaximumAge = table.Column<byte>(type: "tinyint", nullable: true),
                    EntryFee = table.Column<decimal>(type: "decimal(10,2)", nullable: false, defaultValue: 0m)
                        .Annotation("Relational:DefaultConstraintName", "DF_EventCategories_EntryFee"),
                    Capacity = table.Column<int>(type: "int", nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_EventCategories_IsAvailable"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_EventCategories_CreatedAt"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCategories", x => x.EventCategoryId);
                    table.UniqueConstraint("UQ_EventCategories_Category_Event", x => new { x.EventCategoryId, x.EventId });
                    table.CheckConstraint("CK_EventCategories_AgeRange", "MinimumAge IS NULL OR MaximumAge IS NULL OR MinimumAge <= MaximumAge");
                    table.CheckConstraint("CK_EventCategories_Capacity", "Capacity IS NULL OR Capacity > 0");
                    table.CheckConstraint("CK_EventCategories_DistanceKm", "DistanceKm > 0");
                    table.CheckConstraint("CK_EventCategories_EntryFee", "EntryFee >= 0");
                    table.CheckConstraint("CK_EventCategories_MaximumAge", "MaximumAge IS NULL OR MaximumAge BETWEEN 1 AND 120");
                    table.CheckConstraint("CK_EventCategories_MinimumAge", "MinimumAge IS NULL OR MinimumAge BETWEEN 1 AND 120");
                    table.ForeignKey(
                        name: "FK_EventCategories_Events",
                        column: x => x.EventId,
                        principalSchema: "dbo",
                        principalTable: "Events",
                        principalColumn: "EventId");
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                schema: "dbo",
                columns: table => new
                {
                    EnrolmentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantUserId = table.Column<int>(type: "int", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    EventCategoryId = table.Column<int>(type: "int", nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Enrolments_EnrolmentDate"),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Confirmed")
                        .Annotation("Relational:DefaultConstraintName", "DF_Enrolments_Status"),
                    BibNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentId);
                    table.UniqueConstraint("UQ_Enrolments_Participant_Event", x => new { x.ParticipantUserId, x.EventId });
                    table.CheckConstraint("CK_Enrolments_Status", "Status IN (N'Confirmed', N'Cancelled')");
                    table.ForeignKey(
                        name: "FK_Enrolments_EventCategories",
                        columns: x => new { x.EventCategoryId, x.EventId },
                        principalSchema: "dbo",
                        principalTable: "EventCategories",
                        principalColumns: new[] { "EventCategoryId", "EventId" });
                    table.ForeignKey(
                        name: "FK_Enrolments_Events",
                        column: x => x.EventId,
                        principalSchema: "dbo",
                        principalTable: "Events",
                        principalColumn: "EventId");
                    table.ForeignKey(
                        name: "FK_Enrolments_Users",
                        column: x => x.ParticipantUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "EventRoutes",
                schema: "dbo",
                columns: table => new
                {
                    RouteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventCategoryId = table.Column<int>(type: "int", nullable: false),
                    RouteName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    StartLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FinishLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RouteDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RouteMapUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ElevationGainMetres = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_EventRoutes_CreatedAt"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventRoutes", x => x.RouteId);
                    table.UniqueConstraint("UQ_EventRoutes_EventCategoryId", x => x.EventCategoryId);
                    table.CheckConstraint("CK_EventRoutes_ElevationGainMetres", "ElevationGainMetres IS NULL OR ElevationGainMetres >= 0");
                    table.CheckConstraint("CK_EventRoutes_FinishLocation_NotBlank", "LEN(LTRIM(RTRIM(FinishLocation))) > 0");
                    table.CheckConstraint("CK_EventRoutes_RouteDescription_NotBlank", "LEN(LTRIM(RTRIM(RouteDescription))) > 0");
                    table.CheckConstraint("CK_EventRoutes_RouteName_NotBlank", "LEN(LTRIM(RTRIM(RouteName))) > 0");
                    table.CheckConstraint("CK_EventRoutes_StartLocation_NotBlank", "LEN(LTRIM(RTRIM(StartLocation))) > 0");
                    table.ForeignKey(
                        name: "FK_EventRoutes_EventCategories",
                        column: x => x.EventCategoryId,
                        principalSchema: "dbo",
                        principalTable: "EventCategories",
                        principalColumn: "EventCategoryId");
                });

            migrationBuilder.CreateTable(
                name: "Results",
                schema: "dbo",
                columns: table => new
                {
                    ResultId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentId = table.Column<int>(type: "int", nullable: false),
                    RecordedByUserId = table.Column<int>(type: "int", nullable: false),
                    ResultStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Completed")
                        .Annotation("Relational:DefaultConstraintName", "DF_Results_ResultStatus"),
                    FinishTimeSeconds = table.Column<int>(type: "int", nullable: true),
                    OverallPosition = table.Column<int>(type: "int", nullable: true),
                    CategoryPosition = table.Column<int>(type: "int", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                        .Annotation("Relational:DefaultConstraintName", "DF_Results_RecordedAt"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultId);
                    table.UniqueConstraint("UQ_Results_EnrolmentId", x => x.EnrolmentId);
                    table.CheckConstraint("CK_Results_CategoryPosition", "CategoryPosition IS NULL OR CategoryPosition > 0");
                    table.CheckConstraint("CK_Results_FinishTime", "(ResultStatus = N'Completed' AND FinishTimeSeconds IS NOT NULL AND FinishTimeSeconds > 0) OR (ResultStatus <> N'Completed' AND FinishTimeSeconds IS NULL)");
                    table.CheckConstraint("CK_Results_OverallPosition", "OverallPosition IS NULL OR OverallPosition > 0");
                    table.CheckConstraint("CK_Results_ResultStatus", "ResultStatus IN ( N'Completed', N'DidNotFinish', N'Disqualified', N'DidNotStart' )");
                    table.ForeignKey(
                        name: "FK_Results_Enrolments",
                        column: x => x.EnrolmentId,
                        principalSchema: "dbo",
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentId");
                    table.ForeignKey(
                        name: "FK_Results_Users",
                        column: x => x.RecordedByUserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Roles",
                columns: new[] { "RoleId", "RoleName" },
                values: new object[,]
                {
                    { (byte)1, "Organiser" },
                    { (byte)2, "Participant" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Users",
                columns: new[] { "UserId", "CreatedAt", "DateOfBirth", "Email", "FirstName", "IsActive", "LastName", "PasswordHash", "PhoneNumber", "ProfilePictureBlobName", "RoleId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 9, 19, 12, 0, 0, 0, DateTimeKind.Utc), null, "thabo.ndlovu@example.com", "Thabo", true, "Ndlovu", "AQAAAAIAAYagAAAAEMJEo34Vex/Es3ymj/KGDAsh0XTg/maiNgOcLa2boUyPUM5+RCqT/0h++cgbZz9QKQ==", "0825550101", null, (byte)1, null },
                    { 2, new DateTime(2026, 9, 19, 12, 5, 0, 0, DateTimeKind.Utc), null, "ayesha.khan@example.com", "Ayesha", true, "Khan", "AQAAAAIAAYagAAAAENQbJEgO1gVHjHJ5qFByozUY2cbU8ylUaFYisC4nx4Hg/xMev27k+Ge6phAOBOsxCA==", "0835550102", null, (byte)1, null },
                    { 3, new DateTime(2026, 9, 19, 12, 10, 0, 0, DateTimeKind.Utc), new DateOnly(1994, 6, 12), "naledi.mokoena@example.com", "Naledi", true, "Mokoena", "AQAAAAIAAYagAAAAEDGx3Kf8pdpEdcvJgX7WsWvSff/SlnpYHBOzinfgqgYepnz19uEvaNNth2Tp67JSKQ==", "0845550103", null, (byte)2, null },
                    { 4, new DateTime(2026, 9, 19, 12, 15, 0, 0, DateTimeKind.Utc), new DateOnly(1989, 11, 3), "sipho.dlamini@example.com", "Sipho", true, "Dlamini", "AQAAAAIAAYagAAAAEGKZ3ZVEHVCoL6bra3uVYqs9oPOEklCO8xFdN2QUbsquTGekE1T4L64zlVfYua6tuw==", "0715550104", null, (byte)2, null }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Events",
                columns: new[] { "EventId", "AddressLine1", "BannerImageBlobName", "City", "CreatedAt", "Description", "DistanceKm", "EntryClosingDateTime", "EventDateTime", "EventName", "EventType", "Latitude", "Longitude", "OrganiserUserId", "Province", "Status", "UpdatedAt", "VenueName" },
                values: new object[,]
                {
                    { 1, "44 Isaiah Ntshangase Road", null, "Durban", new DateTime(2026, 5, 1, 8, 0, 0, 0, DateTimeKind.Utc), "A coastal road race with five and ten kilometre categories.", 10.00m, new DateTime(2026, 8, 9, 21, 59, 59, 0, DateTimeKind.Utc), new DateTime(2026, 8, 16, 4, 30, 0, 0, DateTimeKind.Utc), "Durban Sunrise 10K", "Running", -29.829000m, 31.030300m, 1, "KwaZulu-Natal", "Completed", null, "Moses Mabhida Stadium" },
                    { 2, "1 Fritz Sonnenberg Road", null, "Cape Town", new DateTime(2026, 9, 1, 9, 0, 0, 0, DateTimeKind.Utc), "A supported road cycling event with forty and eighty kilometre routes.", 80.00m, new DateTime(2027, 3, 1, 21, 59, 59, 0, DateTimeKind.Utc), new DateTime(2027, 3, 14, 4, 0, 0, 0, DateTimeKind.Utc), "Cape Peninsula Cycle Challenge", "Cycling", -33.905800m, 18.411900m, 2, "Western Cape", "Open", null, "Green Point Urban Park" },
                    { 3, "1 Klipspruit Valley Road", null, "Soweto", new DateTime(2026, 9, 10, 10, 0, 0, 0, DateTimeKind.Utc), "A community walk visiting important heritage locations in Soweto.", 10.00m, new DateTime(2027, 4, 17, 21, 59, 59, 0, DateTimeKind.Utc), new DateTime(2027, 4, 24, 5, 30, 0, 0, DateTimeKind.Utc), "Soweto Heritage Walk", "Walking", -26.278400m, 27.858500m, 1, "Gauteng", "Open", null, "Walter Sisulu Square" }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "EventCategories",
                columns: new[] { "EventCategoryId", "Capacity", "CategoryName", "CreatedAt", "DistanceKm", "EntryFee", "EventId", "IsAvailable", "MaximumAge", "MinimumAge", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 1800, "5 km Run", new DateTime(2026, 5, 2, 8, 0, 0, 0, DateTimeKind.Utc), 5.00m, 120.00m, 1, false, null, (byte)10, null },
                    { 2, 2500, "10 km Run", new DateTime(2026, 5, 2, 8, 5, 0, 0, DateTimeKind.Utc), 10.00m, 180.00m, 1, false, null, (byte)15, null },
                    { 3, 1500, "40 km Cycle", new DateTime(2026, 9, 2, 9, 0, 0, 0, DateTimeKind.Utc), 40.00m, 350.00m, 2, true, null, (byte)16, null },
                    { 4, 1000, "80 km Cycle", new DateTime(2026, 9, 2, 9, 5, 0, 0, DateTimeKind.Utc), 80.00m, 550.00m, 2, true, null, (byte)18, null },
                    { 5, 2000, "Under 20 5 km Walk", new DateTime(2026, 9, 11, 10, 0, 0, 0, DateTimeKind.Utc), 5.00m, 80.00m, 3, true, (byte)19, null, null },
                    { 6, 1200, "10 km Walk", new DateTime(2026, 9, 11, 10, 5, 0, 0, DateTimeKind.Utc), 10.00m, 120.00m, 3, true, null, (byte)12, null }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Enrolments",
                columns: new[] { "EnrolmentId", "BibNumber", "EnrolmentDate", "EventCategoryId", "EventId", "ParticipantUserId", "Status", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "D1001", new DateTime(2026, 6, 20, 9, 15, 0, 0, DateTimeKind.Utc), 2, 1, 3, "Confirmed", null },
                    { 2, "D0501", new DateTime(2026, 6, 22, 14, 30, 0, 0, DateTimeKind.Utc), 1, 1, 4, "Confirmed", null },
                    { 3, "C4001", new DateTime(2026, 9, 18, 11, 20, 0, 0, DateTimeKind.Utc), 3, 2, 3, "Confirmed", null },
                    { 4, null, new DateTime(2026, 9, 16, 15, 45, 0, 0, DateTimeKind.Utc), 6, 3, 4, "Cancelled", new DateTime(2026, 9, 18, 8, 10, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "EventRoutes",
                columns: new[] { "RouteId", "CreatedAt", "ElevationGainMetres", "EventCategoryId", "FinishLocation", "RouteDescription", "RouteMapUrl", "RouteName", "StartLocation", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 5, 3, 8, 0, 0, 0, DateTimeKind.Utc), 35, 1, "Moses Mabhida Stadium south entrance", "A flat out-and-back route along Masabalala Yengwa Avenue.", null, "Stadium 5 km Loop", "Moses Mabhida Stadium south entrance", null },
                    { 2, new DateTime(2026, 5, 3, 8, 5, 0, 0, DateTimeKind.Utc), 85, 2, "Moses Mabhida Stadium south entrance", "A coastal loop passing the beachfront before returning to the stadium.", null, "Stadium Coastal Loop", "Moses Mabhida Stadium south entrance", null },
                    { 3, new DateTime(2026, 9, 3, 9, 0, 0, 0, DateTimeKind.Utc), 420, 3, "Green Point Urban Park", "A forty kilometre road route through the Atlantic Seaboard.", null, "Peninsula Short Route", "Green Point Urban Park", null },
                    { 4, new DateTime(2026, 9, 3, 9, 5, 0, 0, DateTimeKind.Utc), 980, 4, "Green Point Urban Park", "An eighty kilometre endurance route through the southern peninsula.", null, "Peninsula Long Route", "Green Point Urban Park", null },
                    { 5, new DateTime(2026, 9, 12, 10, 0, 0, 0, DateTimeKind.Utc), 45, 5, "Walter Sisulu Square", "A five kilometre community route through Kliptown.", null, "Kliptown Heritage Route", "Walter Sisulu Square", null },
                    { 6, new DateTime(2026, 9, 12, 10, 5, 0, 0, DateTimeKind.Utc), 110, 6, "Walter Sisulu Square", "A ten kilometre walking route connecting several heritage locations.", null, "Soweto Heritage Route", "Walter Sisulu Square", null }
                });

            migrationBuilder.InsertData(
                schema: "dbo",
                table: "Results",
                columns: new[] { "ResultId", "CategoryPosition", "EnrolmentId", "FinishTimeSeconds", "Notes", "OverallPosition", "RecordedAt", "RecordedByUserId", "ResultStatus", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, 10, 1, 2874, null, 42, new DateTime(2026, 8, 16, 6, 15, 0, 0, DateTimeKind.Utc), 1, "Completed", null },
                    { 2, null, 2, null, "Participant withdrew at the three kilometre water point.", null, new DateTime(2026, 8, 16, 6, 20, 0, 0, DateTimeKind.Utc), 1, "DidNotFinish", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_EventCategoryId",
                schema: "dbo",
                table: "Enrolments",
                column: "EventCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_EventId_Status",
                schema: "dbo",
                table: "Enrolments",
                columns: new[] { "EventId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UX_Enrolments_EventId_BibNumber",
                schema: "dbo",
                table: "Enrolments",
                columns: new[] { "EventId", "BibNumber" },
                unique: true,
                filter: "BibNumber IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_EventId",
                schema: "dbo",
                table: "EventCategories",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "UQ_EventCategories_Event_CategoryName",
                schema: "dbo",
                table: "EventCategories",
                columns: new[] { "EventId", "CategoryName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_EventDateTime_Status",
                schema: "dbo",
                table: "Events",
                columns: new[] { "EventDateTime", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserUserId",
                schema: "dbo",
                table: "Events",
                column: "OrganiserUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_RecordedByUserId",
                schema: "dbo",
                table: "Results",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Results_ResultStatus",
                schema: "dbo",
                table: "Results",
                column: "ResultStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                schema: "dbo",
                table: "Users",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventRoutes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Results",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Enrolments",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EventCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Events",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "dbo");
        }
    }
}
