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

namespace RaceDay.Api.Tests.Categories;

// HTTP, session and SQL Server checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database
// Accessed: 18 October 2026.
public sealed class CategoryTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>, IAsyncLifetime
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
    public async Task Create_SavesExplicitFalseAndReturnsAnOwnerReadableLocation()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var request = Details();
        request["isAvailable"] = false;
        using var response = await client.PostAsJsonAsync(Path(item.EventId), request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<CategorySummaryResponse>())!;
        Assert.Equal(item.EventId, saved.EventId);
        Assert.Equal("10 km Run", saved.CategoryName);
        Assert.False(saved.IsAvailable);
        Assert.Null(saved.Route);
        Assert.Equal(Path(item.EventId, saved.EventCategoryId), response.Headers.Location?.OriginalString);
        using var ownerRead = await client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, ownerRead.StatusCode);
        using var anonymous = factory.Client();
        using var hidden = await anonymous.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        await using var read = database.CreateContext();
        var row = (await read.EventCategories.FindAsync(saved.EventCategoryId))!;
        Assert.Equal(Now.UtcDateTime, row.CreatedAt);
        Assert.Null(row.UpdatedAt);
        Assert.Equal<byte?>(15, row.MinimumAge);
        Assert.Null(row.MaximumAge);
        Assert.Equal(150m, row.EntryFee);
        await AssertSafe(response);
    }

    [Theory]
    [InlineData("categoryName")]
    [InlineData("distanceKm")]
    [InlineData("entryFee")]
    [InlineData("isAvailable")]
    public async Task Writes_RequirePlannedFields(string field)
    {
        var request = Details();
        request.Remove(field);
        await AssertInvalid(request);
    }

    [Theory]
    [InlineData("categoryName", "")]
    [InlineData("categoryName", "  ")]
    [InlineData("categoryName", null)]
    [InlineData("distanceKm", 0)]
    [InlineData("distanceKm", -1)]
    [InlineData("distanceKm", 10000)]
    [InlineData("distanceKm", 10.123)]
    [InlineData("distanceKm", null)]
    [InlineData("entryFee", -1)]
    [InlineData("entryFee", 100000000)]
    [InlineData("entryFee", 10.123)]
    [InlineData("entryFee", null)]
    [InlineData("minimumAge", 0)]
    [InlineData("minimumAge", 121)]
    [InlineData("minimumAge", 20.5)]
    [InlineData("maximumAge", 0)]
    [InlineData("maximumAge", 121)]
    [InlineData("maximumAge", 14)]
    [InlineData("capacity", 0)]
    [InlineData("capacity", -1)]
    [InlineData("capacity", 2.5)]
    [InlineData("capacity", 2147483648L)]
    [InlineData("isAvailable", null)]
    [InlineData("isAvailable", "yes")]
    public async Task Writes_RejectInvalidValuesAndPrecision(string field, object? value)
    {
        var request = Details();
        request[field] = value;
        await AssertInvalid(request);
    }

    [Fact]
    public async Task Writes_RejectOverlongNames()
    {
        var request = Details();
        request["categoryName"] = new string('a', 81);
        await AssertInvalid(request);
    }

    [Theory]
    [InlineData("eventId")]
    [InlineData("eventCategoryId")]
    [InlineData("organiserUserId")]
    [InlineData("createdAt")]
    [InlineData("updatedAt")]
    [InlineData("route")]
    public async Task Writes_RejectServerAssignedOrUnknownFields(string field)
    {
        var request = Details();
        request[field] = 99;
        await AssertInvalid(request);
    }

    [Fact]
    public async Task Create_AcceptsSqlBoundariesAndOptionalNulls()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var request = Details();
        request["categoryName"] = new string('a', 80);
        request["distanceKm"] = 9999.99m;
        request["entryFee"] = 99999999.99m;
        request["minimumAge"] = 120;
        request["maximumAge"] = 120;
        request["capacity"] = int.MaxValue;
        using var response = await client.PostAsJsonAsync(Path(item.EventId), request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        request["categoryName"] = "Free walk";
        request["distanceKm"] = .01m;
        request["entryFee"] = 0;
        request.Remove("minimumAge");
        request.Remove("maximumAge");
        request.Remove("capacity");
        using var free = await client.PostAsJsonAsync(Path(item.EventId), request);
        Assert.Equal(HttpStatusCode.Created, free.StatusCode);
        var saved = (await free.Content.ReadFromJsonAsync<CategorySummaryResponse>())!;
        Assert.Null(saved.MinimumAge);
        Assert.Null(saved.MaximumAge);
        Assert.Null(saved.Capacity);
        Assert.Equal(0m, saved.EntryFee);
    }

    [Fact]
    public async Task Names_AreUniqueWithinEachEventAndCanBeRenamed()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var other = await AddEvent();
        var category = await AddCategory(item.EventId);
        using var duplicate = await client.PostAsJsonAsync(Path(item.EventId), Details());
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var allowed = await client.PostAsJsonAsync(Path(other.EventId), Details());
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        var request = Details();
        request["categoryName"] = "New name";
        using var renamed = await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), request);
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
        var second = await AddCategory(item.EventId, name: "Other category");
        using var clash = await client.PutAsJsonAsync(Path(item.EventId, second.EventCategoryId), request);
        Assert.Equal(HttpStatusCode.Conflict, clash.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal("New name", (await read.EventCategories.FindAsync(category.EventCategoryId))!.CategoryName);
        Assert.Equal("Other category", (await read.EventCategories.FindAsync(second.EventCategoryId))!.CategoryName);
    }

    [Fact]
    public async Task ConcurrentDuplicateCreation_SavesOnlyOneCategory()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first);
        await Login(second);
        var item = await AddEvent();
        var responses = await Task.WhenAll(first.PostAsJsonAsync(Path(item.EventId), Details()),
            second.PostAsJsonAsync(Path(item.EventId), Details()));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
            await using var read = database.CreateContext();
            Assert.Equal(1, await read.EventCategories.CountAsync());
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Fact]
    public async Task Update_ClearsOptionalFieldsAndKeepsItsRouteAndIdentity()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var category = await AddCategory(item.EventId);
        var route = await AddRoute(category.EventCategoryId);
        var request = Details();
        request["categoryName"] = " Updated category ";
        request.Remove("minimumAge");
        request.Remove("maximumAge");
        request.Remove("capacity");
        request["isAvailable"] = false;
        using var response = await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var saved = (await response.Content.ReadFromJsonAsync<CategorySummaryResponse>())!;
        Assert.Equal("Updated category", saved.CategoryName);
        Assert.Equal(item.EventId, saved.EventId);
        Assert.Equal(category.EventCategoryId, saved.EventCategoryId);
        Assert.Equal(route.RouteId, saved.Route!.RouteId);
        Assert.False(saved.IsAvailable);
        Assert.Null(saved.MinimumAge);
        Assert.Null(saved.MaximumAge);
        Assert.Null(saved.Capacity);
        await using var read = database.CreateContext();
        var row = (await read.EventCategories.FindAsync(category.EventCategoryId))!;
        Assert.Equal(category.CreatedAt, row.CreatedAt);
        Assert.Equal(Now.UtcDateTime, row.UpdatedAt);
    }

    [Theory]
    [InlineData("Confirmed", "distanceKm", 12)]
    [InlineData("Confirmed", "minimumAge", 16)]
    [InlineData("Confirmed", "maximumAge", 80)]
    [InlineData("Confirmed", "entryFee", 200)]
    [InlineData("Cancelled", "distanceKm", 12)]
    [InlineData("Cancelled", "minimumAge", 16)]
    [InlineData("Cancelled", "maximumAge", 80)]
    [InlineData("Cancelled", "entryFee", 200)]
    public async Task SavedEntries_ProtectAgreedCategoryDetails(string status, string field, object value)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent("Closed");
        var category = await AddCategory(item.EventId);
        await AddEntry(item.EventId, category.EventCategoryId, 3, status);
        var request = Details();
        request[field] = value;
        using var response = await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        await using var read = database.CreateContext();
        var row = (await read.EventCategories.FindAsync(category.EventCategoryId))!;
        Assert.Equal(10m, row.DistanceKm);
        Assert.Equal<byte?>(15, row.MinimumAge);
        Assert.Null(row.MaximumAge);
        Assert.Equal(150m, row.EntryFee);
        Assert.Null(row.UpdatedAt);
    }

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("Cancelled")]
    public async Task SavedEntries_AllowRenamingAndHidingButPreventDeletion(string status)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent("Closed");
        var category = await AddCategory(item.EventId);
        await AddEntry(item.EventId, category.EventCategoryId, 3, status);
        var request = Details();
        request["categoryName"] = "Corrected name";
        request["isAvailable"] = false;
        using var updated = await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), request);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var deleted = await client.DeleteAsync(Path(item.EventId, category.EventCategoryId));
        Assert.Equal(HttpStatusCode.Conflict, deleted.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal(1, await read.Enrolments.CountAsync());
        Assert.Equal(1, await read.EventCategories.CountAsync());
    }

    [Theory]
    [InlineData(1, "Confirmed", HttpStatusCode.Conflict)]
    [InlineData(2, "Confirmed", HttpStatusCode.OK)]
    [InlineData(3, "Confirmed", HttpStatusCode.OK)]
    [InlineData(null, "Confirmed", HttpStatusCode.OK)]
    [InlineData(1, "Cancelled", HttpStatusCode.OK)]
    public async Task Capacity_CountsConfirmedEntriesOnly(int? capacity, string secondStatus, HttpStatusCode expected)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent("Closed");
        var category = await AddCategory(item.EventId);
        await AddEntry(item.EventId, category.EventCategoryId, 3);
        await AddEntry(item.EventId, category.EventCategoryId, 4, secondStatus);
        var request = Details();
        request["capacity"] = capacity;
        using var response = await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), request);
        Assert.Equal(expected, response.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal(expected == HttpStatusCode.OK ? capacity : 100,
            (await read.EventCategories.FindAsync(category.EventCategoryId))!.Capacity);
    }

    [Theory]
    [InlineData("PUT", "Open", false, HttpStatusCode.Conflict)]
    [InlineData("DELETE", "Open", false, HttpStatusCode.Conflict)]
    [InlineData("PUT", "Open", true, HttpStatusCode.OK)]
    [InlineData("DELETE", "Open", true, HttpStatusCode.NoContent)]
    [InlineData("PUT", "Closed", false, HttpStatusCode.OK)]
    [InlineData("DELETE", "Closed", false, HttpStatusCode.NoContent)]
    public async Task OpenEvents_KeepAtLeastOneAvailableCategory(string method, string status, bool another, HttpStatusCode expected)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent(status);
        var category = await AddCategory(item.EventId);
        if (another) await AddCategory(item.EventId, name: "Another distance");
        var request = Details();
        request["isAvailable"] = false;
        using var response = method == "DELETE" ? await client.DeleteAsync(Path(item.EventId, category.EventCategoryId)) :
            await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), request);
        Assert.Equal(expected, response.StatusCode);
        await using var read = database.CreateContext();
        if (expected == HttpStatusCode.Conflict)
            Assert.True((await read.EventCategories.FindAsync(category.EventCategoryId))!.IsAvailable);
        Assert.NotNull(await read.Events.FindAsync(item.EventId));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Delete_RemovesUnusedCategoryAndOptionalRouteTogether(bool withRoute)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var category = await AddCategory(item.EventId);
        if (withRoute) await AddRoute(category.EventCategoryId);
        using var response = await client.DeleteAsync(Path(item.EventId, category.EventCategoryId));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsStringAsync());
        using var missing = await client.GetAsync(Path(item.EventId, category.EventCategoryId));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var read = database.CreateContext();
        Assert.Empty(await read.EventCategories.ToListAsync());
        Assert.Empty(await read.EventRoutes.ToListAsync());
        Assert.NotNull(await read.Events.FindAsync(item.EventId));
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task ProtectedOperations_RejectMissingWrongRoleAndExpiredSessions(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var item = await AddEvent();
        var category = await AddCategory(item.EventId);
        using var missing = await Send(client, method, item.EventId, category.EventCategoryId);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        await Login(client, "naledi.mokoena@example.com");
        using var wrong = await Send(client, method, item.EventId, category.EventCategoryId);
        Assert.Equal(HttpStatusCode.Forbidden, wrong.StatusCode);
        await Login(client);
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var expired = await Send(client, method, item.EventId, category.EventCategoryId);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task OwnershipChecks_RejectAnotherOrganiserAndMissingEvents(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent(owner: 2);
        var category = await AddCategory(item.EventId);
        using var forbidden = await Send(client, method, item.EventId, category.EventCategoryId);
        using var missing = await Send(client, method, int.MaxValue, category.EventCategoryId);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal("10 km Run", (await read.EventCategories.FindAsync(category.EventCategoryId))!.CategoryName);
        Assert.Equal(1, await read.EventCategories.CountAsync());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task CategoryIds_CannotBeUsedUnderAnotherEvent(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var other = await AddEvent();
        var category = await AddCategory(other.EventId);
        string path = Path(item.EventId, category.EventCategoryId);
        using var response = method switch
        {
            "GET" => await client.GetAsync(path), "DELETE" => await client.DeleteAsync(path),
            _ => await client.PutAsJsonAsync(path, Details())
        };
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var read = database.CreateContext();
        Assert.NotNull(await read.EventCategories.FindAsync(category.EventCategoryId));
    }

    [Theory]
    [InlineData("Open", HttpStatusCode.OK)]
    [InlineData("Closed", HttpStatusCode.OK)]
    [InlineData("Completed", HttpStatusCode.OK)]
    [InlineData("Draft", HttpStatusCode.NotFound)]
    [InlineData("Cancelled", HttpStatusCode.NotFound)]
    public async Task PublicList_UsesPublicStatusesAndAvailableCategoriesEvenForTheOwner(string status, HttpStatusCode expected)
    {
        using var factory = Factory();
        using var owner = factory.Client();
        await Login(owner);
        using var anonymous = factory.Client();
        var item = await AddEvent(status);
        var visible = await AddCategory(item.EventId);
        await AddRoute(visible.EventCategoryId);
        await AddCategory(item.EventId, available: false, name: "Hidden");
        await AddCategory((await AddEvent("Open")).EventId, name: "Another event");
        foreach (var client in new[] { anonymous, owner })
        {
            using var response = await client.GetAsync(Path(item.EventId));
            Assert.Equal(expected, response.StatusCode);
            if (expected != HttpStatusCode.OK) continue;
            var categories = (await response.Content.ReadFromJsonAsync<List<CategorySummaryResponse>>())!;
            var category = Assert.Single(categories);
            Assert.Equal(visible.EventCategoryId, category.EventCategoryId);
            Assert.Equal(item.EventId, category.EventId);
            Assert.Equal("Stadium route", category.Route!.RouteName);
            Assert.True(response.Headers.CacheControl?.NoStore);
            await AssertSafe(response);
        }
    }

    [Fact]
    public async Task Lists_ReturnEmptyArraysAndOwnedListIncludesHiddenRecords()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent("Closed");
        Assert.Empty((await client.GetFromJsonAsync<List<CategorySummaryResponse>>(Path(item.EventId)))!);
        Assert.Empty((await client.GetFromJsonAsync<List<CategorySummaryResponse>>(OwnedPath(item.EventId)))!);
        var first = await AddCategory(item.EventId, available: false);
        var second = await AddCategory(item.EventId, name: "5 km walk");
        var all = (await client.GetFromJsonAsync<List<CategorySummaryResponse>>(OwnedPath(item.EventId)))!;
        Assert.Equal(new[] { first.EventCategoryId, second.EventCategoryId }, all.Select(x => x.EventCategoryId));
        Assert.Single((await client.GetFromJsonAsync<List<CategorySummaryResponse>>(Path(item.EventId)))!);
    }

    [Theory]
    [InlineData("Open", true)]
    [InlineData("Closed", true)]
    [InlineData("Completed", true)]
    [InlineData("Open", false)]
    [InlineData("Draft", true)]
    [InlineData("Cancelled", true)]
    public async Task Details_OnlyTheOwnerCanReadHiddenCategories(string status, bool available)
    {
        using var factory = Factory();
        var item = await AddEvent(status);
        var category = await AddCategory(item.EventId, available);
        bool isPublic = available && status is "Open" or "Closed" or "Completed";
        foreach (string? email in new string?[] { null, "thabo.ndlovu@example.com", "ayesha.khan@example.com", "naledi.mokoena@example.com" })
        {
            using var client = factory.Client();
            if (email is not null) await Login(client, email);
            using var response = await client.GetAsync(Path(item.EventId, category.EventCategoryId));
            Assert.Equal(isPublic || email == "thabo.ndlovu@example.com" ? HttpStatusCode.OK : HttpStatusCode.NotFound,
                response.StatusCode);
        }
    }

    [Fact]
    public async Task ExpiredOwnerSession_CannotRevealADraftButPublicCategoriesRemainReadable()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var draft = await AddEvent();
        var hidden = await AddCategory(draft.EventId);
        var item = await AddEvent("Open");
        var visible = await AddCategory(item.EventId);
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var rejected = await client.GetAsync(Path(draft.EventId, hidden.EventCategoryId));
        Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
        using var allowed = await client.GetAsync(Path(item.EventId, visible.EventCategoryId));
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    [Fact]
    public async Task Swagger_DescribesAllSixOperationsAndItsExampleWorks()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (path, method, code, secured) in new[]
        {
            ("/api/events/{eventId}/categories", "get", "200", false),
            ("/api/events/{eventId}/categories/{categoryId}", "get", "200", false),
            ("/api/organiser/events/{eventId}/categories", "get", "200", true),
            ("/api/events/{eventId}/categories", "post", "201", true),
            ("/api/events/{eventId}/categories/{categoryId}", "put", "200", true),
            ("/api/events/{eventId}/categories/{categoryId}", "delete", "204", true)
        })
        {
            var operation = paths.GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("responses").TryGetProperty(code, out _));
            Assert.True(operation.GetProperty("responses").TryGetProperty("404", out _));
            if (secured)
            {
                Assert.True(operation.GetProperty("security")[0].TryGetProperty("SessionCookie", out _));
                Assert.True(operation.GetProperty("responses").TryGetProperty("401", out _));
                Assert.True(operation.GetProperty("responses").TryGetProperty("403", out _));
            }
        }
        var optional = paths.GetProperty("/api/events/{eventId}/categories/{categoryId}").GetProperty("get").GetProperty("security");
        Assert.Empty(optional[0].EnumerateObject());
        Assert.True(optional[1].TryGetProperty("SessionCookie", out _));
        string example = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("CategoryWriteRequest").GetProperty("example").GetRawText();
        using var created = await client.PostAsync(Path(item.EventId), new StringContent(example, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var updated = await client.PutAsync(created.Headers.Location, new StringContent(example, Encoding.UTF8, "application/json"));
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
        var category = await AddCategory(item.EventId);
        string path = method == "POST" ? Path(item.EventId) : Path(item.EventId, category.EventCategoryId);
        foreach (string json in new[] { "{", "null", "" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var form = new HttpRequestMessage(new HttpMethod(method), path)
            { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["categoryName"] = "Test" }) };
        using var unsupported = await client.SendAsync(form);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
    }

    [Fact]
    public async Task EventWorkflow_AddsACategoryOpensTheEventAndRequiresClosingBeforeHidingItsLastCategory()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        string createJson = schemas.GetProperty("CreateEventRequest").GetProperty("example").GetRawText();
        using var created = await client.PostAsync("/api/events", new StringContent(createJson, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var item = (await created.Content.ReadFromJsonAsync<EventDetailsResponse>())!;
        var update = JsonSerializer.Deserialize<Dictionary<string, object?>>(createJson)!;
        update["status"] = "Open";
        using var notReady = await client.PutAsJsonAsync($"/api/events/{item.EventId}", update);
        Assert.Equal(HttpStatusCode.Conflict, notReady.StatusCode);
        using var added = await client.PostAsJsonAsync(Path(item.EventId), Details());
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var category = (await added.Content.ReadFromJsonAsync<CategorySummaryResponse>())!;
        using var opened = await client.PutAsJsonAsync($"/api/events/{item.EventId}", update);
        Assert.Equal(HttpStatusCode.OK, opened.StatusCode);
        using var anonymous = factory.Client();
        Assert.Single((await anonymous.GetFromJsonAsync<List<CategorySummaryResponse>>(Path(item.EventId)))!);
        var hidden = Details();
        hidden["isAvailable"] = false;
        using var blocked = await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), hidden);
        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        update["status"] = "Closed";
        using var closed = await client.PutAsJsonAsync($"/api/events/{item.EventId}", update);
        Assert.Equal(HttpStatusCode.OK, closed.StatusCode);
        using var disabled = await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), hidden);
        Assert.Equal(HttpStatusCode.OK, disabled.StatusCode);
        Assert.Empty((await anonymous.GetFromJsonAsync<List<CategorySummaryResponse>>(Path(item.EventId)))!);
        update["status"] = "Open";
        using var noCategory = await client.PutAsJsonAsync($"/api/events/{item.EventId}", update);
        Assert.Equal(HttpStatusCode.Conflict, noCategory.StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ChangedAccounts_CannotUseAnOldSessionToRevealHiddenCategories(bool changeRole)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var category = await AddCategory(item.EventId);
        await using var context = database.CreateContext();
        var user = (await context.Users.FindAsync(1))!;
        byte oldRole = user.RoleId;
        bool wasActive = user.IsActive;
        try
        {
            if (changeRole) user.RoleId = 2;
            else user.IsActive = false;
            await context.SaveChangesAsync();
            using var response = await client.GetAsync(Path(item.EventId, category.EventCategoryId));
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            using var protectedRead = await client.GetAsync(OwnedPath(item.EventId));
            Assert.Equal(HttpStatusCode.Unauthorized, protectedRead.StatusCode);
        }
        finally
        {
            user.RoleId = oldRole;
            user.IsActive = wasActive;
            await context.SaveChangesAsync();
        }
    }

    private async Task AssertInvalid(Dictionary<string, object?> request)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var item = await AddEvent();
        var category = await AddCategory(item.EventId);
        using var create = await client.PostAsJsonAsync(Path(item.EventId), request);
        using var update = await client.PutAsJsonAsync(Path(item.EventId, category.EventCategoryId), request);
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal(1, await read.EventCategories.CountAsync());
        var row = (await read.EventCategories.FindAsync(category.EventCategoryId))!;
        Assert.Equal("10 km Run", row.CategoryName);
        Assert.Equal(10m, row.DistanceKm);
        Assert.Null(row.UpdatedAt);
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

    private static string Path(int eventId, int? categoryId = null) => $"/api/events/{eventId}/categories" +
        (categoryId.HasValue ? $"/{categoryId}" : "");
    private static string OwnedPath(int eventId) => $"/api/organiser/events/{eventId}/categories";

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, int eventId, int categoryId) => method switch
    {
        "POST" => client.PostAsJsonAsync(Path(eventId), Details()),
        "PUT" => client.PutAsJsonAsync(Path(eventId, categoryId), Details()),
        "DELETE" => client.DeleteAsync(Path(eventId, categoryId)), _ => client.GetAsync(OwnedPath(eventId))
    };

    private static Dictionary<string, object?> Details() => new()
    {
        ["categoryName"] = " 10 km Run ", ["distanceKm"] = 10m, ["minimumAge"] = 15,
        ["maximumAge"] = null, ["entryFee"] = 150m, ["capacity"] = 100, ["isAvailable"] = true
    };

    private async Task<Event> AddEvent(string status = "Draft", int owner = 1)
    {
        var item = new Event
        {
            EventName = "Community Run", OrganiserUserId = owner, EventType = "Running",
            Description = "A community event.", DistanceKm = 10,
            EventDateTime = new DateTime(2030, 1, 10, 6, 0, 0, DateTimeKind.Utc),
            EntryClosingDateTime = new DateTime(2030, 1, 3, 6, 0, 0, DateTimeKind.Utc),
            VenueName = "Main Stadium", AddressLine1 = "1 Main Road", City = "Johannesburg", Province = "Gauteng",
            Status = status, CreatedAt = Now.UtcDateTime.AddDays(-10)
        };
        await using var context = database.CreateContext();
        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private async Task<EventCategory> AddCategory(int eventId, bool available = true, string name = "10 km Run")
    {
        var category = new EventCategory
        {
            EventId = eventId, CategoryName = name, DistanceKm = 10, MinimumAge = 15,
            EntryFee = 150, Capacity = 100, IsAvailable = available, CreatedAt = Now.UtcDateTime.AddDays(-1)
        };
        await using var context = database.CreateContext();
        context.EventCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private async Task<EventRoute> AddRoute(int categoryId)
    {
        var route = new EventRoute
        {
            EventCategoryId = categoryId, RouteName = "Stadium route", StartLocation = "Gate A",
            FinishLocation = "Finish line", RouteDescription = "Follow the marked course.",
            RouteMapUrl = "https://example.com/map", ElevationGainMetres = 100, CreatedAt = Now.UtcDateTime
        };
        await using var context = database.CreateContext();
        context.EventRoutes.Add(route);
        await context.SaveChangesAsync();
        return route;
    }

    private async Task AddEntry(int eventId, int categoryId, int participantId, string status = "Confirmed")
    {
        await using var context = database.CreateContext();
        context.Enrolments.Add(new Enrolment
        {
            EventId = eventId, EventCategoryId = categoryId, ParticipantUserId = participantId,
            Status = status, EnrolmentDate = Now.UtcDateTime
        });
        await context.SaveChangesAsync();
    }

    private static async Task AssertSafe(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        foreach (string field in new[] { "passwordHash", "email", "phoneNumber", "participant", "enrolments", "organiserUserId" })
            Assert.DoesNotContain("\"" + field + "\"", json, StringComparison.OrdinalIgnoreCase);
    }
}
