using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace RaceDay.Web.Tests;

// Private rosters, field binding and protected writes: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Enrolments/.
// Accessed: 18 November 2026.
public sealed class RosterTests
{
    private const string Root = "/organiser/events/2/enrolments";
    internal const string EntryJson = """
        {"enrolmentId":8,"participantUserId":3,"participantName":"Naledi Mokoena","eventId":2,
         "eventName":"Cape Cycle","eventDateTime":"2027-03-14T04:00:00Z","eventCategoryId":3,
         "categoryName":"40 km Cycle","categoryDistanceKm":40,"enrolmentDate":"2026-11-18T09:30:00Z",
         "status":"Confirmed","bibNumber":null,"updatedAt":null,"result":null}
        """;

    [Theory]
    [InlineData("")]
    [InlineData("/8")]
    [InlineData("/8/bib")]
    public async Task GuestsAndParticipants_CannotOpenOrganiserEnrolmentPages(string suffix)
    {
        using var factory = Factory("Participant"); using var client = factory.Client(); using var guest = await client.GetAsync(Root + suffix); Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var login = await SignIn(client); using var denied = await client.GetAsync(Root + suffix); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task RosterPaging_ForwardsOnlyTheEventFiltersAndCurrentCookie()
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/events/2/enrolments") return Reply(request);
            Assert.Equal("RaceDay.Session=roster-api-token", request.Headers.GetValues("Cookie").Single());
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query); Assert.Equal(new[] { "eventCategoryId", "page", "pageSize", "status" }, query.Keys.Order());
            Assert.Equal("3", query["eventCategoryId"]); Assert.Equal("2", query["page"]); Assert.Equal("10", query["pageSize"]); Assert.Equal("Confirmed", query["status"]);
            return Page(2, 11);
        };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root + "?page=2&status=%20Confirmed%20&eventCategoryId=3&organiserUserId=999&pageSize=100"); string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("Naledi Mokoena", body); Assert.Contains("40 km Cycle", body); Assert.Contains("eventCategoryId=3", body); Assert.Contains("status=Confirmed", body); Assert.Contains("Page 2 of 2", body); Assert.Contains("no-store", response.Headers.CacheControl!.ToString()); Assert.DoesNotContain("roster-api-token", body);
    }

    [Theory]
    [InlineData("", "No enrolments yet")]
    [InlineData("?status=Cancelled", "No enrolments match these filters")]
    [InlineData("?page=4", "No enrolments on this page")]
    public async Task EmptyAndLaterPages_AreNotServiceFailures(string query, string expected)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/2/enrolments" ? WebApiFactory.Json($$"""{"page":{{(query.Contains("page=4") ? 4 : 1)}},"pageSize":10,"totalCount":0,"totalPages":0,"items":[]}""") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); Assert.Contains(expected, await client.GetStringAsync(Root + query));
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=invalid")]
    [InlineData("?eventCategoryId=0")]
    [InlineData("?eventCategoryId=invalid")]
    public async Task InvalidTypedFilters_DoNotReadTheRoster(string query)
    {
        using var factory = Factory(); int reads = 0; factory.Reply = request => { if (request.RequestUri!.AbsolutePath == "/api/events/2/enrolments") reads++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root + query); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, reads);
    }

    [Fact]
    public async Task ApiRejectedFilters_ShowOnlyTheFriendlyExplanation()
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/2/enrolments" ? Problem(400, "private filter failure") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root + "?eventCategoryId=999"); string page = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("Choose a valid entry status", page); Assert.DoesNotContain("private filter failure", page);
    }

    [Fact]
    public async Task Details_ShowOnlyTheRequiredParticipantEntryData()
    {
        using var factory = Factory(); using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root + "/8"); string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("Naledi Mokoena", body); Assert.Contains("Confirmed", body); Assert.Contains("Not assigned", body); Assert.Contains("18 November 2026, 11:30", body); Assert.Contains("Assign or edit bib", body); Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task BibWrites_SendOnlyBibNumberAndUseTheRouteEntry()
    {
        using var factory = Factory(); bool saved = false; factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Put) return Reply(request); saved = true; Assert.Equal("/api/enrolments/8/bib-number", request.RequestUri!.AbsolutePath); Assert.Equal("RaceDay.Session=roster-api-token", request.Headers.GetValues("Cookie").Single());
            var body = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!; Assert.Single(body.AsObject()); Assert.Equal(" a-103 ", body["bibNumber"]!.GetValue<string>());
            var entry = JsonNode.Parse(EntryJson)!; entry["bibNumber"] = "A-103"; return WebApiFactory.Json(entry.ToJsonString());
        };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/bib", new() { ["BibNumber"] = " a-103 ", ["EnrolmentId"] = "999", ["EventId"] = "999", ["Status"] = "Cancelled", ["ParticipantUserId"] = "999" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal(Root + "/8", response.Headers.Location?.OriginalString); Assert.True(saved);
    }

    [Fact]
    public async Task MissingAntiforgery_PreventsBibAssignment()
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; return Reply(request); }; using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.PostAsync(Root + "/8/bib", new FormUrlEncodedContent(new Dictionary<string, string> { { "BibNumber", "A1" } })); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123456789012345678901")]
    public async Task InvalidBibLength_DoesNotWrite(string bib)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; return Reply(request); }; using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/bib", new() { ["BibNumber"] = bib }); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(409)]
    public async Task RejectedBibAssignments_KeepTheValueAndApiExplanation(int status)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? Problem(status, "This bib number <already> exists") : Reply(request); using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/bib", new() { ["BibNumber"] = "A-103" }); string page = await response.Content.ReadAsStringAsync(); Assert.Equal(status, (int)response.StatusCode); Assert.Contains("A-103", page); Assert.Contains("This bib number &lt;already&gt; exists", page);
    }

    [Theory]
    [InlineData("foreign-event")]
    [InlineData("null-item")]
    [InlineData("invalid-json")]
    [InlineData("wrong-page")]
    public async Task MalformedPrivateRosters_AreNeverDisplayed(string failure)
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/events/2/enrolments") return Reply(request); var entry = JsonNode.Parse(EntryJson)!; if (failure == "foreign-event") entry["eventId"] = 99;
            return failure switch { "invalid-json" => WebApiFactory.Json("{"), "wrong-page" => Page(2, 11), _ => WebApiFactory.Json("{\"page\":1,\"pageSize\":10,\"totalCount\":1,\"totalPages\":1,\"items\":[" + (failure == "null-item" ? "null" : entry.ToJsonString()) + "]}") };
        };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.DoesNotContain("Naledi Mokoena", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EnrolmentFromAnotherEvent_CannotOpenOrSubmitTheBibForm()
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; if (request.RequestUri!.AbsolutePath != "/api/enrolments/8") return Reply(request); var entry = JsonNode.Parse(EntryJson)!; entry["eventId"] = 1; return WebApiFactory.Json(entry.ToJsonString()); };
        using var client = factory.Client(); using var login = await SignIn(client); using var get = await client.GetAsync(Root + "/8/bib"); Assert.Equal(HttpStatusCode.NotFound, get.StatusCode); using var post = await AccountTests.PostForm(client, Root + "/8/bib", new() { ["BibNumber"] = "A1" }); Assert.Equal(HttpStatusCode.NotFound, post.StatusCode); Assert.Equal(0, writes);
    }

    [Fact]
    public async Task AnUnownedEvent_DoesNotRevealItsRoster()
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/organiser/dashboard" ? WebApiFactory.Json(OrganiserTests.EmptyDashboard) : Reply(request); using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.DoesNotContain("Naledi Mokoena", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("server")]
    [InlineData("network")]
    [InlineData("wrong-identity")]
    public async Task UnconfirmedBibWrites_AdviseReadingTheSavedEntry(string failure)
    {
        using var factory = Factory(); factory.Reply = request =>
        { if (request.Method != HttpMethod.Put) return Reply(request); if (failure == "network") throw new HttpRequestException("private failure"); if (failure == "server") return new HttpResponseMessage(HttpStatusCode.InternalServerError); var entry = JsonNode.Parse(EntryJson)!; entry["participantUserId"] = 999; entry["bibNumber"] = "A1"; return WebApiFactory.Json(entry.ToJsonString()); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/bib", new() { ["BibNumber"] = "A1" }); string page = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("Check the saved enrolment before trying again", page); Assert.DoesNotContain("private failure", page); Assert.DoesNotContain("bib number has been saved", page);
    }

    [Fact]
    public async Task ExpiredApiSessions_ClearTheLocalLogin()
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : Reply(request); using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/bib", new() { ["BibNumber"] = "A1" }); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); using var next = await client.GetAsync(Root); Assert.Equal(HttpStatusCode.Redirect, next.StatusCode);
    }

    [Fact]
    public async Task ParticipantNames_AreEncodedInRosterAndDetails()
    {
        using var factory = Factory(); var entry = JsonNode.Parse(EntryJson)!; entry["participantName"] = "<script>name()</script>"; factory.Reply = request => request.RequestUri!.AbsolutePath switch { "/api/events/2/enrolments" => WebApiFactory.Json("{\"page\":1,\"pageSize\":10,\"totalCount\":1,\"totalPages\":1,\"items\":[" + entry.ToJsonString() + "]}"), "/api/enrolments/8" => WebApiFactory.Json(entry.ToJsonString()), _ => Reply(request) };
        using var client = factory.Client(); using var login = await SignIn(client); foreach (string route in new[] { Root, Root + "/8" }) { string page = await client.GetStringAsync(route); Assert.Contains("&lt;script&gt;name()&lt;/script&gt;", page); Assert.DoesNotContain("<script>", page); }
    }

    private static WebApiFactory Factory(string role = "Organiser") => new() { Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" ? Login(role) : Reply(request), ProfileReply = _ => WebApiFactory.Profile(role, "Thabo", 1) };
    private static Task<HttpResponseMessage> SignIn(HttpClient client) => AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
    private static HttpResponseMessage Page(int page = 1, int count = 1) => WebApiFactory.Json($$"""{"page":{{page}},"pageSize":10,"totalCount":{{count}},"totalPages":{{(count + 9) / 10}},"items":[{{EntryJson}}]}""");
    private static HttpResponseMessage Problem(int status, string title) => new((HttpStatusCode)status) { Content = new StringContent(JsonSerializer.Serialize(new { title }), System.Text.Encoding.UTF8, "application/problem+json") };
    private static HttpResponseMessage Reply(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch { "/api/auth/login" => Login(), "/api/organiser/dashboard" => WebApiFactory.Json(OrganiserTests.DashboardJson), "/api/events/2" => WebApiFactory.Json(EventBrowsingTests.DetailsJson), "/api/events/2/enrolments" => Page(), "/api/enrolments/8" => WebApiFactory.Json(EntryJson), _ => WebApiFactory.Json(WebApiFactory.EventJson) };
    private static HttpResponseMessage Login(string role = "Organiser") { var response = WebApiFactory.Json(JsonSerializer.Serialize(new { sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20), user = new { userId = 1, firstName = "Thabo", role, isActive = true } })); response.Headers.Add("Set-Cookie", "RaceDay.Session=roster-api-token; Path=/; HttpOnly"); return response; }
}
