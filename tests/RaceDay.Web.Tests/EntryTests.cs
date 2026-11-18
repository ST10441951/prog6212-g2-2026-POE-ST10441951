using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace RaceDay.Web.Tests;

// Participant form, ownership and API failure checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Enrolments/.
// Accessed: 18 November 2026.
public sealed class EntryTests
{
    internal const string EntryJson = """
        {"enrolmentId":12,"participantUserId":3,"participantName":"Naledi Mokoena",
         "eventId":2,"eventName":"Cape Peninsula Cycle Challenge","eventDateTime":"2027-03-14T04:00:00Z",
         "eventCategoryId":3,"categoryName":"40 km Cycle","categoryDistanceKm":40,
         "enrolmentDate":"2026-11-18T12:00:00Z","status":"Confirmed","bibNumber":null}
        """;

    [Theory]
    [InlineData("/entries")]
    [InlineData("/entries/12")]
    [InlineData("/events/2/enter")]
    public async Task PrivateEntryPages_RejectGuestsAndOrganisers(string route)
    {
        using var factory = Factory("Organiser");
        using var client = factory.Client();
        using var guest = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var login = await SignIn(client);
        using var forbidden = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        if (route.EndsWith("enter"))
        {
            using var post = await client.PostAsync(route, new FormUrlEncodedContent([]));
            Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        }
    }

    [Fact]
    public async Task EnterForm_ShowsApiCategoriesAndRequiresAntiforgeryBeforeWrites()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        string page = await client.GetStringAsync("/events/2/enter");
        Assert.Contains("40 km Cycle", page);
        Assert.Contains("__RequestVerificationToken", page);
        Assert.Contains("name=\"EventCategoryId\"", page);
        using var response = await client.PostAsync("/events/2/enter", new FormUrlEncodedContent(new Dictionary<string, string> { ["EventCategoryId"] = "3" }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Creation_SendsOnlyRouteEventAndSelectedCategoryThenShowsTheSavedApiRecord()
    {
        using var factory = Factory();
        JsonElement? sent = null;
        int saves = 0;
        factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/enrolments")
            {
                Assert.Equal("RaceDay.Session=participant-api-token", request.Headers.GetValues("Cookie").Single());
                sent = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement.Clone();
                saves++;
                var response = WebApiFactory.Json(EntryJson);
                response.StatusCode = HttpStatusCode.Created;
                response.Headers.Location = new Uri("https://untrusted.example/ignored");
                return response;
            }
            return Reply(factory, request);
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, "/events/2/enter", new()
        {
            ["EventCategoryId"] = "3", ["EventId"] = "999", ["ParticipantUserId"] = "999", ["Status"] = "Cancelled"
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/entries/12", response.Headers.Location?.OriginalString);
        Assert.Equal(new[] { "eventCategoryId", "eventId" }, sent!.Value.EnumerateObject().Select(item => item.Name).Order());
        Assert.Equal(2, sent.Value.GetProperty("eventId").GetInt32());
        Assert.Equal(3, sent.Value.GetProperty("eventCategoryId").GetInt32());
        using var detail = await client.GetAsync("/entries/12");
        string page = await detail.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        Assert.Contains("Your entry has been saved", page);
        Assert.Contains("Confirmed", page);
        Assert.Contains("40 km Cycle", page);
        Assert.Contains("Naledi Mokoena", page);
        Assert.Contains("no-store", detail.Headers.CacheControl!.ToString());
        Assert.DoesNotContain("participant-api-token", page);
        Assert.DoesNotContain("Your entry has been saved", await client.GetStringAsync("/entries/12"));
        Assert.Equal(1, saves);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("invalid")]
    public async Task InvalidSelections_Return400WithoutPostingToTheApi(string category)
    {
        using var factory = Factory();
        int saves = 0;
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/enrolments" && request.Method == HttpMethod.Post) saves++;
            return Reply(factory, request);
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, "/events/2/enter", new() { ["EventCategoryId"] = category });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, saves);
        Assert.Contains("field-validation-error", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(400, "A date of birth is required for this category")]
    [InlineData(400, "Your age on the event date is outside the category limits")]
    [InlineData(404, "Category not found")]
    [InlineData(409, "This category is full")]
    [InlineData(409, "This event is not Open or entries have closed")]
    [InlineData(409, "An entry already exists for this event")]
    [InlineData(409, "<script>private()</script>")]
    public async Task ApiRejections_KeepTheSelectionAndDisplayEncodedReasons(int status, string title)
    {
        using var factory = Factory();
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/enrolments"
            ? Problem(status, title) : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, "/events/2/enter", new() { ["EventCategoryId"] = "3" });
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Contains(WebUtility.HtmlEncode(title), page);
        string option = Regex.Match(page, "<option[^>]*value=\"3\"[^>]*>").Value;
        Assert.Contains("selected=\"selected\"", option);
        Assert.DoesNotContain("<script>private()", page);
        Assert.DoesNotContain("Your entry has been saved", page);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("network")]
    [InlineData("timeout")]
    [InlineData("missing")]
    [InlineData("other-user")]
    [InlineData("other-event")]
    [InlineData("other-category")]
    [InlineData("unconfirmed")]
    public async Task UnconfirmedWrites_Return503AndTellUsersToCheckTheirEntriesBeforeRetrying(string problem)
    {
        using var factory = Factory();
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/enrolments") return Reply(factory, request);
            if (problem == "network") throw new HttpRequestException("private-network-error");
            if (problem == "timeout") throw new TaskCanceledException("private-timeout");
            if (problem == "server") return Problem(500, "private-server-error");
            var body = JsonNode.Parse(EntryJson)!;
            if (problem == "other-user") body["participantUserId"] = 999;
            if (problem == "other-event") body["eventId"] = 999;
            if (problem == "other-category") body["eventCategoryId"] = 999;
            if (problem == "unconfirmed") body["status"] = "Cancelled";
            var response = WebApiFactory.Json(problem == "missing" ? "{}" : body.ToJsonString());
            response.StatusCode = HttpStatusCode.Created;
            return response;
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, "/events/2/enter", new() { ["EventCategoryId"] = "3" });
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Check My entries before submitting again", page);
        Assert.DoesNotContain("private-", page);
        Assert.DoesNotContain("Your entry has been saved", page);
    }

