using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace RaceDay.Web.Tests;

// Role checks, private HTTP requests and encoded views: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Events/OrganiserDashboardResponse.cs.
// Accessed: 18 November 2026.
public sealed class OrganiserTests
{
    internal const string DashboardJson = """
        {"totalEvents":2,"totalEnrolments":3,"confirmedEnrolments":2,"cancelledEnrolments":1,"upcomingEvents":1,"events":[
          {"eventId":1,"eventName":"Durban Road Race","status":"Completed","eventDateTime":"2026-08-16T04:00:00Z","totalEnrolments":2,"confirmedEnrolments":2,"cancelledEnrolments":0},
          {"eventId":2,"eventName":"Cape Cycle","status":"Open","eventDateTime":"2027-03-14T04:00:00Z","totalEnrolments":1,"confirmedEnrolments":0,"cancelledEnrolments":1}]}
        """;
    internal const string EmptyDashboard = """
        {"totalEvents":0,"totalEnrolments":0,"confirmedEnrolments":0,"cancelledEnrolments":0,"upcomingEvents":0,"events":[]}
        """;

    [Theory]
    [InlineData("/organiser")]
    [InlineData("/organiser/events")]
    public async Task GuestsAndParticipants_CannotReadOrganiserPages(string route)
    {
        using var factory = Factory("Participant"); using var client = factory.Client();
        using var guest = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var login = await SignIn(client); using var participant = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Forbidden, participant.StatusCode);
        Assert.DoesNotContain("Durban Road Race", await participant.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Dashboard_ShowsApiTotalsDatesAndCountsPerOwnedEvent()
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/organiser/dashboard") return Reply(request);
            Assert.Equal(HttpMethod.Get, request.Method); Assert.Empty(request.RequestUri.Query);
            Assert.Equal("RaceDay.Session=organiser-api-token", request.Headers.GetValues("Cookie").Single());
            return WebApiFactory.Json(DashboardJson);
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/organiser?userId=999&organiserUserId=999");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("Welcome, Thabo", page);
        Assert.Contains("3</p>", page); Assert.Contains("2 Confirmed", page); Assert.Contains("1 Cancelled", page);
        Assert.Contains("Durban Road Race", page); Assert.Contains("Cape Cycle", page);
        Assert.Contains("16 August 2026, 06:00", page); Assert.Contains("14 March 2027, 06:00", page);
        Assert.Contains("Saved entries", page); Assert.Contains("/organiser/events", page);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString()); Assert.DoesNotContain("organiser-api-token", page);
    }

    [Theory]
    [InlineData("Draft", false)]
    [InlineData("Cancelled", false)]
    [InlineData("Open", true)]
    [InlineData("Closed", true)]
    [InlineData("Completed", true)]
    public async Task OwnedEvents_IncludeAllStatusesAndOnlyLinkToVisiblePublicPages(string status, bool publicLink)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/mine" ? Events(status) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        string body = await client.GetStringAsync("/organiser/events");
        Assert.Contains("Cape Peninsula Cycle Challenge", body); Assert.Contains(">" + status + "</span>", body);
        Assert.Equal(publicLink, body.Contains("View public event")); Assert.Contains("10 March 2027, 06:00", body);
    }

    [Fact]
    public async Task OwnedEventPaging_ForwardsOnlyTheSupportedFiltersAndRetainsStatus()
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/events/mine") return Reply(request);
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Equal(new[] { "page", "pageSize", "status" }, query.Keys.Order()); Assert.Equal("2", query["page"]);
            Assert.Equal("10", query["pageSize"]); Assert.Equal("Draft", query["status"]);
            Assert.Equal("RaceDay.Session=organiser-api-token", request.Headers.GetValues("Cookie").Single());
            return Events("Draft", 2, 11);
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        string body = await client.GetStringAsync("/organiser/events?page=2&status=%20Draft%20&pageSize=100&organiserUserId=999");
        Assert.Contains("Page 2 of 2", body); Assert.Contains("status=Draft", body);
        Assert.DoesNotContain("organiserUserId", body);
    }

    [Theory]
    [InlineData("/organiser", "No events yet")]
    [InlineData("/organiser/events", "No events yet")]
    [InlineData("/organiser/events?status=Draft", "No events match this status")]
    [InlineData("/organiser/events?page=9", "No events on this page")]
    public async Task EmptyAccountsAndLaterPages_AreSuccessfulResponses(string route, string expected)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath switch
        {
            "/api/organiser/dashboard" => WebApiFactory.Json(EmptyDashboard),
            "/api/events/mine" => WebApiFactory.Json($$"""{"page":{{(route.Contains("page=9") ? 9 : 1)}},"pageSize":10,"totalCount":0,"totalPages":0,"items":[]}"""),
            _ => Reply(request)
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        Assert.Contains(expected, await client.GetStringAsync(route));
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=invalid")]
    [InlineData("?page=2147483648")]
    public async Task InvalidTypedPages_DoNotCallTheOwnedEventEndpoint(string query)
    {
        using var factory = Factory(); int reads = 0; factory.Reply = request =>
        { if (request.RequestUri!.AbsolutePath == "/api/events/mine") reads++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/organiser/events" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, reads);
    }

    [Fact]
    public async Task RejectedStatusFilters_ShowAUsefulErrorWithoutRawApiDetails()
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/mine"
            ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("private failure detail") } : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/organiser/events?status=invalid");
        string body = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Choose a valid event status", body); Assert.DoesNotContain("private failure detail", body);
    }

    [Theory]
    [InlineData("/organiser", 401)]
    [InlineData("/organiser", 403)]
    [InlineData("/organiser/events", 401)]
    [InlineData("/organiser/events", 403)]
    public async Task RejectedPrivateSessions_ClearLocalAccess(string route, int status)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" ? Reply(request) : new HttpResponseMessage((HttpStatusCode)status);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/login", response.Headers.Location!.OriginalString);
        using var next = await client.GetAsync("/organiser"); Assert.Equal(HttpStatusCode.Redirect, next.StatusCode);
    }

    [Theory]
    [InlineData("missing-events")]
    [InlineData("wrong-total")]
    [InlineData("negative-count")]
    [InlineData("duplicate-event")]
    [InlineData("wrong-status")]
    [InlineData("null")]
    [InlineData("invalid-json")]
    public async Task InconsistentDashboardResponses_AreNotDisplayed(string failure)
    {
        var body = JsonNode.Parse(DashboardJson)!;
        switch (failure)
        {
            case "missing-events": body.AsObject().Remove("events"); break;
            case "wrong-total": body["totalEnrolments"] = 9; break;
            case "negative-count": body["events"]![0]!["confirmedEnrolments"] = -1; break;
            case "duplicate-event": body["events"]![1]!["eventId"] = 1; break;
            case "wrong-status": body["events"]![0]!["status"] = "Invented"; break;
        }
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/organiser/dashboard"
            ? WebApiFactory.Json(failure == "null" ? "null" : failure == "invalid-json" ? "{" : body.ToJsonString()) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/organiser"); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("Durban Road Race", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("/organiser", "server")]
    [InlineData("/organiser/events", "network")]
    [InlineData("/organiser/events", "timeout")]
    public async Task UnavailablePrivateReads_Show503WithoutMisleadingZeroTotals(string route, string failure)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" ? Reply(request) : failure switch
        {
            "network" => throw new HttpRequestException("private network information"),
            "timeout" => throw new TaskCanceledException("private timeout information"),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync(route); string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("Your events are temporarily unavailable", body);
        Assert.DoesNotContain("No events yet", body); Assert.DoesNotContain("private network", body); Assert.DoesNotContain("private timeout", body);
    }

    [Fact]
    public async Task OrganiserNames_AreEncodedAndReadsHaveNoWriteAction()
    {
        var dashboard = JsonNode.Parse(DashboardJson)!; dashboard["events"]![0]!["eventName"] = "<script>event()</script>";
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/organiser/dashboard" ? WebApiFactory.Json(dashboard.ToJsonString()) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        string body = await client.GetStringAsync("/organiser"); Assert.Contains("&lt;script&gt;event()&lt;/script&gt;", body); Assert.DoesNotContain("<script>", body);
        using var attemptedWrite = await client.PostAsync("/organiser/events", new FormUrlEncodedContent([])); Assert.Equal(HttpStatusCode.MethodNotAllowed, attemptedWrite.StatusCode);
    }

    private static WebApiFactory Factory(string role = "Organiser") => new()
    {
        Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" ? Login(role) : Reply(request),
        ProfileReply = _ => WebApiFactory.Profile(role, "Thabo", 1)
    };
    private static Task<HttpResponseMessage> SignIn(HttpClient client) => AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
    private static HttpResponseMessage Events(string status = "Open", int page = 1, int count = 1) => WebApiFactory.Json($$"""
        {"page":{{page}},"pageSize":10,"totalCount":{{count}},"totalPages":{{(count + 9) / 10}},"items":[
        {"eventId":2,"eventName":"Cape Peninsula Cycle Challenge","eventType":"Cycling","distanceKm":80,
         "eventDateTime":"2027-03-10T04:00:00Z","city":"Cape Town","province":"Western Cape","status":"{{status}}"}]}
        """);
    private static HttpResponseMessage Reply(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch
    {
        "/api/auth/login" => Login(),
        "/api/organiser/dashboard" => WebApiFactory.Json(DashboardJson),
        "/api/events/mine" => Events(),
        _ => WebApiFactory.Json(WebApiFactory.EventJson)
    };
    private static HttpResponseMessage Login(string role = "Organiser")
    {
        var response = WebApiFactory.Json(JsonSerializer.Serialize(new { sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20), user = new { userId = 1, firstName = "Thabo", role, isActive = true } }));
        response.Headers.Add("Set-Cookie", "RaceDay.Session=organiser-api-token; Path=/; HttpOnly"); return response;
    }
}
