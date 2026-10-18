using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Authentication;
using RaceDay.Api.Tests.Database;
using Xunit;

namespace RaceDay.Api.Tests.Events;

// Real middleware, HTTP requests and SQL Server checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// Accessed: 18 October 2026.
public sealed class OrganiserEventTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>, IAsyncLifetime
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
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Create_AssignsTheOwnerAndDraftStatusAndReturnsAReadableLocation()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var request = Details();
        request["eventDateTime"] = "2030-01-10T08:00:00+02:00";
        request["entryClosingDateTime"] = "2030-01-03T08:00:00+02:00";
        using var response = await client.PostAsJsonAsync("/api/events", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = (await response.Content.ReadFromJsonAsync<EventDetailsResponse>())!;
        Assert.Equal("Draft", created.Status);
        Assert.Equal("Community Run", created.EventName);
        Assert.Equal("A community event.", created.Description);
        Assert.Equal("Main Stadium", created.VenueName);
        Assert.Equal("1 Main Road", created.AddressLine1);
        Assert.Equal("Johannesburg", created.City);
        Assert.Equal(new DateTime(2030, 1, 10, 6, 0, 0, DateTimeKind.Utc), created.EventDateTime);
        Assert.Equal(DateTimeKind.Utc, created.EventDateTime.Kind);
        Assert.Equal($"/api/events/{created.EventId}", response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.CacheControl?.NoStore);
        using var fetched = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        using var anonymous = factory.Client();
        using var hidden = await anonymous.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        await using var read = database.CreateContext();
        var saved = (await read.Events.FindAsync(created.EventId))!;
        Assert.Equal(1, saved.OrganiserUserId);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAt);
        Assert.Null(saved.UpdatedAt);
        Assert.Null(saved.BannerImageBlobName);
        Assert.Equal(10m, saved.DistanceKm);
        Assert.Equal(-26.2041m, saved.Latitude);
        Assert.Equal(28.0473m, saved.Longitude);
        await AssertSafe(response);
    }

    [Fact]
    public async Task EventLifecycle_SavesEditsOpensClosesReopensCancelsAndKeepsEntries()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        using var created = await client.PostAsJsonAsync("/api/events", Details());
        int id = (await created.Content.ReadFromJsonAsync<EventDetailsResponse>())!.EventId;
        var update = Details("Draft");
        update["eventName"] = "Corrected Run";
        using var changed = await client.PutAsJsonAsync($"/api/events/{id}", update);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var category = await AddCategory(id);
        foreach (string status in new[] { "Open", "Closed", "Open" })
        {
            update["status"] = status;
            using var response = await client.PutAsJsonAsync($"/api/events/{id}", update);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(status, (await response.Content.ReadFromJsonAsync<EventDetailsResponse>())!.Status);
        }
        await AddEntry(id, category.EventCategoryId, 3, "Confirmed");
        await AddEntry(id, category.EventCategoryId, 4, "Cancelled");
        update["status"] = "Cancelled";
        using var cancelled = await client.PutAsJsonAsync($"/api/events/{id}", update);
        Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        await using var read = database.CreateContext();
        var saved = (await read.Events.FindAsync(id))!;
        Assert.Equal("Cancelled", saved.Status);
        Assert.Equal("Corrected Run", saved.EventName);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAt);
        Assert.Equal(Now.UtcDateTime, saved.UpdatedAt);
        Assert.Equal(2, await read.Enrolments.CountAsync(x => x.EventId == id));
        Assert.Equal(1, await read.EventCategories.CountAsync(x => x.EventId == id));
        Assert.Equal(new[] { "Cancelled", "Confirmed" },
            await read.Enrolments.Where(x => x.EventId == id).OrderBy(x => x.Status).Select(x => x.Status).ToArrayAsync());
    }

    [Theory]
    [InlineData("GET", "/api/events/mine")]
    [InlineData("GET", "/api/organiser/dashboard")]
    [InlineData("POST", "/api/events")]
    [InlineData("PUT", "/api/events/1")]
    [InlineData("DELETE", "/api/events/1")]
    public async Task EveryOrganiserEndpoint_RejectsMissingAndParticipantSessions(string method, string route)
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var missing = await Send(client, method, route);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        await Login(client, "naledi.mokoena@example.com");
        using var wrongRole = await Send(client, method, route);
        Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
        Assert.Null(wrongRole.Headers.Location);
        await Login(client);
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var expired = await Send(client, method, route);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Writes_RejectOtherOwnersAndMissingRecordsWithoutChangingSql(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var other = await AddEvent(owner: 2);
        await Login(client);
        using var forbidden = await Send(client, method, $"/api/events/{other.EventId}");
        using var missing = await Send(client, method, "/api/events/2147483647");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var read = database.CreateContext();
        var saved = (await read.Events.FindAsync(other.EventId))!;
        Assert.Equal(2, saved.OrganiserUserId);
        Assert.Equal("Original Event", saved.EventName);
        Assert.Null(saved.UpdatedAt);
    }

    [Theory]
    [InlineData("eventName")]
    [InlineData("eventType")]
    [InlineData("description")]
    [InlineData("distanceKm")]
    [InlineData("eventDateTime")]
    [InlineData("entryClosingDateTime")]
    [InlineData("venueName")]
    [InlineData("addressLine1")]
    [InlineData("city")]
    [InlineData("province")]
    public async Task Writes_RequireEveryPlannedField(string field)
    {
        var request = Details();
        request.Remove(field);
        await AssertInvalidWrites(request);
    }

    [Theory]
    [InlineData("eventName", "")]
    [InlineData("eventName", "   ")]
    [InlineData("description", null)]
    [InlineData("venueName", "   ")]
    [InlineData("addressLine1", "")]
    [InlineData("city", "   ")]
    [InlineData("eventType", "Swimming")]
    [InlineData("province", "Atlantis")]
    [InlineData("distanceKm", 0)]
    [InlineData("distanceKm", -1)]
    [InlineData("distanceKm", 10000)]
    [InlineData("distanceKm", 0.001)]
    [InlineData("distanceKm", 10.123)]
    [InlineData("latitude", 91)]
    [InlineData("longitude", -181)]
    [InlineData("latitude", -26.1234567)]
    [InlineData("longitude", 28.1234567)]
    [InlineData("latitude", null)]
    [InlineData("eventDateTime", "not-a-date")]
    [InlineData("eventDateTime", "2030-01-10T06:00:00.1Z")]
    [InlineData("entryClosingDateTime", "2030-01-03T06:00:00.1Z")]
    [InlineData("entryClosingDateTime", "2030-01-10T06:00:00Z")]
    [InlineData("entryClosingDateTime", "2030-01-11T06:00:00Z")]
    public async Task Writes_RejectInvalidFieldsAndSqlPrecision(string field, object? value)
    {
        var request = Details();
        request[field] = value;
        await AssertInvalidWrites(request);
    }

    [Theory]
    [InlineData("eventName", 121)]
    [InlineData("description", 1001)]
    [InlineData("venueName", 121)]
    [InlineData("addressLine1", 121)]
    [InlineData("city", 81)]
    public async Task Writes_RejectOverlongText(string field, int length)
    {
        var request = Details();
        request[field] = new string('x', length);
        await AssertInvalidWrites(request);
    }

    [Theory]
    [InlineData("eventId", 1)]
    [InlineData("organiserUserId", 2)]
    [InlineData("createdAt", "2030-01-01T00:00:00Z")]
    [InlineData("updatedAt", "2030-01-01T00:00:00Z")]
    [InlineData("bannerImageBlobName", "injected/banner.jpg")]
    [InlineData("role", "Organiser")]
    public async Task Writes_RejectServerOwnedFields(string field, object value)
    {
        var request = Details();
        request[field] = value;
        await AssertInvalidWrites(request);
    }

    [Fact]
    public async Task Create_RejectsPastDatesAndAClientSelectedStatus()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var request = Details();
        request["eventDateTime"] = Now.UtcDateTime;
        request["entryClosingDateTime"] = Now.UtcDateTime.AddDays(-1);
        using var past = await client.PostAsJsonAsync("/api/events", request);
        Assert.Equal(HttpStatusCode.BadRequest, past.StatusCode);
        request = Details("Open");
        using var assignedStatus = await client.PostAsJsonAsync("/api/events", request);
        Assert.Equal(HttpStatusCode.BadRequest, assignedStatus.StatusCode);
        await using var read = database.CreateContext();
        Assert.Empty(await read.Events.ToListAsync());
    }

    [Fact]
    public async Task Create_AcceptsSqlBoundariesAndOptionalCoordinates()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var request = Details();
        request["distanceKm"] = 9999.99m;
        request["latitude"] = -90m;
        request["longitude"] = 180m;
        request["eventName"] = new string('a', 120);
        request["description"] = new string('b', 1000);
        request["venueName"] = new string('c', 120);
        request["addressLine1"] = new string('d', 120);
        request["city"] = new string('e', 80);
        using var boundary = await client.PostAsJsonAsync("/api/events", request);
        Assert.Equal(HttpStatusCode.Created, boundary.StatusCode);
        request.Remove("latitude");
        request.Remove("longitude");
        request["distanceKm"] = 0.01m;
        using var optional = await client.PostAsJsonAsync("/api/events", request);
        Assert.Equal(HttpStatusCode.Created, optional.StatusCode);
        var saved = (await optional.Content.ReadFromJsonAsync<EventDetailsResponse>())!;
        Assert.Null(saved.Latitude);
        Assert.Null(saved.Longitude);
    }

    [Theory]
    [InlineData("Draft", "Draft", true)]
    [InlineData("Draft", "Open", true)]
    [InlineData("Draft", "Closed", false)]
    [InlineData("Draft", "Cancelled", true)]
    [InlineData("Draft", "Completed", false)]
    [InlineData("Open", "Draft", false)]
    [InlineData("Open", "Open", true)]
    [InlineData("Open", "Closed", true)]
    [InlineData("Open", "Cancelled", true)]
    [InlineData("Open", "Completed", true)]
    [InlineData("Closed", "Draft", false)]
    [InlineData("Closed", "Open", true)]
    [InlineData("Closed", "Closed", true)]
    [InlineData("Closed", "Cancelled", true)]
    [InlineData("Closed", "Completed", true)]
    [InlineData("Cancelled", "Draft", false)]
    [InlineData("Cancelled", "Open", false)]
    [InlineData("Cancelled", "Closed", false)]
    [InlineData("Cancelled", "Cancelled", true)]
    [InlineData("Cancelled", "Completed", false)]
    [InlineData("Completed", "Draft", false)]
    [InlineData("Completed", "Open", false)]
    [InlineData("Completed", "Closed", false)]
    [InlineData("Completed", "Cancelled", false)]
    [InlineData("Completed", "Completed", true)]
    public async Task Update_EnforcesEveryStatusTransition(string current, string target, bool allowed)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        bool past = current == "Completed" || (target == "Completed" && current is "Open" or "Closed");
        var item = await AddEvent(current, past: past);
        await AddCategory(item.EventId);
        var request = From(item, target);
        using var response = await client.PutAsJsonAsync($"/api/events/{item.EventId}", request);
        Assert.Equal(allowed ? HttpStatusCode.OK : HttpStatusCode.Conflict, response.StatusCode);
        await using var read = database.CreateContext();
        var saved = (await read.Events.FindAsync(item.EventId))!;
        Assert.Equal(allowed ? target : current, saved.Status);
        if (!allowed) Assert.Null(saved.UpdatedAt);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("unavailable")]
    [InlineData("closingPassed")]
    [InlineData("startPassed")]
    [InlineData("futureCompletion")]
    public async Task Update_RejectsOpeningAndCompletionWhenTheEventIsNotReady(string reason)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent(reason == "futureCompletion" ? "Open" : "Draft", past: reason == "startPassed");
        if (reason == "closingPassed")
        {
            await using var edit = database.CreateContext();
            item.EntryClosingDateTime = Now.UtcDateTime.AddDays(-1);
            await edit.Events.Where(x => x.EventId == item.EventId).ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.EntryClosingDateTime, item.EntryClosingDateTime));
        }
        if (reason != "none") await AddCategory(item.EventId, reason != "unavailable");
        using var response = await client.PutAsJsonAsync($"/api/events/{item.EventId}",
            From(item, reason == "futureCompletion" ? "Completed" : "Open"));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("Confirmed", "eventDateTime")]
    [InlineData("Confirmed", "entryClosingDateTime")]
    [InlineData("Confirmed", "eventType")]
    [InlineData("Confirmed", "distanceKm")]
    [InlineData("Cancelled", "eventDateTime")]
    [InlineData("Cancelled", "entryClosingDateTime")]
    [InlineData("Cancelled", "eventType")]
    [InlineData("Cancelled", "distanceKm")]
    public async Task Update_ProtectsScheduleTypeAndDistanceForEverySavedEntry(string entryStatus, string field)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var category = await AddCategory(item.EventId);
        await AddEntry(item.EventId, category.EventCategoryId, 3, entryStatus);
        var request = From(item, "Draft");
        request[field] = field switch
        {
            "eventDateTime" => item.EventDateTime.AddDays(1),
            "entryClosingDateTime" => item.EntryClosingDateTime.AddDays(1),
            "eventType" => "Walking", _ => 11m
        };
        using var response = await client.PutAsJsonAsync($"/api/events/{item.EventId}", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var read = database.CreateContext();
        var saved = (await read.Events.FindAsync(item.EventId))!;
        Assert.Equal(item.EventDateTime, saved.EventDateTime);
        Assert.Equal(item.EntryClosingDateTime, saved.EntryClosingDateTime);
        Assert.Equal(item.EventType, saved.EventType);
        Assert.Equal(item.DistanceKm, saved.DistanceKm);
        Assert.Null(saved.UpdatedAt);
    }

    [Fact]
    public async Task Update_ProtectsResultsAndTerminalHistoryButAllowsTextCorrections()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent("Closed", past: true);
        var category = await AddCategory(item.EventId);
        var entry = await AddEntry(item.EventId, category.EventCategoryId, 3);
        await using (var context = database.CreateContext())
        {
            context.Results.Add(new Result
            {
                EnrolmentId = entry.EnrolmentId, RecordedByUserId = 1, ResultStatus = "Completed",
                FinishTimeSeconds = 3600, RecordedAt = Now.UtcDateTime
            });
            await context.SaveChangesAsync();
        }
        using var cancel = await client.PutAsJsonAsync($"/api/events/{item.EventId}", From(item, "Cancelled"));
        Assert.Equal(HttpStatusCode.Conflict, cancel.StatusCode);
        using var complete = await client.PutAsJsonAsync($"/api/events/{item.EventId}", From(item, "Completed"));
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        var text = From(item, "Completed");
        text["description"] = "Corrected description.";
        using var correction = await client.PutAsJsonAsync($"/api/events/{item.EventId}", text);
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
        text["distanceKm"] = 12m;
        using var history = await client.PutAsJsonAsync($"/api/events/{item.EventId}", text);
        Assert.Equal(HttpStatusCode.Conflict, history.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal(1, await read.Results.CountAsync());
        Assert.Equal("Completed", (await read.Events.FindAsync(item.EventId))!.Status);
    }

    [Theory]
    [InlineData("category")]
    [InlineData("confirmed")]
    [InlineData("cancelled")]
    [InlineData("result")]
    [InlineData("banner")]
    public async Task Delete_ProtectsRelatedRecordsAndImages(string dependency)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        if (dependency == "banner")
        {
            await using var edit = database.CreateContext();
            await edit.Events.Where(x => x.EventId == item.EventId).ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.BannerImageBlobName, "event-banners/example.jpg"));
        }
        else
        {
            var category = await AddCategory(item.EventId);
            if (dependency != "category")
            {
                var entry = await AddEntry(item.EventId, category.EventCategoryId, 3,
                    dependency == "cancelled" ? "Cancelled" : "Confirmed");
                if (dependency == "result")
                {
                    await using var edit = database.CreateContext();
                    edit.Results.Add(new Result
                    {
                        EnrolmentId = entry.EnrolmentId, RecordedByUserId = 1,
                        ResultStatus = "Completed", FinishTimeSeconds = 3600, RecordedAt = Now.UtcDateTime
                    });
                    await edit.SaveChangesAsync();
                }
            }
        }
        using var response = await client.DeleteAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var read = database.CreateContext();
        Assert.NotNull(await read.Events.FindAsync(item.EventId));
        if (dependency == "result") Assert.Equal(1, await read.Results.CountAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Unknown")]
    public async Task Update_RequiresAValidStatus(string? status)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var request = Details(status);
        using var response = await client.PutAsJsonAsync($"/api/events/{item.EventId}", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var read = database.CreateContext();
        Assert.Null((await read.Events.FindAsync(item.EventId))!.UpdatedAt);
    }

    [Theory]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    public async Task Update_ProtectsTerminalDatesEvenWithoutEntries(string status)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent(status, past: status == "Completed");
        var request = From(item, status);
        request["eventDateTime"] = item.EventDateTime.AddDays(1);
        using var blocked = await client.PutAsJsonAsync($"/api/events/{item.EventId}", request);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        request = From(item, status);
        request["eventName"] = "Corrected name";
        using var correction = await client.PutAsJsonAsync($"/api/events/{item.EventId}", request);
        Assert.Equal(HttpStatusCode.OK, correction.StatusCode);
    }

    [Fact]
    public async Task Update_ReplacesEditableFieldsAndCanClearCoordinatesBeforeEntriesExist()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var request = From(item, "Draft");
        request["eventType"] = "Cycling";
        request["distanceKm"] = 20m;
        request["eventDateTime"] = item.EventDateTime.AddDays(1);
        request["entryClosingDateTime"] = item.EntryClosingDateTime.AddDays(1);
        request["latitude"] = null;
        request["longitude"] = null;
        request["province"] = "Western Cape";
        using var response = await client.PutAsJsonAsync($"/api/events/{item.EventId}", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var read = database.CreateContext();
        var saved = (await read.Events.FindAsync(item.EventId))!;
        Assert.Equal("Cycling", saved.EventType);
        Assert.Equal(20m, saved.DistanceKm);
        Assert.Equal(item.EventDateTime.AddDays(1), saved.EventDateTime);
        Assert.Equal(item.EntryClosingDateTime.AddDays(1), saved.EntryClosingDateTime);
        Assert.Equal("Western Cape", saved.Province);
        Assert.Null(saved.Latitude);
        Assert.Null(saved.Longitude);
        Assert.Equal(item.CreatedAt, saved.CreatedAt);
        Assert.Equal(1, saved.OrganiserUserId);
    }

    [Fact]
    public async Task Delete_RemovesAnEmptyEventAndLaterReadsAndDeletesReturn404()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        using var removed = await client.DeleteAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Equal("", await removed.Content.ReadAsStringAsync());
        using var get = await client.GetAsync($"/api/events/{item.EventId}");
        using var again = await client.DeleteAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
        await using var read = database.CreateContext();
        Assert.Null(await read.Events.FindAsync(item.EventId));
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Open")]
    [InlineData("Closed")]
    [InlineData("Cancelled")]
    [InlineData("Completed")]
    public async Task Mine_FiltersEveryStatusAndNeverReturnsAnotherOwnersEvents(string status)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        Event? expected = null;
        foreach (string value in new[] { "Draft", "Open", "Closed", "Cancelled", "Completed" })
        {
            var item = await AddEvent(value);
            if (value == status) expected = item;
        }
        await AddEvent(status, owner: 2);
        var page = (await client.GetFromJsonAsync<EventPageResponse>(
            "/api/events/mine?status=" + Uri.EscapeDataString(" " + status.ToLowerInvariant() + " ") + "&organiserUserId=2"))!;
        Assert.Equal(expected!.EventId, Assert.Single(page.Items).EventId);
        Assert.Equal(1, page.TotalCount);
        var all = (await client.GetFromJsonAsync<EventPageResponse>("/api/events/mine?pageSize=2&page=2"))!;
        Assert.Equal(5, all.TotalCount);
        Assert.Equal(3, all.TotalPages);
        Assert.Equal(2, all.Items.Count);
        await using var read = database.CreateContext();
        var ordered = await read.Events.Where(x => x.OrganiserUserId == 1)
            .OrderBy(x => x.EventDateTime).ThenBy(x => x.EventId).Select(x => x.EventId).ToArrayAsync();
        Assert.Equal(ordered.Skip(2).Take(2), all.Items.Select(x => x.EventId));
    }

    [Theory]
    [InlineData("status=Unknown")]
    [InlineData("status=%20%20")]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=2147483647&pageSize=100")]
    public async Task Mine_RejectsInvalidFiltersAndPaging(string query)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        using var response = await client.GetAsync("/api/events/mine?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_CountsOnlyOwnedEventsAndSavedEntryStatusesWithoutResultDuplication()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var first = await AddEvent("Open");
        var second = await AddEvent("Completed", past: true);
        await AddEvent("Cancelled");
        var other = await AddEvent("Open", owner: 2);
        var category = await AddCategory(first.EventId);
        await AddEntry(first.EventId, category.EventCategoryId, 3);
        await AddEntry(first.EventId, category.EventCategoryId, 4, "Cancelled");
        var historical = await AddCategory(second.EventId);
        var resultEntry = await AddEntry(second.EventId, historical.EventCategoryId, 3);
        var otherCategory = await AddCategory(other.EventId);
        await AddEntry(other.EventId, otherCategory.EventCategoryId, 4);
        await using (var edit = database.CreateContext())
        {
            edit.Results.Add(new Result
            {
                EnrolmentId = resultEntry.EnrolmentId, RecordedByUserId = 1,
                ResultStatus = "Completed", FinishTimeSeconds = 3600, RecordedAt = Now.UtcDateTime
            });
            await edit.SaveChangesAsync();
        }
        using var response = await client.GetAsync("/api/organiser/dashboard?organiserUserId=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = (await response.Content.ReadFromJsonAsync<OrganiserDashboardResponse>())!;
        Assert.Equal(3, dashboard.TotalEvents);
        Assert.Equal(3, dashboard.TotalEnrolments);
        Assert.Equal(2, dashboard.ConfirmedEnrolments);
        Assert.Equal(1, dashboard.CancelledEnrolments);
        Assert.Equal(1, dashboard.UpcomingEvents);
        Assert.DoesNotContain(dashboard.Events, x => x.EventId == other.EventId);
        Assert.Equal(second.EventId, dashboard.Events[0].EventId);
        var firstSummary = Assert.Single(dashboard.Events, x => x.EventId == first.EventId);
        Assert.Equal(2, firstSummary.TotalEnrolments);
        Assert.Equal(1, firstSummary.ConfirmedEnrolments);
        Assert.Equal(1, firstSummary.CancelledEnrolments);
        Assert.All(dashboard.Events, x => Assert.Equal(DateTimeKind.Utc, x.EventDateTime.Kind));
        Assert.True(response.Headers.CacheControl?.NoStore);
        await AssertSafe(response);
    }

    [Fact]
    public async Task MineAndDashboard_ReturnZeroTotalsForAnOrganiserWithoutEvents()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "ayesha.khan@example.com");
        var mine = (await client.GetFromJsonAsync<EventPageResponse>("/api/events/mine"))!;
        Assert.Empty(mine.Items);
        Assert.Equal(0, mine.TotalCount);
        Assert.Equal(0, mine.TotalPages);
        var dashboard = (await client.GetFromJsonAsync<OrganiserDashboardResponse>("/api/organiser/dashboard"))!;
        Assert.Empty(dashboard.Events);
        Assert.Equal(0, dashboard.TotalEvents);
        Assert.Equal(0, dashboard.TotalEnrolments);
        Assert.Equal(0, dashboard.ConfirmedEnrolments);
        Assert.Equal(0, dashboard.CancelledEnrolments);
        Assert.Equal(0, dashboard.UpcomingEvents);
    }

    [Fact]
    public async Task Swagger_DescribesTheFiveProtectedOperationsAndItsWriteExamplesWork()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (route, method, success) in new[]
        {
            ("/api/events/mine", "get", "200"), ("/api/organiser/dashboard", "get", "200"),
            ("/api/events", "post", "201"), ("/api/events/{eventId}", "put", "200"),
            ("/api/events/{eventId}", "delete", "204")
        })
        {
            var operation = paths.GetProperty(route).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("SessionCookie", out _));
            foreach (string code in new[] { success, "401", "403" })
                Assert.True(operation.GetProperty("responses").TryGetProperty(code, out _));
        }
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        using var created = await client.PostAsync("/api/events", new StringContent(
            schemas.GetProperty("CreateEventRequest").GetProperty("example").GetRawText(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var updated = await client.PutAsync(created.Headers.Location, new StringContent(
            schemas.GetProperty("UpdateEventRequest").GetProperty("example").GetRawText(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Writes_RejectMalformedNullAndNonJsonBodies(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        string route = method == "POST" ? "/api/events" : $"/api/events/{item.EventId}";
        foreach (string json in new[] { "{", "null", "" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), route)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var form = new HttpRequestMessage(new HttpMethod(method), route)
            { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["eventName"] = "Test" }) };
        using var unsupported = await client.SendAsync(form);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
    }

    private async Task AssertInvalidWrites(Dictionary<string, object?> request)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        using var create = await client.PostAsJsonAsync("/api/events", request);
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        request["status"] = "Draft";
        using var update = await client.PutAsJsonAsync($"/api/events/{item.EventId}", request);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal(1, await read.Events.CountAsync());
        var saved = (await read.Events.FindAsync(item.EventId))!;
        Assert.Equal("Original Event", saved.EventName);
        Assert.Null(saved.UpdatedAt);
        Assert.Equal(1, saved.OrganiserUserId);
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

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, string route) => method switch
    {
        "POST" => client.PostAsJsonAsync(route, Details()),
        "PUT" => client.PutAsJsonAsync(route, Details("Draft")),
        "DELETE" => client.DeleteAsync(route), _ => client.GetAsync(route)
    };

    private static Dictionary<string, object?> Details(string? status = null)
    {
        var values = new Dictionary<string, object?>
        {
            ["eventName"] = " Community Run ", ["eventType"] = "Running", ["description"] = " A community event. ",
            ["distanceKm"] = 10m, ["eventDateTime"] = new DateTime(2030, 1, 10, 6, 0, 0, DateTimeKind.Utc),
            ["entryClosingDateTime"] = new DateTime(2030, 1, 3, 6, 0, 0, DateTimeKind.Utc),
            ["venueName"] = " Main Stadium ", ["addressLine1"] = " 1 Main Road ", ["city"] = " Johannesburg ",
            ["province"] = "Gauteng", ["latitude"] = -26.2041m, ["longitude"] = 28.0473m
        };
        if (status is not null) values["status"] = status;
        return values;
    }

    private static Dictionary<string, object?> From(Event item, string status)
    {
        var values = Details(status);
        values["eventName"] = item.EventName;
        values["eventType"] = item.EventType;
        values["description"] = item.Description;
        values["distanceKm"] = item.DistanceKm;
        values["eventDateTime"] = DateTime.SpecifyKind(item.EventDateTime, DateTimeKind.Utc);
        values["entryClosingDateTime"] = DateTime.SpecifyKind(item.EntryClosingDateTime, DateTimeKind.Utc);
        return values;
    }

    private async Task<Event> AddEvent(string status = "Draft", int owner = 1, bool past = false)
    {
        var item = new Event
        {
            EventName = "Original Event", OrganiserUserId = owner, EventType = "Running",
            Description = "Original description.", DistanceKm = 10,
            EventDateTime = past ? Now.UtcDateTime.AddDays(-1) : new DateTime(2030, 1, 10, 6, 0, 0, DateTimeKind.Utc),
            EntryClosingDateTime = past ? Now.UtcDateTime.AddDays(-2) : new DateTime(2030, 1, 3, 6, 0, 0, DateTimeKind.Utc),
            VenueName = "Main Stadium", AddressLine1 = "1 Main Road", City = "Johannesburg", Province = "Gauteng",
            Status = status, CreatedAt = Now.UtcDateTime.AddDays(-10)
        };
        await using var context = database.CreateContext();
        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private async Task<EventCategory> AddCategory(int eventId, bool available = true)
    {
        var category = new EventCategory
        {
            EventId = eventId, CategoryName = "10 km", DistanceKm = 10, EntryFee = 100, IsAvailable = available,
            CreatedAt = Now.UtcDateTime
        };
        await using var context = database.CreateContext();
        context.EventCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private async Task<Enrolment> AddEntry(int eventId, int categoryId, int participantId, string status = "Confirmed")
    {
        var entry = new Enrolment
        {
            EventId = eventId, EventCategoryId = categoryId, ParticipantUserId = participantId,
            Status = status, EnrolmentDate = Now.UtcDateTime
        };
        await using var context = database.CreateContext();
        context.Enrolments.Add(entry);
        await context.SaveChangesAsync();
        return entry;
    }

    private static async Task AssertSafe(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        foreach (string field in new[] { "passwordHash", "password", "email", "phoneNumber", "bannerImageBlobName", "participant" })
            Assert.DoesNotContain("\"" + field + "\"", json, StringComparison.OrdinalIgnoreCase);
    }
}