    [Theory]
    [InlineData("Confirmed")]
    [InlineData("Cancelled")]
    public async Task MyEntries_UsesOnlyTheCurrentSessionAndPreservesPagingStatus(string status)
    {
        using var factory = Factory();
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/enrolments/mine") return Reply(factory, request);
            Assert.Equal("RaceDay.Session=participant-api-token", request.Headers.GetValues("Cookie").Single());
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Equal("2", query["page"]);
            Assert.Equal("10", query["pageSize"]);
            Assert.Equal(status, query["status"]);
            Assert.False(query.ContainsKey("participantUserId"));
            return Page(status, 2, 21);
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.GetAsync($"/entries?page=2&status={status}&participantUserId=999");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("href=\"/entries/12\"", page);
        Assert.Contains(status, page);
        Assert.Contains("status=" + status, page);
        Assert.Contains("Page 2 of 3", page);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.DoesNotContain("participant-api-token", page);
    }

    [Theory]
    [InlineData("/entries")]
    [InlineData("/entries/12")]
    public async Task OtherUsersInApiResponses_AreNeverDisplayed(string route)
    {
        using var factory = Factory();
        var item = JsonNode.Parse(EntryJson)!;
        item["participantUserId"] = 999;
        item["participantName"] = "Private Other Person";
        factory.Reply = request => request.RequestUri!.AbsolutePath.StartsWith("/api/enrolments")
            ? WebApiFactory.Json(route == "/entries" ? JsonSerializer.Serialize(new
            {
                page = 1, pageSize = 10, totalCount = 1, totalPages = 1, items = new[] { item }
            }) : item.ToJsonString()) : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("Private Other Person", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(503)]
    public async Task PrivateReadFailures_ReturnTheCorrectStatusWithoutExposingApiErrors(int status)
    {
        using var factory = Factory();
        factory.Reply = request => request.RequestUri!.AbsolutePath.StartsWith("/api/enrolments")
            ? Problem(status, "private-api-details") : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.GetAsync("/entries/12");
        Assert.Equal(status == 401 ? HttpStatusCode.Redirect : (HttpStatusCode)status, response.StatusCode);
        Assert.DoesNotContain("private-api-details", await response.Content.ReadAsStringAsync());
        if (status == 401)
        {
            Assert.Contains("/account/login", response.Headers.Location!.OriginalString);
            using var ended = await client.GetAsync("/entries");
            Assert.Equal(HttpStatusCode.Redirect, ended.StatusCode);
        }
    }

    [Fact]
    public async Task EmptyHistory_HasAUsefulStateAndStatusFiltersCanBeCleared()
    {
        using var factory = Factory();
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/enrolments/mine"
            ? WebApiFactory.Json("""{"page":1,"pageSize":10,"totalCount":0,"totalPages":0,"items":[]}""") : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        Assert.Contains("No event entries yet", await client.GetStringAsync("/entries"));
        Assert.Contains("No entries match this status", await client.GetStringAsync("/entries?status=Cancelled"));
    }

    [Theory]
    [InlineData("Closed")]
    [InlineData("Completed")]
    public async Task NonOpenEvents_DoNotDisplayAnEntryForm(string status)
    {
        using var factory = Factory();
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/2"
            ? WebApiFactory.Json(EventBrowsingTests.DetailsJson.Replace("\"Open\"", "\"" + status + "\"")) : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        string page = await client.GetStringAsync("/events/2/enter");
        Assert.Contains("not accepting new entries", page);
        Assert.DoesNotContain("Confirm entry", page);
    }

    private static WebApiFactory Factory(string role = "Participant")
    {
        var factory = new WebApiFactory();
        factory.ProfileReply = _ => WebApiFactory.Profile(role);
        factory.Reply = request => Reply(factory, request, role);
        return factory;
    }

    private static HttpResponseMessage Reply(WebApiFactory factory, HttpRequestMessage request, string role = "Participant")
    {
        string route = request.RequestUri!.AbsolutePath;
        if (route == "/api/auth/login")
        {
            var response = WebApiFactory.Json(JsonSerializer.Serialize(new
            {
                sessionExpiresAt = factory.Clock.GetUtcNow().AddMinutes(20),
                user = new { userId = 3, firstName = "Naledi", role, isActive = true }
            }));
            response.Headers.Add("Set-Cookie", "RaceDay.Session=participant-api-token; path=/");
            return response;
        }
        if (route == "/api/events/2") return WebApiFactory.Json(EventBrowsingTests.DetailsJson);
        if (route == "/api/enrolments/12") return WebApiFactory.Json(EntryJson);
        if (route == "/api/enrolments/mine") return Page("Confirmed", 1, 1);
        return WebApiFactory.Json(WebApiFactory.EventJson);
    }

    private static HttpResponseMessage Page(string status, int page, int count) => WebApiFactory.Json(JsonSerializer.Serialize(new
    {
        page, pageSize = 10, totalCount = count, totalPages = (count + 9) / 10,
        items = new[] { JsonNode.Parse(EntryJson.Replace("\"Confirmed\"", "\"" + status + "\"")) }
    }));

    private static HttpResponseMessage Problem(int status, string title) => new((HttpStatusCode)status)
    {
        Content = WebApiFactory.Json(JsonSerializer.Serialize(new { title, status })).Content
    };

    private static Task<HttpResponseMessage> SignIn(HttpClient client) =>
        AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
}
