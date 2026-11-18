using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace RaceDay.Web.Tests;

// Private reads, role restrictions and HTTP test hosts: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Results/.
// Accessed: 18 November 2026.
public sealed class ResultTests
{
    internal const string ResultJson = """
        {"resultId":5,"enrolmentId":8,"participantUserId":3,"eventId":1,"eventName":"Soweto Road Race",
         "eventDateTime":"2026-10-04T04:00:00Z","eventCategoryId":1,"categoryName":"21 km Open",
         "categoryDistanceKm":21.1,"bibNumber":"S808","resultStatus":"Completed","finishTimeSeconds":7265,
         "overallPosition":47,"totalFinishers":312,"categoryPosition":12,"notes":"Official result.",
         "recordedAt":"2026-10-04T12:00:00Z","updatedAt":"2026-10-05T09:00:00Z"}
        """;

    [Theory]
    [InlineData("/results")]
    [InlineData("/results/8")]
    public async Task GuestsAreRedirected_AndOrganisersCannotReadParticipantResults(string route)
    {
        using var factory = Factory("Organiser");
        using var client = factory.Client();
        using var guest = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var login = await SignIn(client);
        using var organiser = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Forbidden, organiser.StatusCode);
        Assert.DoesNotContain("Soweto Road Race", await organiser.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PersonalHistory_UsesOnlyTheCurrentCookieAndAllowedPagingFilters()
    {
        using var factory = Factory();
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/results/mine") return Reply(request);
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("RaceDay.Session=results-api-token", request.Headers.GetValues("Cookie").Single());
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Equal(new[] { "page", "pageSize", "resultStatus" }, query.Keys.Order());
            Assert.Equal("2", query["page"]); Assert.Equal("10", query["pageSize"]);
            Assert.Equal("Completed", query["resultStatus"]);
            return Page(JsonNode.Parse(ResultJson)!, 2, 11);
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/results?page=2&resultStatus=%20Completed%20&participantUserId=999&pageSize=100");
        string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Soweto Road Race", body); Assert.Contains("02:01:05", body);
        Assert.Contains("47 of 312 finishers", body); Assert.Contains("21 km Open", body);
        Assert.Contains("04 October 2026, 06:00", body);
        Assert.Contains("resultStatus=Completed", body); Assert.Contains("Page 2 of 2", body);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.DoesNotContain("results-api-token", body);
    }

    [Fact]
    public async Task Details_ShowTheRecordedTimePositionsAndCorrectionWithoutChangingTheResult()
    {
        using var factory = Factory(); using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/results/8");
        string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("02:01:05", body); Assert.Contains("47 of 312 finishers", body);
        Assert.Contains("Category position", body); Assert.Contains("S808", body);
        Assert.Contains("Official result.", body); Assert.Contains("Last corrected", body);
        Assert.Contains("/events/1", body); Assert.Contains("/entries/8", body);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        using var attemptedWrite = await client.PostAsync("/results/8", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, attemptedWrite.StatusCode);
    }

    [Theory]
    [InlineData("DidNotFinish", "Did not finish")]
    [InlineData("DidNotStart", "Did not start")]
    [InlineData("Disqualified", "Disqualified")]
    public async Task NonFinishingOutcomes_HaveNoInventedTimeOrRank(string status, string label)
    {
        var item = JsonNode.Parse(ResultJson)!; item["resultStatus"] = status;
        item["finishTimeSeconds"] = item["overallPosition"] = item["categoryPosition"] = null;
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/enrolments/8/result"
            ? WebApiFactory.Json(item.ToJsonString()) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        string page = await client.GetStringAsync("/results/8");
        Assert.Contains(label, page); Assert.Contains("No official finish time", page); Assert.Contains("Not ranked", page);
        Assert.Contains("312", page); Assert.DoesNotContain("00:00:00", page); Assert.DoesNotContain("47 of", page);
    }

    [Theory]
    [InlineData(90061, "25:01:01")]
    [InlineData(1, "00:00:01")]
    public async Task FinishTimes_PreserveTotalHoursAndSeconds(int seconds, string expected)
    {
        var item = JsonNode.Parse(ResultJson)!; item["finishTimeSeconds"] = seconds; item["overallPosition"] = null;
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/enrolments/8/result"
            ? WebApiFactory.Json(item.ToJsonString()) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        string page = await client.GetStringAsync("/results/8");
        Assert.Contains(expected, page); Assert.Contains("Not assigned", page); Assert.DoesNotContain("0 of", page);
    }

    [Theory]
    [InlineData("", "No official results yet")]
    [InlineData("?resultStatus=DidNotFinish", "No results match this status")]
    [InlineData("?page=5", "No results on this page")]
    public async Task EmptyAndLaterPages_AreSuccessfulReadOnlyResponses(string query, string expected)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/results/mine"
            ? WebApiFactory.Json($$"""{"page":{{(query.Contains("page=5") ? 5 : 1)}},"pageSize":10,"totalCount":0,"totalPages":0,"items":[]}""") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        string body = await client.GetStringAsync("/results" + query); Assert.Contains(expected, body);
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=invalid")]
    [InlineData("?page=2147483648")]
    public async Task InvalidTypedPaging_IsRejectedBeforeReadingHistory(string query)
    {
        using var factory = Factory(); int reads = 0; factory.Reply = request =>
        { if (request.RequestUri!.AbsolutePath == "/api/results/mine") reads++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/results"+query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, reads);
    }

    [Fact]
    public async Task RejectedApiFilters_ShowAHelpfulMessageWithoutRawProblems()
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/results/mine"
            ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("private raw failure") } : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/results?resultStatus=unknown");
        string body = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Choose a valid result status", body); Assert.DoesNotContain("private raw failure", body);
    }

