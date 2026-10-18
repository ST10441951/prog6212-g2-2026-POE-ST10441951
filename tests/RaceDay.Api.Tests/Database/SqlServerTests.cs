using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests.Database;

// SQL Server tests and rollback isolation: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// Accessed: 18 October 2026.
public sealed class SqlServerTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private static readonly string Part1Sql =
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Planning", "RaceDay.sql"));

    [Fact]
    public async Task Migration_CreatesAll71PlannedColumns()
    {
        var expected = new List<string>();
        foreach (Match table in TableDefinitions())
        {
            foreach (Match column in Regex.Matches(table.Groups[2].Value,
                @"^\s*(\w+)\s+(TINYINT|INT|NVARCHAR\(\d+\)|BIT|DATE|DATETIME2\(0\)|DECIMAL\(\d+,\d+\))\s+(IDENTITY\(1,1\)\s+)?(NOT NULL|NULL)",
                RegexOptions.Multiline))
            {
                expected.Add($"{table.Groups[1].Value}.{column.Groups[1].Value}|" +
                    $"{column.Groups[2].Value.ToLowerInvariant()}|" +
                    $"{(column.Groups[4].Value == "NULL" ? 1 : 0)}|{(column.Groups[3].Success ? 1 : 0)}");
            }
        }

        string[] actual = await ReadRows("""
            SELECT t.name + '.' + c.name + '|' +
                TYPE_NAME(c.user_type_id) +
                CASE WHEN TYPE_NAME(c.user_type_id) = 'nvarchar'
                    THEN '(' + CAST(c.max_length / 2 AS varchar) + ')'
                     WHEN TYPE_NAME(c.user_type_id) = 'decimal'
                    THEN '(' + CAST(c.precision AS varchar) + ',' + CAST(c.scale AS varchar) + ')'
                     WHEN TYPE_NAME(c.user_type_id) = 'datetime2'
                    THEN '(' + CAST(c.scale AS varchar) + ')' ELSE '' END + '|' +
                CAST(c.is_nullable AS varchar) + '|' + CAST(c.is_identity AS varchar)
            FROM sys.tables t JOIN sys.columns c ON t.object_id = c.object_id
            WHERE t.name <> '__EFMigrationsHistory'
            """);

        Assert.Equal(71, expected.Count);
        Assert.Equal(expected.Order(), actual.Order());
    }

    [Fact]
    public async Task Migration_PreservesNineForeignKeysAndNoAction()
    {
        var expected = new List<string>();
        foreach (Match table in TableDefinitions())
        {
            foreach (Match key in Regex.Matches(table.Groups[2].Value,
                @"CONSTRAINT (FK_\w+)\s+FOREIGN KEY \(([^)]+)\)\s+REFERENCES dbo\.(\w+) \(([^)]+)\)"))
            {
                expected.Add($"{key.Groups[1].Value}|{table.Groups[1].Value}|" +
                    $"{RemoveSpaces(key.Groups[2].Value)}|{key.Groups[3].Value}|" +
                    $"{RemoveSpaces(key.Groups[4].Value)}|0|0");
            }
        }

        string[] actual = await ReadRows("""
            SELECT fk.name + '|' + OBJECT_NAME(fk.parent_object_id) + '|' +
                STRING_AGG(CAST(pc.name AS nvarchar(max)), ',')
                    WITHIN GROUP (ORDER BY fkc.constraint_column_id) + '|' +
                OBJECT_NAME(fk.referenced_object_id) + '|' +
                STRING_AGG(CAST(rc.name AS nvarchar(max)), ',')
                    WITHIN GROUP (ORDER BY fkc.constraint_column_id) + '|' +
                CAST(fk.delete_referential_action AS varchar) + '|' +
                CAST(fk.update_referential_action AS varchar)
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
            JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
            GROUP BY fk.name, fk.parent_object_id, fk.referenced_object_id,
                fk.delete_referential_action, fk.update_referential_action
            """);

        Assert.Equal(9, expected.Count);
        Assert.Equal(expected.Order(), actual.Order());
    }

    [Fact]
    public async Task Migration_PreservesUniqueColumnsAndAllPlannedIndexes()
    {
        var expected = new List<string>();
        foreach (Match table in TableDefinitions())
        {
            foreach (Match key in Regex.Matches(table.Groups[2].Value,
                @"CONSTRAINT (UQ_\w+)\s+UNIQUE \(([^)]+)\)"))
                expected.Add($"{table.Groups[1].Value}|{key.Groups[1].Value}|1|{RemoveSpaces(key.Groups[2].Value)}|");
        }
        foreach (Match index in Regex.Matches(Part1Sql,
            @"CREATE (UNIQUE )?NONCLUSTERED INDEX (\w+)\s+ON dbo\.(\w+) \(([^)]+)\)(?:\s+WHERE ([^;]+))?;"))
        {
            expected.Add($"{index.Groups[3].Value}|{index.Groups[2].Value}|" +
                $"{(index.Groups[1].Success ? 1 : 0)}|{RemoveSpaces(index.Groups[4].Value)}|" +
                $"{NormaliseExpression(index.Groups[5].Value)}");
        }

        string[] actual = await ReadRows("""
            SELECT t.name + '|' + i.name + '|' + CAST(i.is_unique AS varchar) + '|' +
                STRING_AGG(CAST(c.name AS nvarchar(max)), ',')
                    WITHIN GROUP (ORDER BY ic.key_ordinal) + '|' + COALESCE(i.filter_definition, '')
            FROM sys.tables t JOIN sys.indexes i ON i.object_id = t.object_id
            JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.is_primary_key = 0 AND t.name <> '__EFMigrationsHistory' AND ic.key_ordinal > 0
            GROUP BY t.name, i.name, i.is_unique, i.filter_definition
            """);
        actual = actual.Select(row =>
        {
            string[] fields = row.Split('|');
            fields[4] = NormaliseExpression(fields[4]);
            return string.Join('|', fields);
        }).ToArray();

        Assert.Equal(16, expected.Count);
        Assert.Equal(expected.Order(), actual.Order());
    }

    [Fact]
    public async Task Migration_PreservesNamedChecksDefaultsAndPrimaryKeys()
    {
        string[] expectedChecks = Regex.Matches(Part1Sql, @"CONSTRAINT (CK_\w+)")
            .Select(match => match.Groups[1].Value).Order().ToArray();
        string[] expectedDefaults = Regex.Matches(Part1Sql, @"CONSTRAINT (DF_\w+)")
            .Select(match => match.Groups[1].Value).Order().ToArray();

        Assert.Equal(36, expectedChecks.Length);
        Assert.Equal(expectedChecks, (await ReadRows("SELECT name FROM sys.check_constraints")).Order());
        Assert.Equal(expectedDefaults, (await ReadRows("SELECT name FROM sys.default_constraints")).Order());

        string[] expectedDefaultValues = Regex.Matches(Part1Sql,
            @"CONSTRAINT (DF_\w+) DEFAULT \((.*?)\)(?=,|\r?\n|$)")
            .Select(match => match.Groups[1].Value + "|" + NormaliseDefault(match.Groups[2].Value)).Order().ToArray();
        string[] actualDefaultValues = (await ReadRows("SELECT name + '|' + definition FROM sys.default_constraints"))
            .Select(row => row.Split('|')[0] + "|" + NormaliseDefault(row.Split('|')[1])).Order().ToArray();
        Assert.Equal(12, expectedDefaultValues.Length);
        Assert.Equal(expectedDefaultValues, actualDefaultValues);

        string[] expectedKeys = Regex.Matches(Part1Sql, @"CONSTRAINT (PK_\w+)\s+PRIMARY KEY")
            .Select(match => match.Groups[1].Value).Order().ToArray();
        Assert.Equal(7, expectedKeys.Length);
        Assert.Equal(expectedKeys, (await ReadRows(
            "SELECT name FROM sys.key_constraints WHERE type = 'PK' AND name <> 'PK___EFMigrationsHistory'")).Order());
    }

    [Fact]
    public async Task SeedData_CanBeReadWithAllRelatedRecords()
    {
        await using var context = database.CreateContext();
        Assert.Equal(2, await context.Roles.CountAsync());
        Assert.Equal(4, await context.Users.CountAsync());
        Assert.Equal(3, await context.Events.CountAsync());
        Assert.Equal(6, await context.EventCategories.CountAsync());
        Assert.Equal(6, await context.EventRoutes.CountAsync());
        Assert.Equal(4, await context.Enrolments.CountAsync());
        Assert.Equal(2, await context.Results.CountAsync());

        var entry = await context.Enrolments.AsNoTracking()
            .Include(x => x.Participant).ThenInclude(x => x.Role)
            .Include(x => x.Event).ThenInclude(x => x.Organiser)
            .Include(x => x.Category).ThenInclude(x => x.Route)
            .Include(x => x.Result).ThenInclude(x => x!.RecordedBy)
            .SingleAsync(x => x.EnrolmentId == 1);

        Assert.Equal("Participant", entry.Participant.Role.RoleName);
        Assert.Equal("naledi.mokoena@example.com", entry.Participant.Email);
        Assert.Equal(new DateOnly(1994, 6, 12), entry.Participant.DateOfBirth);
        Assert.Equal("Durban Sunrise 10K", entry.Event.EventName);
        Assert.Equal(10m, entry.Category.DistanceKm);
        Assert.Equal("Stadium Coastal Loop", entry.Category.Route?.RouteName);
        Assert.Equal(entry.EventId, entry.Category.EventId);
        Assert.Equal(2874, entry.Result?.FinishTimeSeconds);
        Assert.Equal(entry.Event.OrganiserUserId, entry.Result?.RecordedBy.UserId);
        Assert.False((await context.EventCategories.FindAsync(1))!.IsAvailable);
    }

    [Fact]
    public async Task ApplyingMigrationAgain_DoesNotDuplicateOrResetSeedData()
    {
        await using var context = database.CreateContext();
        await context.Database.ExecuteSqlRawAsync("UPDATE dbo.Users SET FirstName = N'Changed' WHERE UserId = 3");
        try
        {
            await context.Database.MigrateAsync();
            Assert.Single(await context.Database.GetAppliedMigrationsAsync());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            Assert.False(context.Database.HasPendingModelChanges());
            Assert.Equal(4, await context.Users.CountAsync());
            Assert.Equal("Changed", (await context.Users.FindAsync(3))!.FirstName);
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync("UPDATE dbo.Users SET FirstName = N'Naledi' WHERE UserId = 3");
        }
    }

    [Fact]
    public async Task SeedAccounts_UseTheExistingHashedDemonstrationPassword()
    {
        // Password verification: Microsoft (2026).
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1.verifyhashedpassword
        await using var context = database.CreateContext();
        var hasher = new PasswordHasher<User>();
        foreach (var user in await context.Users.AsNoTracking().ToListAsync())
        {
            Assert.NotEqual("RaceDayDemo!2026", user.PasswordHash);
            Assert.NotEqual(PasswordVerificationResult.Failed,
                hasher.VerifyHashedPassword(user, user.PasswordHash, "RaceDayDemo!2026"));
        }
    }

    [Fact]
    public async Task NewRecordAndCategoryRename_PersistThroughNewContexts()
    {
        int userId = 0;
        try
        {
            await using (var write = database.CreateContext())
            {
                var user = new User
                {
                    RoleId = 2, FirstName = "Test", LastName = "Participant",
                    Email = "database-test@example.com", IsActive = false,
                    PasswordHash = (await write.Users.AsNoTracking().FirstAsync()).PasswordHash
                };
                write.Users.Add(user);
                (await write.EventCategories.FindAsync(3))!.CategoryName = "Updated 40 km Cycle";
                await write.SaveChangesAsync();
                userId = user.UserId;
            }

            await using var read = database.CreateContext();
            var saved = await read.Users.AsNoTracking().SingleAsync(x => x.UserId == userId);
            Assert.True(userId > 4);
            Assert.False(saved.IsActive);
            Assert.InRange(saved.CreatedAt, DateTime.UtcNow.AddMinutes(-2), DateTime.UtcNow.AddMinutes(2));
            Assert.Equal("Updated 40 km Cycle", (await read.EventCategories.FindAsync(3))!.CategoryName);
        }
        finally
        {
            await using var cleanup = database.CreateContext();
            await cleanup.Users.Where(x => x.UserId == userId).ExecuteDeleteAsync();
            await cleanup.Database.ExecuteSqlRawAsync(
                "UPDATE dbo.EventCategories SET CategoryName = N'40 km Cycle' WHERE EventCategoryId = 3");
        }
    }

    [Fact]
    public async Task Api_ResolvesConfiguredContextAndReadsSqlServer()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:RaceDay"] = database.ConnectionString }));
        });
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal(3, await context.Events.CountAsync());
    }

    [Theory]
    [InlineData("UPDATE dbo.Roles SET RoleName = N'Volunteer' WHERE RoleId = 1", "CK_Roles_RoleName")]
    [InlineData("UPDATE dbo.Users SET FirstName = N' ' WHERE UserId = 3", "CK_Users_FirstName_NotBlank")]
    [InlineData("UPDATE dbo.Users SET LastName = N'' WHERE UserId = 3", "CK_Users_LastName_NotBlank")]
    [InlineData("UPDATE dbo.Users SET Email = N'' WHERE UserId = 3", "CK_Users_Email_NotBlank")]
    [InlineData("UPDATE dbo.Users SET PasswordHash = N'' WHERE UserId = 3", "CK_Users_PasswordHash_NotBlank")]
    [InlineData("UPDATE dbo.Users SET ProfilePictureBlobName = N' ' WHERE UserId = 3", "CK_Users_ProfilePictureBlobName_NotBlank")]
    [InlineData("UPDATE dbo.Users SET DateOfBirth = CONVERT(date, SYSUTCDATETIME()) WHERE UserId = 3", "CK_Users_DateOfBirth")]
    [InlineData("UPDATE dbo.Events SET EventName = N'' WHERE EventId = 1", "CK_Events_EventName_NotBlank")]
    [InlineData("UPDATE dbo.Events SET EventType = N'Swimming' WHERE EventId = 1", "CK_Events_EventType")]
    [InlineData("UPDATE dbo.Events SET Description = N'' WHERE EventId = 1", "CK_Events_Description_NotBlank")]
    [InlineData("UPDATE dbo.Events SET BannerImageBlobName = N' ' WHERE EventId = 1", "CK_Events_BannerImageBlobName_NotBlank")]
    [InlineData("UPDATE dbo.Events SET DistanceKm = 0 WHERE EventId = 1", "CK_Events_DistanceKm")]
    [InlineData("UPDATE dbo.Events SET EntryClosingDateTime = EventDateTime WHERE EventId = 1", "CK_Events_EntryClosingDateTime")]
    [InlineData("UPDATE dbo.Events SET VenueName = N'' WHERE EventId = 1", "CK_Events_VenueName_NotBlank")]
    [InlineData("UPDATE dbo.Events SET AddressLine1 = N'' WHERE EventId = 1", "CK_Events_AddressLine1_NotBlank")]
    [InlineData("UPDATE dbo.Events SET City = N'' WHERE EventId = 1", "CK_Events_City_NotBlank")]
    [InlineData("UPDATE dbo.Events SET Province = N'Invalid' WHERE EventId = 1", "CK_Events_Province")]
    [InlineData("UPDATE dbo.Events SET Latitude = 91 WHERE EventId = 1", "CK_Events_Latitude")]
    [InlineData("UPDATE dbo.Events SET Longitude = -181 WHERE EventId = 1", "CK_Events_Longitude")]
    [InlineData("UPDATE dbo.Events SET Status = N'Invalid' WHERE EventId = 1", "CK_Events_Status")]
    [InlineData("UPDATE dbo.EventCategories SET DistanceKm = 0 WHERE EventCategoryId = 1", "CK_EventCategories_DistanceKm")]
    [InlineData("UPDATE dbo.EventCategories SET MinimumAge = 0 WHERE EventCategoryId = 1", "CK_EventCategories_MinimumAge")]
    [InlineData("UPDATE dbo.EventCategories SET MaximumAge = 121 WHERE EventCategoryId = 1", "CK_EventCategories_MaximumAge")]
    [InlineData("UPDATE dbo.EventCategories SET MinimumAge = 20, MaximumAge = 19 WHERE EventCategoryId = 1", "CK_EventCategories_AgeRange")]
    [InlineData("UPDATE dbo.EventCategories SET EntryFee = -1 WHERE EventCategoryId = 1", "CK_EventCategories_EntryFee")]
    [InlineData("UPDATE dbo.EventCategories SET Capacity = 0 WHERE EventCategoryId = 1", "CK_EventCategories_Capacity")]
    [InlineData("UPDATE dbo.EventRoutes SET RouteName = N'' WHERE RouteId = 1", "CK_EventRoutes_RouteName_NotBlank")]
    [InlineData("UPDATE dbo.EventRoutes SET StartLocation = N'' WHERE RouteId = 1", "CK_EventRoutes_StartLocation_NotBlank")]
    [InlineData("UPDATE dbo.EventRoutes SET FinishLocation = N'' WHERE RouteId = 1", "CK_EventRoutes_FinishLocation_NotBlank")]
    [InlineData("UPDATE dbo.EventRoutes SET RouteDescription = N'' WHERE RouteId = 1", "CK_EventRoutes_RouteDescription_NotBlank")]
    [InlineData("UPDATE dbo.EventRoutes SET ElevationGainMetres = -1 WHERE RouteId = 1", "CK_EventRoutes_ElevationGainMetres")]
    [InlineData("UPDATE dbo.Enrolments SET Status = N'Invalid' WHERE EnrolmentId = 1", "CK_Enrolments_Status")]
    [InlineData("UPDATE dbo.Results SET ResultStatus = N'Invalid' WHERE ResultId = 2", "CK_Results_ResultStatus")]
    [InlineData("UPDATE dbo.Results SET FinishTimeSeconds = NULL WHERE ResultId = 1", "CK_Results_FinishTime")]
    [InlineData("UPDATE dbo.Results SET OverallPosition = 0 WHERE ResultId = 1", "CK_Results_OverallPosition")]
    [InlineData("UPDATE dbo.Results SET CategoryPosition = -1 WHERE ResultId = 1", "CK_Results_CategoryPosition")]
    public async Task SqlServer_RejectsEachInvalidCheckValue(string sql, string constraint)
    {
        await ExpectConstraintFailure(sql, constraint, 547);
    }

    [Theory]
    [InlineData("UPDATE dbo.Users SET Email = N'NALEDI.MOKOENA@EXAMPLE.COM' WHERE UserId = 4", "UQ_Users_Email")]
    [InlineData("UPDATE dbo.EventCategories SET CategoryName = N'5 km Run' WHERE EventCategoryId = 2", "UQ_EventCategories_Event_CategoryName")]
    [InlineData("UPDATE dbo.EventRoutes SET EventCategoryId = 1 WHERE RouteId = 2", "UQ_EventRoutes_EventCategoryId")]
    [InlineData("UPDATE dbo.Enrolments SET ParticipantUserId = 3, EventId = 2, EventCategoryId = 3 WHERE EnrolmentId = 4", "UQ_Enrolments_Participant_Event")]
    [InlineData("UPDATE dbo.Results SET EnrolmentId = 1 WHERE ResultId = 2", "UQ_Results_EnrolmentId")]
    [InlineData("UPDATE dbo.Enrolments SET BibNumber = N'D1001' WHERE EnrolmentId = 2", "UX_Enrolments_EventId_BibNumber")]
    public async Task SqlServer_RejectsDuplicateValues(string sql, string constraint)
    {
        await ExpectConstraintFailure(sql, constraint, 2601, 2627);
    }

    [Fact]
    public async Task SqlServer_RejectsCategoryFromAnotherEvent()
    {
        await ExpectConstraintFailure(
            "UPDATE dbo.Enrolments SET EventCategoryId = 3 WHERE EnrolmentId = 1",
            "FK_Enrolments_EventCategories", 547);
    }

    [Fact]
    public async Task SqlServer_ProtectsRecordsWithDependentHistory()
    {
        await ExpectConstraintFailure(
            "DELETE FROM dbo.Users WHERE UserId = 2", "FK_Events_Users", 547);
        await ExpectConstraintFailure(
            "DELETE FROM dbo.Enrolments WHERE EnrolmentId = 1", "FK_Results_Enrolments", 547);
    }

    [Fact]
    public async Task BibNumbers_AllowNullsAndTheSameNumberInDifferentEvents()
    {
        await using var context = database.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE dbo.Enrolments SET BibNumber = NULL WHERE EventId = 1");
        Assert.Equal(2, await context.Enrolments.CountAsync(x => x.EventId == 1 && x.BibNumber == null));
        await context.Database.ExecuteSqlRawAsync(
            "UPDATE dbo.Enrolments SET BibNumber = N'SHARED' WHERE EnrolmentId IN (1, 3)");
        Assert.Equal(2, await context.Enrolments.CountAsync(x => x.BibNumber == "SHARED"));
    }

    [Theory]
    [InlineData("Completed", 1)]
    [InlineData("DidNotFinish", null)]
    [InlineData("Disqualified", null)]
    [InlineData("DidNotStart", null)]
    public async Task Results_AcceptValidStatusAndTime(string status, int? seconds)
    {
        await using var context = database.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var result = (await context.Results.FindAsync(2))!;
        result.ResultStatus = status;
        result.FinishTimeSeconds = seconds;
        await context.SaveChangesAsync();
    }

    [Theory]
    [InlineData("Completed", 0)]
    [InlineData("Completed", -1)]
    [InlineData("DidNotFinish", 1)]
    [InlineData("Disqualified", 1)]
    [InlineData("DidNotStart", 1)]
    public async Task Results_RejectIncompatibleStatusAndTime(string status, int seconds)
    {
        await using var context = database.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var error = await Assert.ThrowsAsync<SqlException>(async () =>
            await context.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE dbo.Results SET ResultStatus = {status}, FinishTimeSeconds = {seconds} WHERE ResultId = 2"));
        Assert.Equal(547, error.Number);
        Assert.Contains("CK_Results_FinishTime", error.Message);
    }

    private async Task ExpectConstraintFailure(string sql, string constraint, params int[] errorNumbers)
    {
        await using var context = database.CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var error = await Assert.ThrowsAsync<SqlException>(async () =>
            await context.Database.ExecuteSqlRawAsync(sql));
        Assert.Contains(error.Number, errorNumbers);
        Assert.Contains(constraint, error.Message);
    }

    private async Task<string[]> ReadRows(string sql)
    {
        await using var context = database.CreateContext();
        await context.Database.OpenConnectionAsync();
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
            rows.Add(reader.GetString(0));
        return rows.ToArray();
    }

    private static MatchCollection TableDefinitions() =>
        Regex.Matches(Part1Sql, @"CREATE TABLE dbo\.(\w+)\s*\((.*?)\r?\n    \);", RegexOptions.Singleline);

    private static string RemoveSpaces(string value) => Regex.Replace(value, @"\s+", "");

    private static string NormaliseExpression(string value) => Regex.Replace(value, @"[\s\[\]()]", "");

    private static string NormaliseDefault(string value) => NormaliseExpression(value)
        .ToLowerInvariant().Replace("convertbit,1", "1").Replace("0.0", "0");
}
