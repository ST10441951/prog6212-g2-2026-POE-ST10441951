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

namespace RaceDay.Api.Tests.Routes;

// Real HTTP, session and SQL Server checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database
// Accessed: 18 October 2026.
public sealed class RouteTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>, IAsyncLifetime
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
    public async Task Create_AssignsTheCategoryLinkAndReturnsAnOwnerReadableDraftLocation()
    {
        using var factory = Factory();
        using var owner = factory.Client();
        await Login(owner);
        var category = await AddCategory(available: false);
        using var response = await owner.PostAsJsonAsync(Path(category), Details());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(Path(category), response.Headers.Location?.OriginalString);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var route = (await response.Content.ReadFromJsonAsync<RouteSummaryResponse>())!;
        Assert.Equal("Stadium Loop", route.RouteName);
        Assert.Equal("Gate A", route.StartLocation);
        Assert.Equal("Finish line", route.FinishLocation);
        Assert.Equal("Follow the marked course.", route.RouteDescription);
        Assert.Equal("https://example.com/routes/stadium", route.RouteMapUrl);
        Assert.Equal(85, route.ElevationGainMetres);
        using var fetched = await owner.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        using var anonymous = factory.Client();
        using var hidden = await anonymous.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        await using var read = database.CreateContext();
        var saved = (await read.EventRoutes.FindAsync(route.RouteId))!;
        Assert.Equal(category.EventCategoryId, saved.EventCategoryId);
        Assert.Equal(Now.UtcDateTime, saved.CreatedAt);
        Assert.Null(saved.UpdatedAt);
        Assert.Equal(1, await read.EventCategories.CountAsync());
        await AssertSafe(response);
    }

    [Theory]
    [InlineData("routeName")]
    [InlineData("startLocation")]
    [InlineData("finishLocation")]
    [InlineData("routeDescription")]
    public async Task Writes_RequireAllPlannedTextFields(string field)
    {
        var request = Details();
        request.Remove(field);
        await AssertInvalid(request);
    }

    [Theory]
    [InlineData("routeName", null)]
    [InlineData("routeName", "")]
    [InlineData("routeName", "  ")]
    [InlineData("startLocation", null)]
    [InlineData("startLocation", "  ")]
    [InlineData("finishLocation", null)]
    [InlineData("finishLocation", "  ")]
    [InlineData("routeDescription", null)]
    [InlineData("routeDescription", "  ")]
    [InlineData("routeMapUrl", "not-a-url")]
    [InlineData("routeMapUrl", "/maps/course")]
    [InlineData("routeMapUrl", "//example.com/map")]
    [InlineData("routeMapUrl", "ftp://example.com/map")]
    [InlineData("routeMapUrl", "file:///C:/map.png")]
    [InlineData("routeMapUrl", "javascript:alert(1)")]
    [InlineData("routeMapUrl", "mailto:runner@example.com")]
    [InlineData("routeMapUrl", "https://")]
    [InlineData("routeMapUrl", "https://example.com/map with spaces")]
    [InlineData("routeMapUrl", "https://user:secret@example.com/map")]
    [InlineData("elevationGainMetres", -1)]
    [InlineData("elevationGainMetres", 1.5)]
    [InlineData("elevationGainMetres", 2147483648L)]
    [InlineData("elevationGainMetres", "high")]
    public async Task Writes_RejectInvalidTextUrlsAndElevation(string field, object? value)
    {
        var request = Details();
        request[field] = value;
        await AssertInvalid(request);
    }

    [Theory]
    [InlineData("routeName", 121)]
    [InlineData("startLocation", 201)]
    [InlineData("finishLocation", 201)]
    [InlineData("routeDescription", 1001)]
    [InlineData("routeMapUrl", 501)]
    public async Task Writes_RejectValuesLongerThanTheirSqlColumns(string field, int length)
    {
        var request = Details();
        request[field] = field == "routeMapUrl" ? "https://example.com/" + new string('a', length - "https://example.com/".Length) : new string('a', length);
        await AssertInvalid(request);
    }

    [Theory]
    [InlineData("routeId")]
    [InlineData("eventCategoryId")]
    [InlineData("eventId")]
    [InlineData("organiserUserId")]
    [InlineData("createdAt")]
    [InlineData("updatedAt")]
    [InlineData("category")]
    public async Task Writes_RejectIdentityLinksTimestampsAndUnknownProperties(string field)
    {
        var request = Details();
        request[field] = 99;
        await AssertInvalid(request);
    }

    [Fact]
    public async Task Create_AcceptsSqlLengthBoundariesAndMaximumIntegerElevation()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        var request = Details();
        request["routeName"] = new string('a', 120);
        request["startLocation"] = new string('a', 200);
        request["finishLocation"] = new string('a', 200);
        request["routeDescription"] = new string('a', 1000);
        request["routeMapUrl"] = "https://example.com/" + new string('a', 500 - "https://example.com/".Length);
        request["elevationGainMetres"] = int.MaxValue;
        using var response = await client.PostAsJsonAsync(Path(category), request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var route = (await response.Content.ReadFromJsonAsync<RouteSummaryResponse>())!;
        Assert.Equal(120, route.RouteName.Length);
        Assert.Equal(500, route.RouteMapUrl!.Length);
        Assert.Equal(int.MaxValue, route.ElevationGainMetres);
        await using var read = database.CreateContext();
        Assert.Equal(1000, (await read.EventRoutes.FindAsync(route.RouteId))!.RouteDescription.Length);
    }

    [Theory]
    [InlineData("http://example.com/map")]
    [InlineData("https://example.com/map?course=10#start")]
    [InlineData(" https://example.com/map ")]
    [InlineData("https://example.com/map%20name")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Create_AcceptsOptionalMapLinksAndZeroElevation(string? link)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        var request = Details();
        request["routeMapUrl"] = link;
        request["elevationGainMetres"] = 0;
        using var response = await client.PostAsJsonAsync(Path(category), request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var route = (await response.Content.ReadFromJsonAsync<RouteSummaryResponse>())!;
        Assert.Equal(string.IsNullOrWhiteSpace(link) ? null : link.Trim(), route.RouteMapUrl);
        Assert.Equal(0, route.ElevationGainMetres);
    }

    [Fact]
    public async Task Create_RejectsASecondRouteButOtherCategoriesCanHaveTheirOwn()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        var route = await AddRoute(category);
        using var duplicate = await client.PostAsJsonAsync(Path(category), Details());
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        var second = await AddCategory();
        using var allowed = await client.PostAsJsonAsync(Path(second), Details());
        Assert.Equal(HttpStatusCode.Created, allowed.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal(2, await read.EventRoutes.CountAsync());
        Assert.Equal("Original route", (await read.EventRoutes.FindAsync(route.RouteId))!.RouteName);
    }

    [Fact]
    public async Task ConcurrentCreation_PreservesTheOneRoutePerCategoryConstraint()
    {
        using var factory = Factory();
        using var first = factory.Client();
        using var second = factory.Client();
        await Login(first);
        await Login(second);
        var category = await AddCategory();
        var responses = await Task.WhenAll(first.PostAsJsonAsync(Path(category), Details()), second.PostAsJsonAsync(Path(category), Details()));
        try
        {
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Created);
            Assert.Single(responses, x => x.StatusCode == HttpStatusCode.Conflict);
            await using var read = database.CreateContext();
            Assert.Equal(1, await read.EventRoutes.CountAsync());
        }
        finally { foreach (var response in responses) response.Dispose(); }
    }

    [Theory]
    [InlineData("omitted")]
    [InlineData("null")]
    [InlineData("blank")]
    public async Task Update_ClearsOptionalValuesAndKeepsIdsAndCreationTime(string optional)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        var original = await AddRoute(category);
        var request = Details();
        if (optional == "omitted") { request.Remove("routeMapUrl"); request.Remove("elevationGainMetres"); }
        else { request["routeMapUrl"] = optional == "blank" ? "  " : null; request["elevationGainMetres"] = null; }
        using var response = await client.PutAsJsonAsync(Path(category), request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var route = (await response.Content.ReadFromJsonAsync<RouteSummaryResponse>())!;
        Assert.Equal(original.RouteId, route.RouteId);
        Assert.Equal("Stadium Loop", route.RouteName);
        Assert.Equal("Gate A", route.StartLocation);
        Assert.Equal("Finish line", route.FinishLocation);
        Assert.Equal("Follow the marked course.", route.RouteDescription);
        Assert.Null(route.RouteMapUrl);
        Assert.Null(route.ElevationGainMetres);
        await using var read = database.CreateContext();
        var saved = (await read.EventRoutes.FindAsync(original.RouteId))!;
        Assert.Equal(category.EventCategoryId, saved.EventCategoryId);
        Assert.Equal(original.CreatedAt, saved.CreatedAt);
        Assert.Equal(Now.UtcDateTime, saved.UpdatedAt);
    }

    [Fact]
    public async Task RouteChanges_KeepCategoryEntriesAndResultsAndAllowLaterRecreation()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory("Completed");
        var route = await AddRoute(category);
        await using (var context = database.CreateContext())
        {
            var entry = new Enrolment
            {
                EventId = category.EventId, EventCategoryId = category.EventCategoryId, ParticipantUserId = 3,
                EnrolmentDate = Now.UtcDateTime, Status = "Confirmed", BibNumber = "A101"
            };
            context.Enrolments.Add(entry);
            await context.SaveChangesAsync();
            context.Results.Add(new Result
            {
                EnrolmentId = entry.EnrolmentId, RecordedByUserId = 1, ResultStatus = "Completed",
                FinishTimeSeconds = 3600, RecordedAt = Now.UtcDateTime
            });
            await context.SaveChangesAsync();
        }
        using var updated = await client.PutAsJsonAsync(Path(category), Details());
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var deleted = await client.DeleteAsync(Path(category));
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        Assert.Empty(await deleted.Content.ReadAsStringAsync());
        using var missing = await client.GetAsync(Path(category));
        using var secondDelete = await client.DeleteAsync(Path(category));
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, secondDelete.StatusCode);
        var savedCategory = (await client.GetFromJsonAsync<CategorySummaryResponse>($"/api/events/{category.EventId}/categories/{category.EventCategoryId}"))!;
        Assert.Null(savedCategory.Route);
        await using (var read = database.CreateContext())
        {
            Assert.Null(await read.EventRoutes.FindAsync(route.RouteId));
            Assert.Equal(1, await read.EventCategories.CountAsync());
            Assert.Equal(1, await read.Events.CountAsync());
            Assert.Equal("A101", (await read.Enrolments.SingleAsync()).BibNumber);
            Assert.Equal(3600, (await read.Results.SingleAsync()).FinishTimeSeconds);
        }
        using var recreated = await client.PostAsJsonAsync(Path(category), Details());
        Assert.Equal(HttpStatusCode.Created, recreated.StatusCode);
        Assert.NotEqual(route.RouteId, (await recreated.Content.ReadFromJsonAsync<RouteSummaryResponse>())!.RouteId);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Writes_RejectMissingParticipantAndExpiredSessions(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var category = await AddCategory();
        await AddRoute(category);
        using var missing = await Send(client, method, Path(category));
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        await Login(client, "naledi.mokoena@example.com");
        using var wrongRole = await Send(client, method, Path(category));
        Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
        await Login(client);
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var expired = await Send(client, method, Path(category));
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task Writes_RejectAnotherOwnerWithoutChangingTheRoute(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory(owner: 2);
        var route = await AddRoute(category);
        using var response = await Send(client, method, Path(category));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal("Original route", (await read.EventRoutes.FindAsync(route.RouteId))!.RouteName);
        Assert.Equal(1, await read.EventRoutes.CountAsync());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task EveryOperation_RejectsMissingEventsMissingCategoriesAndCrossEventIds(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory("Open");
        var other = await AddCategory("Open");
        var route = await AddRoute(category);
        foreach (string path in new[]
        {
            $"/api/events/{int.MaxValue}/categories/{category.EventCategoryId}/route",
            $"/api/events/{category.EventId}/categories/{int.MaxValue}/route",
            $"/api/events/{other.EventId}/categories/{category.EventCategoryId}/route"
        })
        {
            using var response = await Send(client, method, path);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        await using var read = database.CreateContext();
        Assert.NotNull(await read.EventRoutes.FindAsync(route.RouteId));
        Assert.Equal(1, await read.EventRoutes.CountAsync());
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task MissingRoutes_Return404WithoutCreatingARecord(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        using var response = await Send(client, method, Path(category));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var read = database.CreateContext();
        Assert.Empty(await read.EventRoutes.ToListAsync());
    }

    [Theory]
    [InlineData("Open", true)]
    [InlineData("Closed", true)]
    [InlineData("Completed", true)]
    [InlineData("Draft", true)]
    [InlineData("Cancelled", true)]
    [InlineData("Open", false)]
    [InlineData("Closed", false)]
    [InlineData("Completed", false)]
    [InlineData("Draft", false)]
    [InlineData("Cancelled", false)]
    public async Task Reads_RespectPublicVisibilityAndOnlyTheOwnersPrivateAccess(string status, bool available)
    {
        using var factory = Factory();
        var category = await AddCategory(status, available);
        var route = await AddRoute(category);
        bool isPublic = available && status is "Open" or "Closed" or "Completed";
        foreach (string? email in new string?[] { null, "thabo.ndlovu@example.com", "ayesha.khan@example.com", "naledi.mokoena@example.com" })
        {
            using var client = factory.Client();
            if (email is not null) await Login(client, email);
            using var response = await client.GetAsync(Path(category));
            var expected = isPublic || email == "thabo.ndlovu@example.com" ? HttpStatusCode.OK : HttpStatusCode.NotFound;
            Assert.Equal(expected, response.StatusCode);
            if (expected != HttpStatusCode.OK) continue;
            Assert.Equal(route.RouteId, (await response.Content.ReadFromJsonAsync<RouteSummaryResponse>())!.RouteId);
            Assert.True(response.Headers.CacheControl?.NoStore);
            await AssertSafe(response);
        }
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("inactive")]
    [InlineData("role changed")]
    [InlineData("invalid cookie")]
    public async Task InvalidOwnerSessions_CannotRevealHiddenRoutesButPublicRoutesStillWork(string reason)
    {
        using var factory = Factory();
        using var client = factory.Client();
        if (reason == "invalid cookie") client.DefaultRequestHeaders.Add("Cookie", "RaceDay.Session=invalid");
        else await Login(client);
        var hidden = await AddCategory();
        await AddRoute(hidden);
        var visible = await AddCategory("Open");
        await AddRoute(visible);
        await using var edit = database.CreateContext();
        var user = (await edit.Users.FindAsync(1))!;
        byte originalRole = user.RoleId;
        try
        {
            if (reason == "expired") factory.Clock.Advance(TimeSpan.FromMinutes(20));
            if (reason == "inactive") user.IsActive = false;
            if (reason == "role changed") user.RoleId = 2;
            await edit.SaveChangesAsync();
            using var rejected = await client.GetAsync(Path(hidden));
            Assert.Equal(HttpStatusCode.NotFound, rejected.StatusCode);
            using var allowed = await client.GetAsync(Path(visible));
            Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        }
        finally { user.IsActive = true; user.RoleId = originalRole; await edit.SaveChangesAsync(); }
    }

    [Fact]
    public async Task UpdatedRoutes_AppearInEventAndCategoryReadsWithoutPrivateData()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory("Open");
        using var created = await client.PostAsJsonAsync(Path(category), Details());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var request = Details();
        request["routeName"] = "Corrected route";
        using var updated = await client.PutAsJsonAsync(Path(category), request);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        using var anonymous = factory.Client();
        var item = (await anonymous.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{category.EventId}"))!;
        Assert.Equal("Corrected route", Assert.Single(item.Categories).Route!.RouteName);
        var list = (await anonymous.GetFromJsonAsync<List<CategorySummaryResponse>>($"/api/events/{category.EventId}/categories"))!;
        Assert.Equal("Corrected route", Assert.Single(list).Route!.RouteName);
        using var direct = await anonymous.GetAsync(Path(category));
        Assert.Equal(HttpStatusCode.OK, direct.StatusCode);
        await AssertSafe(direct);
    }

    [Fact]
    public async Task Swagger_DescribesAllOperationsAndItsCreationAndUpdateExampleWorks()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var path = document.RootElement.GetProperty("paths").GetProperty("/api/events/{eventId}/categories/{categoryId}/route");
        foreach (var (method, code) in new[] { ("get", "200"), ("post", "201"), ("put", "200"), ("delete", "204") })
        {
            var operation = path.GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("responses").TryGetProperty(code, out _));
            Assert.True(operation.GetProperty("responses").TryGetProperty("404", out _));
            Assert.Equal(2, operation.GetProperty("parameters").GetArrayLength());
            if (method == "get")
            {
                Assert.Empty(operation.GetProperty("security")[0].EnumerateObject());
                Assert.True(operation.GetProperty("security")[1].TryGetProperty("SessionCookie", out _));
            }
            else
            {
                Assert.True(operation.GetProperty("security")[0].TryGetProperty("SessionCookie", out _));
                foreach (string failure in new[] { "401", "403" })
                    Assert.True(operation.GetProperty("responses").TryGetProperty(failure, out _));
            }
        }
        string example = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("RouteWriteRequest").GetProperty("example").GetRawText();
        using var created = await client.PostAsync(Path(category), new StringContent(example, Encoding.UTF8, "application/json"));
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
        var category = await AddCategory();
        await AddRoute(category);
        foreach (string json in new[] { "{", "null", "" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), Path(category))
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var form = new HttpRequestMessage(new HttpMethod(method), Path(category))
            { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["routeName"] = "Test" }) };
        using var unsupported = await client.SendAsync(form);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
    }

    private async Task AssertInvalid(Dictionary<string, object?> request)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        var original = await AddRoute(category);
        using var create = await client.PostAsJsonAsync(Path(category), request);
        using var update = await client.PutAsJsonAsync(Path(category), request);
        Assert.Equal(HttpStatusCode.BadRequest, create.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode);
        await using var read = database.CreateContext();
        Assert.Equal(1, await read.EventRoutes.CountAsync());
        var saved = (await read.EventRoutes.FindAsync(original.RouteId))!;
        Assert.Equal("Original route", saved.RouteName);
        Assert.Equal(category.EventCategoryId, saved.EventCategoryId);
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

    private static string Path(EventCategory category) => $"/api/events/{category.EventId}/categories/{category.EventCategoryId}/route";

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, string path) => method switch
    {
        "POST" => client.PostAsJsonAsync(path, Details()), "PUT" => client.PutAsJsonAsync(path, Details()),
        "DELETE" => client.DeleteAsync(path), _ => client.GetAsync(path)
    };

    private static Dictionary<string, object?> Details() => new()
    {
        ["routeName"] = " Stadium Loop ", ["startLocation"] = " Gate A ", ["finishLocation"] = " Finish line ",
        ["routeDescription"] = " Follow the marked course. ", ["routeMapUrl"] = " https://example.com/routes/stadium ",
        ["elevationGainMetres"] = 85
    };

    private async Task<EventCategory> AddCategory(string status = "Draft", bool available = true, int owner = 1)
    {
        var item = new Event
        {
            EventName = "Community Run", OrganiserUserId = owner, EventType = "Running", Description = "A community event.",
            DistanceKm = 10, EventDateTime = Now.UtcDateTime.AddDays(10), EntryClosingDateTime = Now.UtcDateTime.AddDays(3),
            VenueName = "Main Stadium", AddressLine1 = "1 Main Road", City = "Johannesburg", Province = "Gauteng",
            Status = status, CreatedAt = Now.UtcDateTime.AddDays(-10)
        };
        var category = new EventCategory
        {
            Event = item, CategoryName = "10 km Run", DistanceKm = 10, EntryFee = 150, IsAvailable = available,
            CreatedAt = Now.UtcDateTime.AddDays(-5)
        };
        await using var context = database.CreateContext();
        context.EventCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private async Task<EventRoute> AddRoute(EventCategory category)
    {
        var route = new EventRoute
        {
            EventCategoryId = category.EventCategoryId, RouteName = "Original route", StartLocation = "Old start",
            FinishLocation = "Old finish", RouteDescription = "Original instructions.",
            RouteMapUrl = "https://example.com/original", ElevationGainMetres = 100, CreatedAt = Now.UtcDateTime.AddDays(-1)
        };
        await using var context = database.CreateContext();
        context.EventRoutes.Add(route);
        await context.SaveChangesAsync();
        return route;
    }

    private static async Task AssertSafe(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        foreach (string field in new[] { "passwordHash", "email", "phoneNumber", "participant", "enrolments", "organiserUserId", "recordedByUserId" })
            Assert.DoesNotContain("\"" + field + "\"", json, StringComparison.OrdinalIgnoreCase);
    }
}
