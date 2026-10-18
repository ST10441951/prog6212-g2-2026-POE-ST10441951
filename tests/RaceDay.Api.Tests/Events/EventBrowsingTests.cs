using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Authentication;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Authentication;
using RaceDay.Api.Tests.Database;
using Xunit;

namespace RaceDay.Api.Tests.Events;

// Real HTTP pipeline, isolated SQL Server data and controlled time: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/testing/testing-with-the-database
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// Accessed: 18 October 2026.
public sealed class EventBrowsingTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2030, 1, 1, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        // This class uses only its fixture's temporary database. Keep the four seeded users for login.
        await using var context = database.CreateContext();
        await context.Results.ExecuteDeleteAsync();
        await context.Enrolments.ExecuteDeleteAsync();
        await context.EventRoutes.ExecuteDeleteAsync();
        await context.EventCategories.ExecuteDeleteAsync();
        await context.Events.ExecuteDeleteAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Browse_ReturnsOnlyFutureOpenAndClosedEventsWithoutLogin()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var open = await AddEvent("Open");
        var closed = await AddEvent("Closed", start: Now.UtcDateTime.AddDays(2));
        foreach (string status in new[] { "Draft", "Cancelled", "Completed" })
            await AddEvent(status);
        await AddEvent("Open", start: Now.UtcDateTime.AddSeconds(-1));
        await AddEvent("Closed", start: Now.UtcDateTime.AddDays(-1));
        await AddEvent("Open", start: Now.UtcDateTime);

        using var response = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = (await response.Content.ReadFromJsonAsync<EventPageResponse>())!;
        Assert.Equal(1, page.Page);
        Assert.Equal(10, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(1, page.TotalPages);
        Assert.Equal(new[] { open.EventId, closed.EventId }, page.Items.Select(x => x.EventId));
        Assert.All(page.Items, x => Assert.Equal(DateTimeKind.Utc, x.EventDateTime.Kind));
        await AssertPublicFields(response);

        using var login = await Login(client, "thabo.ndlovu@example.com");
        using var signedIn = await client.GetAsync("/api/events?status=Draft&organiserUserId=1");
        Assert.Equal(new[] { open.EventId, closed.EventId },
            (await signedIn.Content.ReadFromJsonAsync<EventPageResponse>())!.Items.Select(x => x.EventId));
    }

    [Fact]
    public async Task Browse_PagesInDateAndIdOrderWithTotalsBeforePaging()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var late = await AddEvent(start: Now.UtcDateTime.AddDays(3));
        var early = await AddEvent();
        var tied = await AddEvent();
        var first = (await client.GetFromJsonAsync<EventPageResponse>("/api/events?page=1&pageSize=2"))!;
        var second = (await client.GetFromJsonAsync<EventPageResponse>("/api/events?page=2&pageSize=2"))!;
        Assert.Equal(new[] { early.EventId, tied.EventId }, first.Items.Select(x => x.EventId));
        Assert.Equal(late.EventId, Assert.Single(second.Items).EventId);
        Assert.Equal(3, first.TotalCount);
        Assert.Equal(3, second.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(2, second.TotalPages);
        var beyond = (await client.GetFromJsonAsync<EventPageResponse>("/api/events?page=3&pageSize=2"))!;
        Assert.Empty(beyond.Items);
        Assert.Equal(3, beyond.TotalCount);
        Assert.Equal(2, beyond.TotalPages);
        var large = (await client.GetFromJsonAsync<EventPageResponse>("/api/events?page=2147483647&pageSize=1"))!;
        Assert.Empty(large.Items);
        Assert.Equal(3, large.TotalCount);
        var maximum = (await client.GetFromJsonAsync<EventPageResponse>("/api/events?pageSize=100"))!;
        Assert.Equal(3, maximum.Items.Count);
    }

    [Fact]
    public async Task Browse_ReturnsAnEmptyPageWhenNoEventsMatch()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var page = (await client.GetFromJsonAsync<EventPageResponse>("/api/events"))!;
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
        Assert.Equal(0, page.TotalPages);
    }

    [Theory]
    [InlineData("Running")]
    [InlineData("Walking")]
    [InlineData("Cycling")]
    public async Task Browse_FiltersEachEventTypeIgnoringCaseAndOuterSpaces(string type)
    {
        using var factory = Factory();
        using var client = factory.Client();
        Event? expected = null;
        foreach (string value in new[] { "Running", "Walking", "Cycling" })
        {
            var item = await AddEvent(type: value);
            if (value == type) expected = item;
        }
        string filter = Uri.EscapeDataString(" " + type.ToLowerInvariant() + " ");
        var page = (await client.GetFromJsonAsync<EventPageResponse>("/api/events?eventType=" + filter))!;
        Assert.Equal(expected!.EventId, Assert.Single(page.Items).EventId);
        Assert.Equal(1, page.TotalCount);
    }

    [Theory]
    [InlineData("Eastern Cape")]
    [InlineData("Free State")]
    [InlineData("Gauteng")]
    [InlineData("KwaZulu-Natal")]
    [InlineData("Limpopo")]
    [InlineData("Mpumalanga")]
    [InlineData("Northern Cape")]
    [InlineData("North West")]
    [InlineData("Western Cape")]
    public async Task Browse_FiltersAllNineProvincesIgnoringCaseAndOuterSpaces(string province)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var match = await AddEvent(province: province);
        await AddEvent(province: province == "Gauteng" ? "Western Cape" : "Gauteng");
        string filter = Uri.EscapeDataString(" " + province.ToLowerInvariant() + " ");
        var page = (await client.GetFromJsonAsync<EventPageResponse>("/api/events?province=" + filter))!;
        Assert.Equal(match.EventId, Assert.Single(page.Items).EventId);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task Browse_CombinesFiltersAndIncludesBothUtcDateBoundaries()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var first = await AddEvent(start: new DateTime(2030, 1, 2, 0, 0, 0, DateTimeKind.Utc));
        var last = await AddEvent(start: new DateTime(2030, 1, 2, 23, 59, 59, DateTimeKind.Utc));
        await AddEvent(start: new DateTime(2030, 1, 1, 23, 59, 59, DateTimeKind.Utc));
        await AddEvent(start: new DateTime(2030, 1, 3, 0, 0, 0, DateTimeKind.Utc));
        await AddEvent(type: "Cycling");
        await AddEvent(province: "Western Cape");
        string query = "/api/events?eventType=Running&province=Gauteng&fromDate=2030-01-02&toDate=2030-01-02&pageSize=1";
        var page = (await client.GetFromJsonAsync<EventPageResponse>(query))!;
        Assert.Equal(first.EventId, Assert.Single(page.Items).EventId);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        var second = (await client.GetFromJsonAsync<EventPageResponse>(query + "&page=2"))!;
        Assert.Equal(last.EventId, Assert.Single(second.Items).EventId);
    }

    [Theory]
    [InlineData("fromDate=2030-01-03", 1)]
    [InlineData("toDate=2030-01-02", 1)]
    [InlineData("fromDate=0001-01-01&toDate=9999-12-31", 2)]
    [InlineData("fromDate=9999-12-31", 0)]
    [InlineData("toDate=0001-01-01", 0)]
    public async Task Browse_AcceptsOneSidedAndExtremeDateFilters(string query, int matches)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await AddEvent(start: new DateTime(2030, 1, 2, 12, 0, 0, DateTimeKind.Utc));
        await AddEvent(start: new DateTime(2030, 1, 3, 12, 0, 0, DateTimeKind.Utc));
        var page = (await client.GetFromJsonAsync<EventPageResponse>("/api/events?" + query))!;
        Assert.Equal(matches, page.TotalCount);
        Assert.Equal(matches, page.Items.Count);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=wrong")]
    [InlineData("page=2147483648")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=-1")]
    [InlineData("pageSize=101")]
    [InlineData("pageSize=wrong")]
    [InlineData("page=2147483647&pageSize=100")]
    [InlineData("eventType=Swimming")]
    [InlineData("eventType=%20%20")]
    [InlineData("province=Atlantis")]
    [InlineData("province=%20%20")]
    [InlineData("fromDate=not-a-date")]
    [InlineData("toDate=2030-02-30")]
    [InlineData("fromDate=2030-02-01&toDate=2030-01-01")]
    public async Task Browse_RejectsInvalidFiltersAndPaging(string query)
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var response = await client.GetAsync("/api/events?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(body.RootElement.GetProperty("errors").EnumerateObject());
    }

    [Theory]
    [InlineData("Open")]
    [InlineData("Closed")]
    [InlineData("Completed")]
    public async Task Details_ReturnsPublicAndHistoricalEventsWithOnlyAvailableCategories(string status)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var item = await AddEvent(status, start: Now.UtcDateTime.AddDays(-1));
        var visible = await AddCategory(item.EventId, available: true, route: true);
        var noRoute = await AddCategory(item.EventId, available: true);
        await AddCategory(item.EventId, available: false, route: true);

        using var response = await client.GetAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var details = (await response.Content.ReadFromJsonAsync<EventDetailsResponse>())!;
        Assert.Equal(item.EventId, details.EventId);
        Assert.Equal(item.EventName, details.EventName);
        Assert.Equal(item.Description, details.Description);
        Assert.Equal(item.AddressLine1, details.AddressLine1);
        Assert.Equal(item.VenueName, details.VenueName);
        Assert.Equal(item.City, details.City);
        Assert.Equal(item.Province, details.Province);
        Assert.Equal(item.EventType, details.EventType);
        Assert.Equal(item.DistanceKm, details.DistanceKm);
        Assert.Equal(item.Latitude, details.Latitude);
        Assert.Equal(item.Longitude, details.Longitude);
        Assert.Equal(item.EventDateTime, details.EventDateTime);
        Assert.Equal(item.EntryClosingDateTime, details.EntryClosingDateTime);
        Assert.Equal(DateTimeKind.Utc, details.EventDateTime.Kind);
        Assert.Equal(DateTimeKind.Utc, details.EntryClosingDateTime.Kind);
        Assert.Null(details.BannerImageUrl);
        Assert.Equal(new[] { visible.EventCategoryId, noRoute.EventCategoryId }, details.Categories.Select(x => x.EventCategoryId));
        var category = details.Categories[0];
        Assert.Equal(visible.CategoryName, category.CategoryName);
        Assert.Equal(visible.MinimumAge, category.MinimumAge);
        Assert.Equal(visible.MaximumAge, category.MaximumAge);
        Assert.Equal(visible.EntryFee, category.EntryFee);
        Assert.Equal(visible.Capacity, category.Capacity);
        Assert.Equal(visible.DistanceKm, category.DistanceKm);
        Assert.NotNull(category.Route);
        Assert.Equal(visible.Route!.RouteId, category.Route.RouteId);
        Assert.Equal(visible.Route.RouteName, category.Route.RouteName);
        Assert.Equal(visible.Route.StartLocation, category.Route.StartLocation);
        Assert.Equal(visible.Route.FinishLocation, category.Route.FinishLocation);
        Assert.Equal(visible.Route.RouteDescription, category.Route.RouteDescription);
        Assert.Equal(visible.Route.RouteMapUrl, category.Route.RouteMapUrl);
        Assert.Equal(visible.Route.ElevationGainMetres, category.Route.ElevationGainMetres);
        Assert.Null(details.Categories[1].Route);
        await AssertPublicFields(response);
    }

    [Theory]
    [InlineData("Draft")]
    [InlineData("Cancelled")]
    public async Task Details_OnlyTheOwningOrganiserCanReadNonPublicEvents(string status)
    {
        using var factory = Factory();
        using var anonymous = factory.Client();
        using var owner = factory.Client();
        using var otherOrganiser = factory.Client();
        using var participant = factory.Client();
        var item = await AddEvent(status);
        await AddCategory(item.EventId, available: false, route: true);
        using var ownerLogin = await Login(owner, "thabo.ndlovu@example.com");
        using var otherLogin = await Login(otherOrganiser, "ayesha.khan@example.com");
        using var participantLogin = await Login(participant, "naledi.mokoena@example.com");
        using var hidden = await anonymous.GetAsync($"/api/events/{item.EventId}");
        using var unrelated = await otherOrganiser.GetAsync($"/api/events/{item.EventId}");
        using var restricted = await participant.GetAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unrelated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, restricted.StatusCode);
        using var owned = await owner.GetAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.OK, owned.StatusCode);
        var details = (await owned.Content.ReadFromJsonAsync<EventDetailsResponse>())!;
        Assert.False(Assert.Single(details.Categories).IsAvailable);
        Assert.NotNull(details.Categories[0].Route);
        Assert.True(owned.Headers.CacheControl?.NoStore);
    }

    [Fact]
    public async Task Details_OwnerSeesUnavailableCategoriesButOtherReadersDoNot()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var item = await AddEvent();
        await AddCategory(item.EventId, available: true);
        await AddCategory(item.EventId, available: false);
        var anonymous = (await client.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{item.EventId}"))!;
        Assert.Single(anonymous.Categories);
        using var ownerLogin = await Login(client, "thabo.ndlovu@example.com");
        Assert.Equal(2, (await client.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{item.EventId}"))!.Categories.Count);
        using var otherLogin = await Login(client, "ayesha.khan@example.com");
        Assert.Single((await client.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{item.EventId}"))!.Categories);
        using var participantLogin = await Login(client, "naledi.mokoena@example.com");
        Assert.Single((await client.GetFromJsonAsync<EventDetailsResponse>($"/api/events/{item.EventId}"))!.Categories);
    }

    [Theory]
    [InlineData("expired")]
    [InlineData("loggedOut")]
    [InlineData("inactive")]
    [InlineData("roleChanged")]
    public async Task Details_OptionalSessionMustRemainValidToGrantOwnerAccess(string change)
    {
        using var factory = Factory();
        using var client = factory.Client();
        string email = $"browse-{Guid.NewGuid():N}@example.com";
        using var registered = await client.PostAsJsonAsync("/api/auth/register", new
        {
            firstName = "Event", lastName = "Owner", email, password = "EventOwner!2030", role = "Organiser"
        });
        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        int ownerId = (await registered.Content.ReadFromJsonAsync<AccountResponse>())!.UserId;
        var hidden = await AddEvent("Draft", owner: ownerId);
        var visible = await AddEvent(owner: ownerId);
        await AddCategory(visible.EventId, available: false);
        using var login = await Login(client, email, "EventOwner!2030");
        using var initial = await client.GetAsync($"/api/events/{hidden.EventId}");
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        if (change == "expired")
            factory.Clock.Advance(TimeSpan.FromMinutes(20));
        else if (change == "loggedOut")
        {
            using var logout = await client.PostAsync("/api/auth/logout", null);
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        }
        else
        {
            await using var context = database.CreateContext();
            var user = (await context.Users.FindAsync(ownerId))!;
            if (change == "inactive") user.IsActive = false;
            else user.RoleId = 2;
            await context.SaveChangesAsync();
        }
        using var denied = await client.GetAsync($"/api/events/{hidden.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        using var publicResponse = await client.GetAsync($"/api/events/{visible.EventId}");
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.Empty((await publicResponse.Content.ReadFromJsonAsync<EventDetailsResponse>())!.Categories);
    }

    [Fact]
    public async Task Details_ForgedCookiesAndIdentityValuesCannotRevealPrivateEvents()
    {
        using var factory = Factory();
        using var client = factory.Client(cookies: false);
        client.DefaultRequestHeaders.Add("Cookie", "RaceDay.Session=forged");
        client.DefaultRequestHeaders.Add("X-User-Id", "1");
        client.DefaultRequestHeaders.Add("X-Role", "Organiser");
        var hidden = await AddEvent("Draft");
        var visible = await AddEvent();
        using var denied = await client.GetAsync($"/api/events/{hidden.EventId}?organiserUserId=1");
        using var allowed = await client.GetAsync($"/api/events/{visible.EventId}");
        using var list = await client.GetAsync("/api/events");
        Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("2147483647")]
    [InlineData("2147483648")]
    [InlineData("not-an-id")]
    public async Task Details_Returns404ForMissingOrInvalidIdentifiers(string id)
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var response = await client.GetAsync("/api/events/" + id);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Details_ReadsCurrentSqlValuesAndDoesNotLeakAnOwnersCachedResponse()
    {
        using var factory = Factory();
        using var client = factory.Client();
        var item = await AddEvent();
        using var first = await client.GetAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await using (var context = database.CreateContext())
            await context.Events.Where(x => x.EventId == item.EventId).ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.Status, "Draft"));
        using var hidden = await client.GetAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);
        using var login = await Login(client, "thabo.ndlovu@example.com");
        using var owned = await client.GetAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.OK, owned.StatusCode);
        Assert.True(owned.Headers.CacheControl?.NoStore);
        using var logout = await client.PostAsync("/api/auth/logout", null);
        using var signedOut = await client.GetAsync($"/api/events/{item.EventId}");
        Assert.Equal(HttpStatusCode.NotFound, signedOut.StatusCode);
    }

    [Fact]
    public async Task Swagger_DescribesPublicBrowsingFiltersAndOptionalOwnerLogin()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        var browse = paths.GetProperty("/api/events").GetProperty("get");
        var details = paths.GetProperty("/api/events/{eventId}").GetProperty("get");
        foreach (var operation in new[] { browse, details })
        {
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("responses").TryGetProperty("200", out _));
        }
        Assert.True(browse.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(details.GetProperty("responses").TryGetProperty("404", out _));
        var parameters = browse.GetProperty("parameters").EnumerateArray().ToDictionary(x => x.GetProperty("name").GetString()!);
        Assert.Equal(new[] { "eventType", "fromDate", "page", "pageSize", "province", "toDate" }, parameters.Keys.Order());
        Assert.Equal(1, parameters["page"].GetProperty("schema").GetProperty("default").GetInt32());
        Assert.Equal(10, parameters["pageSize"].GetProperty("schema").GetProperty("default").GetInt32());
        Assert.False(browse.TryGetProperty("security", out _));
        Assert.Empty(details.GetProperty("security")[0].EnumerateObject());
        Assert.True(details.GetProperty("security")[1].TryGetProperty("SessionCookie", out _));
    }

    private AuthenticationApiFactory Factory()
    {
        var factory = new AuthenticationApiFactory(database.ConnectionString);
        factory.Clock.Advance(Now - factory.Clock.GetUtcNow());
        return factory;
    }

    private async Task<Event> AddEvent(string status = "Open", DateTime? start = null,
        string type = "Running", string province = "Gauteng", int owner = 1)
    {
        var item = new Event
        {
            OrganiserUserId = owner, EventName = $"Browse {Guid.NewGuid():N}", EventType = type,
            Description = "A local community event.", DistanceKm = 10,
            EventDateTime = start ?? Now.UtcDateTime.AddDays(1),
            EntryClosingDateTime = (start ?? Now.UtcDateTime.AddDays(1)).AddDays(-1),
            VenueName = "Community Stadium", AddressLine1 = "1 Main Road", City = "Johannesburg",
            Province = province, Latitude = -26.2041m, Longitude = 28.0473m, Status = status,
            BannerImageBlobName = "private-storage/banner.jpg", CreatedAt = Now.UtcDateTime.AddDays(-10)
        };
        await using var context = database.CreateContext();
        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item;
    }

    private async Task<EventCategory> AddCategory(int eventId, bool available, bool route = false)
    {
        var category = new EventCategory
        {
            EventId = eventId, CategoryName = $"Category {Guid.NewGuid():N}", DistanceKm = 10,
            MinimumAge = 18, MaximumAge = 60, EntryFee = 150, Capacity = 100, IsAvailable = available,
            CreatedAt = Now.UtcDateTime.AddDays(-9)
        };
        if (route)
            category.Route = new EventRoute
            {
                RouteName = "Community Loop", StartLocation = "Main gate", FinishLocation = "Stadium track",
                RouteDescription = "Follow the marked loop.", RouteMapUrl = "https://example.com/route-map",
                ElevationGainMetres = 45, CreatedAt = Now.UtcDateTime.AddDays(-8)
            };
        await using var context = database.CreateContext();
        context.EventCategories.Add(category);
        await context.SaveChangesAsync();
        return category;
    }

    private static async Task<HttpResponseMessage> Login(HttpClient client, string email, string password = "RaceDayDemo!2026")
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response;
    }

    private static async Task AssertPublicFields(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        foreach (string field in new[] { "passwordHash", "password", "email", "phoneNumber", "dateOfBirth",
                     "organiser", "enrolments", "participant", "bannerImageBlobName", "profilePictureBlobName" })
            Assert.DoesNotContain("\"" + field + "\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("private-storage", json);
    }
}
