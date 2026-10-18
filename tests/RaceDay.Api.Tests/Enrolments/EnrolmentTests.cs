using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Enrolments;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Authentication;
using RaceDay.Api.Tests.Database;
using Xunit;

namespace RaceDay.Api.Tests.Enrolments;

// HTTP, session, SQL Server and transaction checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// Accessed: 18 October 2026.
public sealed partial class EnrolmentTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>, IAsyncLifetime
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
        await context.Users.Where(x => x.UserId == 3).ExecuteUpdateAsync(x => x
            .SetProperty(u => u.DateOfBirth, new DateOnly(1994, 6, 12)).SetProperty(u => u.IsActive, true).SetProperty(u => u.RoleId, (byte)2));
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_AssignsTheParticipantAndReturnsAPersistedPrivateLocation()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        using var response = await client.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var entry = (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
        Assert.Equal(3, entry.ParticipantUserId);
        Assert.Equal(category.EventId, entry.EventId);
        Assert.Equal(category.EventCategoryId, entry.EventCategoryId);
        Assert.Equal("Confirmed", entry.Status);
        Assert.Equal("Naledi Mokoena", entry.ParticipantName);
        Assert.Equal("Community Run", entry.EventName);
        Assert.Equal("10 km Run", entry.CategoryName);
        Assert.Equal(10m, entry.CategoryDistanceKm);
        Assert.Equal(Now.UtcDateTime, entry.EnrolmentDate);
        Assert.Equal(DateTimeKind.Utc, entry.EnrolmentDate.Kind);
        Assert.Equal(DateTimeKind.Utc, entry.EventDateTime.Kind);
        Assert.Null(entry.BibNumber);
        Assert.Null(entry.UpdatedAt);
        Assert.Null(entry.Result);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal($"/api/enrolments/{entry.EnrolmentId}", response.Headers.Location?.OriginalString);
        using var read = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, read.StatusCode);
        using var anonymous = factory.Client();
        using var rejected = await anonymous.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.Unauthorized, rejected.StatusCode);
        await using var context = database.CreateContext();
        var saved = (await context.Enrolments.FindAsync(entry.EnrolmentId))!;
        Assert.Equal(3, saved.ParticipantUserId);
        Assert.Equal(category.EventId, saved.EventId);
        Assert.Equal(category.EventCategoryId, saved.EventCategoryId);
        Assert.Equal(Now.UtcDateTime, saved.EnrolmentDate);
        await AssertSafe(response);
    }

    [Fact]
    public async Task Lifecycle_CancelsReactivatesAndDeletesWithoutReplacingTheEntry()
    {
        using var factory = Factory();
        using var participant = factory.Client();
        using var organiser = factory.Client();
        await Login(participant);
        await Login(organiser, "thabo.ndlovu@example.com");
        var category = await AddCategory(capacity: 1);
        var otherCategory = await AddCategory(eventId: category.EventId, name: "Another category");
        using var created = await participant.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        var entry = (await created.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
        using var bib = await organiser.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/bib-number", new { bibNumber = " a1042 " });
        Assert.Equal(HttpStatusCode.OK, bib.StatusCode);
        Assert.Equal("A1042", (await bib.Content.ReadFromJsonAsync<EnrolmentResponse>())!.BibNumber);
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        using var cancelled = await participant.PutAsync($"/api/enrolments/{entry.EnrolmentId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        var history = (await cancelled.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
        Assert.Equal("Cancelled", history.Status);
        Assert.Equal("A1042", history.BibNumber);
        Assert.Equal(entry.EnrolmentDate, history.EnrolmentDate);
        Assert.Equal(Now.UtcDateTime.AddMinutes(1), history.UpdatedAt);
        using var cannotDelete = await participant.DeleteAsync($"/api/enrolments/{entry.EnrolmentId}");
        Assert.Equal(HttpStatusCode.Conflict, cannotDelete.StatusCode);
        using var same = await participant.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/reactivate", new { eventCategoryId = category.EventCategoryId });
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        Assert.Equal("A1042", (await same.Content.ReadFromJsonAsync<EnrolmentResponse>())!.BibNumber);
        using var cancelAgain = await participant.PutAsync($"/api/enrolments/{entry.EnrolmentId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelAgain.StatusCode);
        using var moved = await participant.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/reactivate", new { eventCategoryId = otherCategory.EventCategoryId });
        Assert.Equal(HttpStatusCode.OK, moved.StatusCode);
        var changed = (await moved.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
        Assert.Equal(entry.EnrolmentId, changed.EnrolmentId);
        Assert.Equal(entry.EnrolmentDate, changed.EnrolmentDate);
        Assert.Equal(otherCategory.EventCategoryId, changed.EventCategoryId);
        Assert.Equal("Confirmed", changed.Status);
        Assert.Null(changed.BibNumber);
        using var deleted = await participant.DeleteAsync($"/api/enrolments/{entry.EnrolmentId}");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsStringAsync());
        using var missing = await participant.GetAsync($"/api/enrolments/{entry.EnrolmentId}");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var context = database.CreateContext();
        Assert.Empty(await context.Enrolments.ToListAsync());
        Assert.Equal(1, await context.Events.CountAsync());
        Assert.Equal(2, await context.EventCategories.CountAsync());
    }

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("Cancelled")]
    public async Task Create_RejectsAnyExistingEntryForTheSameParticipantAndEvent(string status)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var first = await AddCategory();
        var second = await AddCategory(eventId: first.EventId, name: "5 km Run");
        var original = await AddEntry(first, status: status);
        using var response = await client.PostAsJsonAsync("/api/enrolments", EntryBody(second));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Enrolments.CountAsync());
        var row = (await context.Enrolments.FindAsync(original.EnrolmentId))!;
        Assert.Equal(first.EventCategoryId, row.EventCategoryId);
        Assert.Equal(status, row.Status);
        Assert.Null(row.UpdatedAt);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Closed")]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    [InlineData("closed now")]
    [InlineData("closed earlier")]
    [InlineData("unavailable")]
    public async Task EntryAndReactivation_RejectClosedOrUnavailableOptions(string reason)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory(status: reason is "Draft" or "Closed" or "Cancelled" or "Completed" ? reason : "Open",
            available: reason != "unavailable");
        if (reason.StartsWith("closed ", StringComparison.Ordinal))
        {
            await using var edit = database.CreateContext();
            var item = (await edit.Events.FindAsync(category.EventId))!;
            item.EntryClosingDateTime = reason == "closed now" ? Now.UtcDateTime : Now.UtcDateTime.AddSeconds(-1);
            await edit.SaveChangesAsync();
        }
        using var create = await client.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        Assert.Equal(HttpStatusCode.Conflict, create.StatusCode);
        var entry = await AddEntry(category, status: "Cancelled");
        using var reactivate = await client.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/reactivate", new { eventCategoryId = category.EventCategoryId });
        Assert.Equal(HttpStatusCode.Conflict, reactivate.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal("Cancelled", (await context.Enrolments.FindAsync(entry.EnrolmentId))!.Status);
        Assert.Equal(1, await context.Enrolments.CountAsync());
    }

    [Theory]
    [InlineData("1994-06-12", 36, 36, "2030-06-12", true)]
    [InlineData("1994-06-12", 36, null, "2030-06-11", false)]
    [InlineData("1994-06-12", null, 35, "2030-06-12", false)]
    [InlineData("1994-06-12", null, 35, "2030-06-11", true)]
    [InlineData("1994-06-12", 35, null, "2030-06-11", true)]
    [InlineData(null, 18, null, "2030-06-12", false)]
    [InlineData(null, null, 60, "2030-06-12", false)]
    [InlineData(null, null, null, "2030-06-12", true)]
    [InlineData("2000-02-29", 30, 30, "2030-02-28", false)]
    [InlineData("2000-02-29", 30, 30, "2030-03-01", true)]
    [InlineData("2000-02-29", 32, 32, "2032-02-28", false)]
    [InlineData("2000-02-29", 32, 32, "2032-02-29", true)]
    public async Task Age_UsesInclusiveCompletedYearsOnTheUtcEventDateForEntryAndReactivation(
        string? birth, int? minimum, int? maximum, string eventDate, bool allowed)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory(minimum: minimum, maximum: maximum);
        await using (var edit = database.CreateContext())
        {
            (await edit.Users.FindAsync(3))!.DateOfBirth = birth is null ? null : DateOnly.Parse(birth, System.Globalization.CultureInfo.InvariantCulture);
            (await edit.Events.FindAsync(category.EventId))!.EventDateTime = DateTime.Parse(eventDate + "T00:01:00Z", null, System.Globalization.DateTimeStyles.AdjustToUniversal);
            await edit.SaveChangesAsync();
        }
        using var created = await client.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        Assert.Equal(allowed ? HttpStatusCode.Created : HttpStatusCode.BadRequest, created.StatusCode);
        await using (var edit = database.CreateContext()) { await edit.Enrolments.ExecuteDeleteAsync(); }
        var cancelled = await AddEntry(category, status: "Cancelled");
        using var reactivated = await client.PutAsJsonAsync($"/api/enrolments/{cancelled.EnrolmentId}/reactivate", new { eventCategoryId = category.EventCategoryId });
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.BadRequest, reactivated.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal(allowed ? "Confirmed" : "Cancelled", (await context.Enrolments.FindAsync(cancelled.EnrolmentId))!.Status);
    }

    [Theory]
    [InlineData("Confirmed", 1, false)]
    [InlineData("Cancelled", 1, true)]
    [InlineData("Confirmed", 2, true)]
    [InlineData("Confirmed", null, true)]
    public async Task Capacity_CountsConfirmedEntriesAndTreatsNullAsUnlimited(string otherStatus, int? capacity, bool allowed)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory(capacity: capacity);
        await AddEntry(category, participant: 4, status: otherStatus);
        using var response = await client.PostAsJsonAsync("/api/enrolments", EntryBody(category));
        Assert.Equal(allowed ? HttpStatusCode.Created : HttpStatusCode.Conflict, response.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal(allowed ? 2 : 1, await context.Enrolments.CountAsync());
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Draft")]
    [InlineData("Closed")]
    [InlineData("event Cancelled")]
    [InlineData("Completed")]
    [InlineData("closed now")]
    [InlineData("result")]
    public async Task CancellationAndDeletion_ProtectHistoryAndClosedEntries(string reason)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory(status: reason is "Draft" or "Closed" or "Completed" ? reason : reason == "event Cancelled" ? "Cancelled" : "Open");
        var entry = await AddEntry(category, status: reason == "Cancelled" ? "Cancelled" : "Confirmed", bib: "A100");
        if (reason == "result") await AddResult(entry);
        if (reason == "closed now")
        {
            await using var edit = database.CreateContext();
            (await edit.Events.FindAsync(category.EventId))!.EntryClosingDateTime = Now.UtcDateTime;
            await edit.SaveChangesAsync();
        }
        using var cancelled = await client.PutAsync($"/api/enrolments/{entry.EnrolmentId}/cancel", null);
        using var deleted = await client.DeleteAsync($"/api/enrolments/{entry.EnrolmentId}");
        Assert.Equal(HttpStatusCode.Conflict, cancelled.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        await using var context = database.CreateContext();
        var saved = (await context.Enrolments.FindAsync(entry.EnrolmentId))!;
        Assert.Equal(entry.Status, saved.Status);
        Assert.Equal("A100", saved.BibNumber);
        Assert.Null(saved.UpdatedAt);
    }

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("result")]
    [InlineData("full")]
    [InlineData("wrong event")]
    public async Task Reactivation_RejectsConflictsWithoutMovingOrReplacingTheEntry(string reason)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory(capacity: 1);
        var entry = await AddEntry(category, status: reason == "Confirmed" ? "Confirmed" : "Cancelled", bib: "A100");
        if (reason == "result") await AddResult(entry);
        if (reason == "full") await AddEntry(category, participant: 4);
        int requestedId = reason == "wrong event" ? (await AddCategory()).EventCategoryId : category.EventCategoryId;
        using var response = await client.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/reactivate", new { eventCategoryId = requestedId });
        Assert.Equal(reason == "wrong event" ? HttpStatusCode.NotFound : HttpStatusCode.Conflict, response.StatusCode);
        await using var context = database.CreateContext();
        var saved = (await context.Enrolments.FindAsync(entry.EnrolmentId))!;
        Assert.Equal(category.EventCategoryId, saved.EventCategoryId);
        Assert.Equal(entry.Status, saved.Status);
        Assert.Equal("A100", saved.BibNumber);
        Assert.Equal(entry.EnrolmentDate, saved.EnrolmentDate);
        Assert.Null(saved.UpdatedAt);
    }

    private AuthenticationApiFactory Factory()
    {
        var factory = new AuthenticationApiFactory(database.ConnectionString);
        factory.Clock.Advance(Now - factory.Clock.GetUtcNow());
        return factory;
    }

    private static async Task Login(HttpClient client, string email = "naledi.mokoena@example.com")
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password = "RaceDayDemo!2026" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static object EntryBody(EventCategory category) => new { eventId = category.EventId, eventCategoryId = category.EventCategoryId };

    private async Task<EventCategory> AddCategory(string status = "Open", bool available = true, int? capacity = null,
        int? minimum = null, int? maximum = null, int owner = 1, int? eventId = null, string name = "10 km Run")
    {
        await using var context = database.CreateContext();
        var category = new EventCategory
        {
            CategoryName = name, DistanceKm = 10, EntryFee = 150, IsAvailable = available, Capacity = capacity,
            MinimumAge = (byte?)minimum, MaximumAge = (byte?)maximum, CreatedAt = Now.UtcDateTime.AddDays(-10)
        };
        if (eventId.HasValue) category.EventId = eventId.Value;
        else category.Event = new Event
        {
            OrganiserUserId = owner, EventName = "Community Run", EventType = "Running", Description = "A community event.",
            DistanceKm = 10, EventDateTime = Now.UtcDateTime.AddDays(10), EntryClosingDateTime = Now.UtcDateTime.AddDays(3),
            VenueName = "Main Stadium", AddressLine1 = "1 Main Road", City = "Johannesburg", Province = "Gauteng",
            Status = status, CreatedAt = Now.UtcDateTime.AddDays(-15)
        };
        context.EventCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private async Task<Enrolment> AddEntry(EventCategory category, int participant = 3, string status = "Confirmed", string? bib = null, int daysAgo = 2)
    {
        var entry = new Enrolment
        {
            ParticipantUserId = participant, EventId = category.EventId, EventCategoryId = category.EventCategoryId,
            Status = status, BibNumber = bib, EnrolmentDate = Now.UtcDateTime.AddDays(-daysAgo)
        };
        await using var context = database.CreateContext();
        context.Enrolments.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    private async Task AddResult(Enrolment entry)
    {
        await using var context = database.CreateContext();
        context.Results.Add(new Result
        {
            EnrolmentId = entry.EnrolmentId, RecordedByUserId = 1, ResultStatus = "Completed", FinishTimeSeconds = 3600,
            OverallPosition = 1, CategoryPosition = 1, RecordedAt = Now.UtcDateTime
        });
        await context.SaveChangesAsync();
    }

    private static async Task AssertSafe(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        foreach (string field in new[] { "password", "passwordHash", "email", "phoneNumber", "dateOfBirth", "profilePictureBlobName", "roleId" })
            Assert.DoesNotContain("\"" + field + "\"", json, StringComparison.OrdinalIgnoreCase);
    }
}