    [Theory]
    [InlineData("/results")]
    [InlineData("/results/8")]
    public async Task ExpiredApiSessions_ClearTheLocalSession(string route)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath.Contains("result")
            ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith("/account/login", response.Headers.Location!.OriginalString);
        using var next = await client.GetAsync("/participant"); Assert.Equal(HttpStatusCode.Redirect, next.StatusCode);
    }

    [Theory]
    [InlineData(403)]
    [InlineData(404)]
    public async Task UnrelatedOrMissingResults_KeepTheirAccessStatus(int status)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/enrolments/8/result"
            ? new HttpResponseMessage((HttpStatusCode)status) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/results/8"); Assert.Equal(status, (int)response.StatusCode);
        Assert.DoesNotContain("Soweto Road Race", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("other-owner")]
    [InlineData("wrong-entry")]
    [InlineData("missing-field")]
    [InlineData("null")]
    [InlineData("invalid-json")]
    [InlineData("unknown-status")]
    [InlineData("missing-time")]
    [InlineData("non-finisher-time")]
    public async Task InconsistentPrivateResponses_AreNeverDisplayed(string scenario)
    {
        var item = JsonNode.Parse(ResultJson)!;
        switch(scenario)
        {
            case "other-owner": item["participantUserId"] = 999; break;
            case "wrong-entry": item["enrolmentId"] = 99; break;
            case "missing-field": item.AsObject().Remove("eventName"); break;
            case "unknown-status": item["resultStatus"] = "Invented"; break;
            case "missing-time": item["finishTimeSeconds"] = null; break;
            case "non-finisher-time": item["resultStatus"] = "DidNotFinish"; break;
        }
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/enrolments/8/result"
            ? WebApiFactory.Json(scenario == "null" ? "null" : scenario == "invalid-json" ? "{" : item.ToJsonString()) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/results/8");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("Soweto Road Race", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("foreign-item")]
    [InlineData("wrong-page")]
    [InlineData("missing-items")]
    [InlineData("wrong-count")]
    public async Task InconsistentHistory_DoesNotLeakAnyItems(string scenario)
    {
        var item = JsonNode.Parse(ResultJson)!;
        if (scenario == "foreign-item") item["participantUserId"] = 999;
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/results/mine"
            ? scenario == "missing-items" ? WebApiFactory.Json("{\"page\":1,\"pageSize\":10,\"totalCount\":0,\"totalPages\":0,\"items\":null}")
                : Page(item, scenario == "wrong-page" ? 2 : 1, scenario == "wrong-count" ? 2 : 1) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/results"); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("Soweto Road Race", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("server")]
    [InlineData("network")]
    [InlineData("timeout")]
    public async Task UnavailableHistory_ShowsAFriendlyError(string failure)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/results/mine" ? failure switch
        {
            "network" => throw new HttpRequestException("private network error"),
            "timeout" => throw new TaskCanceledException("private timeout"),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        } : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync("/results"); string body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Results are temporarily unavailable", body); Assert.DoesNotContain("private network", body); Assert.DoesNotContain("private timeout", body);
    }

    [Fact]
    public async Task OrganiserText_IsEncodedInTheResultPages()
    {
        var item = JsonNode.Parse(ResultJson)!; item["eventName"] = "<script>event</script>"; item["notes"] = "<script>notes</script>";
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/results/mine" ? Page(item)
            : request.RequestUri.AbsolutePath == "/api/enrolments/8/result" ? WebApiFactory.Json(item.ToJsonString()) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        foreach(string route in new[]{"/results", "/results/8"})
        { string body = await client.GetStringAsync(route); Assert.Contains("&lt;script&gt;event&lt;/script&gt;", body); Assert.DoesNotContain("<script>", body); }
    }

    private static WebApiFactory Factory(string role = "Participant") => new()
    {
        Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" ? LoginReply(role) : Reply(request),
        ProfileReply = _ => WebApiFactory.Profile(role)
    };
    private static Task<HttpResponseMessage> SignIn(HttpClient client) => AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
    private static HttpResponseMessage Page(JsonNode item, int page = 1, int count = 1) => WebApiFactory.Json($$"""
        {"page":{{page}},"pageSize":10,"totalCount":{{count}},"totalPages":{{(count+9)/10}},"items":[{{item.ToJsonString()}}]}
        """);
    private static HttpResponseMessage Reply(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch
    {
        "/api/auth/login" => LoginReply(),
        "/api/results/mine" => Page(JsonNode.Parse(ResultJson)!),
        "/api/enrolments/8/result" => WebApiFactory.Json(ResultJson),
        _ => WebApiFactory.Json(WebApiFactory.EventJson)
    };
    private static HttpResponseMessage LoginReply(string role = "Participant")
    {
        var reply = WebApiFactory.Json(System.Text.Json.JsonSerializer.Serialize(new
        {
            sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20),
            user = new { userId = 3, firstName = "Naledi", role, isActive = true }
        }));
        reply.Headers.Add("Set-Cookie", "RaceDay.Session=results-api-token; Path=/; HttpOnly"); return reply;
    }
}
