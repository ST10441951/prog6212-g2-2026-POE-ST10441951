using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Enrolments;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Contracts.Results;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Authentication;
using RaceDay.Api.Tests.Database;
using Xunit;

namespace RaceDay.Api.Tests.Results;

// Real HTTP, sessions and isolated SQL Server checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database
// Accessed: 18 October 2026.
public sealed partial class ResultTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await using var context = database.CreateContext();
        await context.Results.ExecuteDeleteAsync();
        await context.Enrolments.ExecuteDeleteAsync();
        await context.EventRoutes.ExecuteDeleteAsync();
        await context.EventCategories.ExecuteDeleteAsync();
        await context.Events.ExecuteDeleteAsync();
        await context.Users.Where(x => x.UserId > 4).ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_AssignsTheRecorderAndReturnsAStoredResultAndReadableLocation()
    {
        using var factory = Factory();
        using var organiser = factory.Client();
        await Login(organiser);
        var entry = await AddEntry();
        using var response = await organiser.PostAsJsonAsync(Path(entry), Details());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(Path(entry), response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var result = (await response.Content.ReadFromJsonAsync<ResultResponse>())!;
        Assert.Equal(entry.EnrolmentId, result.EnrolmentId);
        Assert.Equal(1, result.RecordedByUserId);
        Assert.Equal(3, result.ParticipantUserId);
        Assert.Equal("Naledi Mokoena", result.ParticipantName);
        Assert.Equal("Community Run", result.EventName);
        Assert.Equal(entry.EventId, result.EventId);
        Assert.Equal(entry.EventCategoryId, result.EventCategoryId);
        Assert.Equal("10 km Run", result.CategoryName);
        Assert.Equal(10m, result.CategoryDistanceKm);
        Assert.Equal("Completed", result.ResultStatus);
        Assert.Equal(3600, result.FinishTimeSeconds);
        Assert.Equal(1, result.TotalFinishers);
        Assert.Equal("Official result.", result.Notes);
        Assert.Equal(Now.UtcDateTime, result.RecordedAt);
        Assert.Equal(DateTimeKind.Utc, result.RecordedAt.Kind);
        Assert.Equal(DateTimeKind.Utc, result.EventDateTime.Kind);
        Assert.Null(result.UpdatedAt);
        using var followed = await organiser.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, followed.StatusCode);
        await Login(organiser, "naledi.mokoena@example.com");
        var own = (await organiser.GetFromJsonAsync<ResultResponse>(Path(entry)))!;
        Assert.Equal(result.ResultId, own.ResultId);
        var summary = (await organiser.GetFromJsonAsync<EnrolmentResponse>($"/api/enrolments/{entry.EnrolmentId}"))!;
        Assert.Equal(result.ResultId, summary.Result!.ResultId);
        Assert.Equal(3600, summary.Result.FinishTimeSeconds);
        await using var context = database.CreateContext();
        var saved = (await context.Results.FindAsync(result.ResultId))!;
        Assert.Equal(1, saved.RecordedByUserId);
        Assert.Equal(entry.EnrolmentId, saved.EnrolmentId);
        Assert.Equal(Now.UtcDateTime, saved.RecordedAt);
        await AssertSafe(response);
    }

    [Fact]
    public async Task Correction_PreservesTheOriginalIdentityRecorderAndRecordingTime()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry();
        var original = await AddResult(entry, recorder: 2);
        var request = Details();
        request["finishTimeSeconds"] = 3500;
        request["overallPosition"] = 2;
        request.Remove("categoryPosition");
        request.Remove("notes");
        using var response = await client.PutAsJsonAsync(Path(entry), request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var corrected = (await response.Content.ReadFromJsonAsync<ResultResponse>())!;
        Assert.Equal(original.ResultId, corrected.ResultId);
        Assert.Equal(entry.EnrolmentId, corrected.EnrolmentId);
        Assert.Equal(2, corrected.RecordedByUserId);
        Assert.Equal(original.RecordedAt, corrected.RecordedAt);
        Assert.Equal(Now.UtcDateTime, corrected.UpdatedAt);
        Assert.Equal(DateTimeKind.Utc, corrected.UpdatedAt!.Value.Kind);
        Assert.Equal(3500, corrected.FinishTimeSeconds);
        Assert.Equal(2, corrected.OverallPosition);
        Assert.Null(corrected.CategoryPosition);
        Assert.Null(corrected.Notes);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Results.CountAsync());
        Assert.Equal("Confirmed", (await context.Enrolments.FindAsync(entry.EnrolmentId))!.Status);
    }

    [Theory]
    [InlineData("Completed", " completed ")]
    [InlineData("DidNotFinish", " DIDNOTFINISH ")]
    [InlineData("Disqualified", "disqualified")]
    [InlineData("DidNotStart", "didnotstart")]
    public async Task Statuses_AreCanonicalAndAllowTheCorrectTimeFields(string canonical, string supplied)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry();
        var request = Details(canonical);
        request["resultStatus"] = supplied;
        request["notes"] = new string('a', 500);
        using var response = await client.PostAsJsonAsync(Path(entry), request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ResultResponse>())!;
        Assert.Equal(canonical, result.ResultStatus);
        Assert.Equal(canonical == "Completed" ? 3600 : (int?)null, result.FinishTimeSeconds);
        Assert.Equal(canonical == "Completed" ? 1 : 0, result.TotalFinishers);
        Assert.Equal(500, result.Notes!.Length);
    }

    [Theory]
    [InlineData("Completed", "DidNotFinish")]
    [InlineData("Completed", "Disqualified")]
    [InlineData("Completed", "DidNotStart")]
    [InlineData("DidNotFinish", "Completed")]
    [InlineData("Disqualified", "Completed")]
    [InlineData("DidNotStart", "Completed")]
    public async Task StatusCorrections_KeepTheSameResultAndUpdateFinisherTotals(string initial, string updated)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry();
        var original = await AddResult(entry, initial);
        using var response = await client.PutAsJsonAsync(Path(entry), Details(updated));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<ResultResponse>())!;
        Assert.Equal(original.ResultId, result.ResultId);
        Assert.Equal(original.RecordedAt, result.RecordedAt);
        Assert.Equal(updated, result.ResultStatus);
        Assert.Equal(updated == "Completed" ? 1 : 0, result.TotalFinishers);
        Assert.Equal(updated == "Completed" ? 3600 : (int?)null, result.FinishTimeSeconds);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Results.CountAsync());
    }

    [Theory]
    [InlineData("resultStatus", null)]
    [InlineData("resultStatus", "")]
    [InlineData("resultStatus", "  ")]
    [InlineData("resultStatus", "Pending")]
    [InlineData("finishTimeSeconds", null)]
    [InlineData("finishTimeSeconds", 0)]
    [InlineData("finishTimeSeconds", -1)]
    [InlineData("finishTimeSeconds", 1.5)]
    [InlineData("finishTimeSeconds", 2147483648L)]
    [InlineData("overallPosition", 0)]
    [InlineData("overallPosition", -1)]
    [InlineData("overallPosition", 1.5)]
    [InlineData("overallPosition", 2147483648L)]
    [InlineData("categoryPosition", 0)]
    [InlineData("categoryPosition", -1)]
    [InlineData("categoryPosition", 1.5)]
    [InlineData("categoryPosition", 2147483648L)]
    public async Task Writes_RejectMissingOrInvalidValues(string field, object? value)
    {
        var request = Details();
        if (value is null) request.Remove(field); else request[field] = value;
        await AssertInvalid(request);
    }

    [Theory]
    [InlineData("DidNotFinish", "finishTimeSeconds")]
    [InlineData("Disqualified", "finishTimeSeconds")]
    [InlineData("DidNotStart", "finishTimeSeconds")]
    [InlineData("DidNotFinish", "overallPosition")]
    [InlineData("Disqualified", "overallPosition")]
    [InlineData("DidNotStart", "overallPosition")]
    [InlineData("DidNotFinish", "categoryPosition")]
    [InlineData("Disqualified", "categoryPosition")]
    [InlineData("DidNotStart", "categoryPosition")]
    public async Task NonFinishers_CannotReceiveOfficialTimesOrPositions(string status, string field)
    {
        var request = Details(status);
        request[field] = 1;
        await AssertInvalid(request);
    }

    [Fact]
    public async Task Writes_RejectNotesLongerThanTheSqlColumn()
    {
        var request = Details();
        request["notes"] = new string('a', 501);
        await AssertInvalid(request);
    }

    [Theory]
    [InlineData("resultId")]
    [InlineData("enrolmentId")]
    [InlineData("recordedByUserId")]
    [InlineData("participantUserId")]
    [InlineData("eventId")]
    [InlineData("eventCategoryId")]
    [InlineData("totalFinishers")]
    [InlineData("recordedAt")]
    [InlineData("updatedAt")]
    public async Task Writes_RejectServerAssignedAndUnexpectedProperties(string field)
    {
        var request = Details();
        request[field] = 99;
        await AssertInvalid(request);
    }

    [Fact]
    public async Task DuplicateCreation_CannotReplaceTheOriginalResult()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry();
        var original = await AddResult(entry);
        using var duplicate = await client.PostAsJsonAsync(Path(entry), Details("DidNotFinish"));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        await using var context = database.CreateContext();
        var saved = Assert.Single(await context.Results.ToListAsync());
        Assert.Equal(original.ResultId, saved.ResultId);
        Assert.Equal("Completed", saved.ResultStatus);
        Assert.Null(saved.UpdatedAt);
    }

    [Theory]
    [InlineData("Cancelled entry")]
    [InlineData("Cancelled event")]
    [InlineData("future event")]
    public async Task Writes_RejectIneligibleEntriesAndEvents(string reason)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry(eventStatus: reason == "Cancelled event" ? "Cancelled" : "Open",
            entryStatus: reason == "Cancelled entry" ? "Cancelled" : "Confirmed");
        if (reason == "future event")
        {
            await using var edit = database.CreateContext();
            (await edit.Events.FindAsync(entry.EventId))!.EventDateTime = Now.UtcDateTime.AddSeconds(1);
            await edit.SaveChangesAsync();
        }
        using var created = await client.PostAsJsonAsync(Path(entry), Details());
        Assert.Equal(HttpStatusCode.Conflict, created.StatusCode);
        var original = await AddResult(entry);
        using var updated = await client.PutAsJsonAsync(Path(entry), Details("DidNotFinish"));
        Assert.Equal(HttpStatusCode.Conflict, updated.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal("Completed", (await context.Results.FindAsync(original.ResultId))!.ResultStatus);
    }

    [Fact]
    public async Task EventStartBoundary_AllowsResultCaptureAtTheStoredStartTime()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry(eventStatus: "Open");
        await using (var edit = database.CreateContext())
        {
            (await edit.Events.FindAsync(entry.EventId))!.EventDateTime = Now.UtcDateTime;
            await edit.SaveChangesAsync();
        }
        var request = Details();
        request["finishTimeSeconds"] = int.MaxValue;
        request["overallPosition"] = int.MaxValue;
        request["categoryPosition"] = int.MaxValue;
        request["notes"] = "  ";
        using var created = await client.PostAsJsonAsync(Path(entry), request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var result = (await created.Content.ReadFromJsonAsync<ResultResponse>())!;
        Assert.Null(result.Notes);
        Assert.Equal(int.MaxValue, result.FinishTimeSeconds);
        Assert.Equal(1, result.TotalFinishers);
    }

    private async Task AssertInvalid(Dictionary<string, object?> request)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry();
        var original = await AddResult(entry);
        using var create = await client.PostAsJsonAsync(Path(entry), request);
        using var update = await client.PutAsJsonAsync(Path(entry), request);
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Results.CountAsync());
        var saved = (await context.Results.FindAsync(original.ResultId))!;
        Assert.Equal("Completed", saved.ResultStatus);
        Assert.Equal(3600, saved.FinishTimeSeconds);
        Assert.Null(saved.UpdatedAt);
    }

    private AuthenticationApiFactory Factory()
    {
        var factory = new AuthenticationApiFactory(database.ConnectionString);
        factory.Clock.Advance(Now - factory.Clock.GetUtcNow());
        return factory;
    }

    private static async Task Login(HttpClient client, string email = "thabo.ndlovu@example.com")
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "RaceDayDemo!2026" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static string Path(Enrolment entry) => $"/api/enrolments/{entry.EnrolmentId}/result";

    private static Dictionary<string, object?> Details(string status = "Completed") => new()
    {
        ["resultStatus"] = status, ["finishTimeSeconds"] = status == "Completed" ? 3600 : (int?)null,
        ["overallPosition"] = status == "Completed" ? 1 : (int?)null,
        ["categoryPosition"] = status == "Completed" ? 1 : (int?)null, ["notes"] = " Official result. "
    };

    private async Task<Enrolment> AddEntry(int participant = 3, int owner = 1, string eventStatus = "Completed",
        string entryStatus = "Confirmed", EventCategory? category = null)
    {
        category ??= new EventCategory
        {
            CategoryName = "10 km Run", DistanceKm = 10, EntryFee = 150, IsAvailable = true, CreatedAt = Now.UtcDateTime.AddDays(-20),
            Event = new Event
            {
                OrganiserUserId = owner, EventName = "Community Run", EventType = "Running", Description = "A community event.",
                DistanceKm = 10, EventDateTime = Now.UtcDateTime.AddDays(-2), EntryClosingDateTime = Now.UtcDateTime.AddDays(-3),
                VenueName = "Main Stadium", AddressLine1 = "1 Main Road", City = "Johannesburg", Province = "Gauteng",
                Status = eventStatus, CreatedAt = Now.UtcDateTime.AddDays(-30)
            }
        };
        await using var context = database.CreateContext();
        if (category.EventCategoryId == 0)
        {
            context.EventCategories.Add(category);
            await context.SaveChangesAsync();
        }
        var entry = new Enrolment
        {
            ParticipantUserId = participant, Status = entryStatus, EnrolmentDate = Now.UtcDateTime.AddDays(-10),
            EventCategoryId = category.EventCategoryId, EventId = category.EventId
        };
        context.Enrolments.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    private async Task<Result> AddResult(Enrolment entry, string status = "Completed", int recorder = 1,
        int? position = 1, int seconds = 3600)
    {
        var result = new Result
        {
            EnrolmentId = entry.EnrolmentId, RecordedByUserId = recorder, ResultStatus = status,
            FinishTimeSeconds = status == "Completed" ? seconds : null,
            OverallPosition = status == "Completed" ? position : null, CategoryPosition = status == "Completed" ? position : null,
            Notes = "Original result.", RecordedAt = Now.UtcDateTime.AddHours(-1)
        };
        await using var context = database.CreateContext();
        context.Results.Add(result);
        await context.SaveChangesAsync();
        return result;
    }

    private async Task<int> AddParticipant()
    {
        await using var context = database.CreateContext();
        var user = new User
        {
            RoleId = 2, FirstName = "Test", LastName = "Runner", Email = Guid.NewGuid().ToString("N") + "@example.com",
            PasswordHash = await context.Users.Where(x => x.UserId == 3).Select(x => x.PasswordHash).SingleAsync(),
            IsActive = true, CreatedAt = Now.UtcDateTime.AddDays(-30)
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user.UserId;
    }

    private static async Task AssertSafe(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        foreach (string field in new[] { "password", "passwordHash", "email", "phoneNumber", "dateOfBirth", "profilePictureBlobName", "roleId" })
            Assert.DoesNotContain("\"" + field + "\"", json, StringComparison.OrdinalIgnoreCase);
    }
}
