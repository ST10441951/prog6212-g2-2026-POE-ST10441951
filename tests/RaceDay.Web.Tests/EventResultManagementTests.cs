using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace RaceDay.Web.Tests;

// Private result forms, explicit writes and API failures: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Results/.
// Accessed: 19 November 2026.
public sealed class EventResultManagementTests
{
    private const string Root = "/organiser/events/2/results";

    [Theory]
    [InlineData("")]
    [InlineData("/8")]
    [InlineData("/8/capture")]
    [InlineData("/8/edit")]
    public async Task GuestsAndParticipants_CannotOpenOrganiserResults(string suffix)
    {
        using var factory = Factory("Participant"); using var client = factory.Client();
        using var guest = await client.GetAsync(Root + suffix); Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var login = await SignIn(client); using var denied = await client.GetAsync(Root + suffix); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Fact]
    public async Task List_ForwardsOnlyAllowedFiltersAndPrivateCookie()
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/events/2/results") return Reply(request);
            Assert.Equal("RaceDay.Session=official-result-token", request.Headers.GetValues("Cookie").Single());
            var query = QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Equal(new[] { "eventCategoryId", "page", "pageSize", "resultStatus" }, query.Keys.Order());
            Assert.Equal("2", query["page"]); Assert.Equal("10", query["pageSize"]); Assert.Equal("Completed", query["resultStatus"]); Assert.Equal("3", query["eventCategoryId"]);
            return Page(2, 11);
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync(Root + "?page=2&resultStatus=%20Completed%20&eventCategoryId=3&participantUserId=999&pageSize=100");
        string html = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("Naledi Mokoena", html); Assert.Contains("02:01:05", html); Assert.Contains("47 of 312 finishers", html); Assert.Contains("eventCategoryId=3", html); Assert.Contains("resultStatus=Completed", html); Assert.Contains("Page 2 of 2", html); Assert.Contains("no-store", response.Headers.CacheControl!.ToString()); Assert.DoesNotContain("official-result-token", html);
    }

    [Theory]
    [InlineData("", "No official results yet")]
    [InlineData("?resultStatus=DidNotFinish", "No results match these filters")]
    [InlineData("?page=4", "No results on this page")]
    public async Task EmptyAndLaterLists_HaveRecoveryMessages(string query, string expected)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/2/results" ? WebApiFactory.Json($$"""{"page":{{(query.Contains("page=4") ? 4 : 1)}},"pageSize":10,"totalCount":0,"totalPages":0,"items":[]}""") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); Assert.Contains(expected, await client.GetStringAsync(Root + query));
    }

    [Theory]
    [InlineData("?page=0")]
    [InlineData("?page=wrong")]
    [InlineData("?eventCategoryId=0")]
    public async Task InvalidTypedFilters_DoNotReadResults(string query)
    {
        using var factory = Factory(); int reads = 0; factory.Reply = request => { if (request.RequestUri!.AbsolutePath == "/api/events/2/results") reads++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root + query); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, reads);
    }

    [Fact]
    public async Task RejectedApiFilters_UseFriendlyMessage()
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/2/results" ? Problem(400, "private-filter-error") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root + "?eventCategoryId=999"); string html = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("Choose a valid result status", html); Assert.DoesNotContain("private-filter-error", html);
    }

    [Fact]
    public async Task Capture_ConvertsElapsedTimeAndSendsOnlyEditableFields()
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Post || !request.RequestUri!.AbsolutePath.EndsWith("/result")) return Reply(request);
            writes++; Assert.Equal("/api/enrolments/8/result", request.RequestUri.AbsolutePath); Assert.Equal("RaceDay.Session=official-result-token", request.Headers.GetValues("Cookie").Single());
            var data = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!;
            Assert.Equal(new[] { "categoryPosition", "finishTimeSeconds", "notes", "overallPosition", "resultStatus" }, data.AsObject().Select(field => field.Key).Order());
            Assert.Equal(90001, data["finishTimeSeconds"]!.GetValue<int>()); return Saved(request);
        };
        using var client = factory.Client(); using var login = await SignIn(client); var fields = Fields("25:00:01"); fields["ResultId"] = "999"; fields["Entry.EventId"] = "999"; fields["RecordedByUserId"] = "999"; fields["Original.ResultId"] = "999";
        using var response = await AccountTests.PostForm(client, Root + "/8/capture", fields); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal(Root + "/8", response.Headers.Location?.OriginalString); Assert.Equal(1, writes);
    }

    [Fact]
    public async Task Correction_ReadsSavedContextAndUsesPut()
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Put) return Reply(request);
            writes++; Assert.Equal("/api/enrolments/8/result", request.RequestUri!.AbsolutePath); return Saved(request);
        };
        using var client = factory.Client(); using var login = await SignIn(client); string form = await client.GetStringAsync(Root + "/8/edit"); Assert.Contains("02:01:05", form); Assert.Contains("Official result.", form);
        using var response = await AccountTests.PostForm(client, Root + "/8/edit", Fields()); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal(1, writes);
    }

    [Theory]
    [InlineData("DidNotFinish")]
    [InlineData("DidNotStart")]
    [InlineData("Disqualified")]
    public async Task NonFinishingResults_SendNullTimeAndPositions(string status)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? Saved(request) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/edit", new() { ["ResultStatus"] = status, ["Notes"] = "  Corrected outcome  " }); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Theory]
    [InlineData("01:60:00")]
    [InlineData("-1:00:00")]
    [InlineData("01:00")]
    [InlineData("999999:59:59")]
    public async Task InvalidElapsedFormat_DoesNotWrite(string time)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.RequestUri!.AbsolutePath.EndsWith("/result") && request.Method != HttpMethod.Get) writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/capture", Fields(time)); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("Use HH:MM:SS", await response.Content.ReadAsStringAsync()); Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("/capture")]
    [InlineData("/edit")]
    public async Task MissingAntiforgery_PreventsResultChanges(string suffix)
    {
        using var factory = Factory(); using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.PostAsync(Root + "/8" + suffix, new FormUrlEncodedContent(Fields())); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("OverallPosition", "0")]
    [InlineData("CategoryPosition", "bad")]
    [InlineData("ResultStatus", "")]
    public async Task InvalidFormFields_DoNotSubmit(string field, string value)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); var fields = Fields(); fields[field] = value; using var response = await AccountTests.PostForm(client, Root + "/8/edit", fields); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(409)]
    public async Task ApiValidationAndConflicts_PreserveFieldsAndEncodeReasons(int status)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? Problem(status, "Result <rejected>", new { finishTimeSeconds = new[] { "Check <time>" } }) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/edit", Fields()); string html = await response.Content.ReadAsStringAsync(); Assert.Equal(status, (int)response.StatusCode); Assert.Contains("01:02:30", html); Assert.Contains("Check &lt;time&gt;", html); Assert.Contains("data-valmsg-for=\"FinishTime\"", html);
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("recorder")]
    [InlineData("recorded-time")]
    [InlineData("values")]
    [InlineData("server-error")]
    public async Task UnconfirmedCorrections_ShowCheckBeforeRetry(string failure)
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Put) return Reply(request);
            if (failure == "server-error") return Problem(503, "private-server-error");
            using var saved = Saved(request); var data = JsonNode.Parse(saved.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!;
            if (failure == "identity") data["resultId"] = 999;
            if (failure == "recorder") data["recordedByUserId"] = 999;
            if (failure == "recorded-time") data["recordedAt"] = "2026-11-19T00:00:00Z";
            if (failure == "values") data["finishTimeSeconds"] = 1;
            return WebApiFactory.Json(data.ToJsonString());
        };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/8/edit", Fields()); string html = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("Check the saved result", html); Assert.DoesNotContain("private-server-error", html);
    }

    [Theory]
    [InlineData("/8")]
    [InlineData("/8/capture")]
    [InlineData("/8/edit")]
    public async Task WrongEventLinks_AreRejectedBeforeResultRequests(string suffix)
    {
        using var factory = Factory(); int requests = 0; factory.Reply = request => { if (request.RequestUri!.AbsolutePath == "/api/enrolments/8") return WebApiFactory.Json(RosterTests.EntryJson.Replace("\"eventId\":2", "\"eventId\":1")); if (request.RequestUri.AbsolutePath.EndsWith("/result")) requests++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root + suffix); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode); Assert.Equal(0, requests);
    }

    [Theory]
    [InlineData("foreign-entry")]
    [InlineData("null-items")]
    [InlineData("invalid-status")]
    public async Task InconsistentLists_DoNotExposePrivateRecords(string failure)
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/events/2/results") return Reply(request);
            if (failure == "null-items") return WebApiFactory.Json("{\"page\":1,\"pageSize\":10,\"totalCount\":1,\"totalPages\":1,\"items\":null}");
            var data = ResultData(); if (failure == "foreign-entry") data["eventId"] = 999; else data["resultStatus"] = "Unknown"; return Page(result: data);
        };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.DoesNotContain("Naledi Mokoena", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OfficialDetails_EncodeNotesAndDisplayCorrectionHistory()
    {
        using var factory = Factory(); factory.Reply = request => { if (request.RequestUri!.AbsolutePath != "/api/enrolments/8/result") return Reply(request); var data = ResultData(); data["notes"] = "<script>note()</script>"; return WebApiFactory.Json(data.ToJsonString()); };
        using var client = factory.Client(); using var login = await SignIn(client); string html = await client.GetStringAsync(Root + "/8"); Assert.Contains("Last corrected", html); Assert.Contains("Correct result", html); Assert.Contains("&lt;script&gt;note()&lt;/script&gt;", html); Assert.DoesNotContain("<script>", html);
    }

    [Fact]
    public async Task EndedApiSession_ClearsLocalAccess()
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/2/results" ? Problem(401, "expired") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.StartsWith("/account/login", response.Headers.Location?.OriginalString); Assert.DoesNotContain("signed-in-user", await client.GetStringAsync("/"));
    }

    private static Dictionary<string, string> Fields(string time = "01:02:30") => new() { ["ResultStatus"] = "Completed", ["FinishTime"] = time, ["OverallPosition"] = "4", ["CategoryPosition"] = "2", ["Notes"] = "  Confirmed timing  " };
    private static WebApiFactory Factory(string role = "Organiser") => new() { Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" ? Login(role) : Reply(request), ProfileReply = _ => WebApiFactory.Profile(role, "Thabo", 1) };
    private static Task<HttpResponseMessage> SignIn(HttpClient client) => AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
    internal static JsonNode ResultData()
    {
        var data = JsonNode.Parse(ResultTests.ResultJson)!; data["eventId"] = 2; data["eventCategoryId"] = 3; data["recordedByUserId"] = 1; data["participantName"] = "Naledi Mokoena"; return data;
    }
    private static HttpResponseMessage Page(int page = 1, int count = 1, JsonNode? result = null) => WebApiFactory.Json($$"""{"page":{{page}},"pageSize":10,"totalCount":{{count}},"totalPages":{{(count + 9) / 10}},"items":[{{(result ?? ResultData()).ToJsonString()}}]}""");
    private static HttpResponseMessage Saved(HttpRequestMessage request)
    {
        var data = ResultData(); var fields = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!;
        foreach (string field in new[] { "resultStatus", "finishTimeSeconds", "overallPosition", "categoryPosition", "notes" }) data[field] = fields[field]?.DeepClone();
        data["notes"] = fields["notes"]?.GetValue<string>().Trim(); data["updatedAt"] = request.Method == HttpMethod.Put ? "2026-11-19T01:00:00Z" : null;
        var response = WebApiFactory.Json(data.ToJsonString()); if (request.Method == HttpMethod.Post) response.StatusCode = HttpStatusCode.Created; return response;
    }
    private static HttpResponseMessage Problem(int status, string title, object? errors = null) => new((HttpStatusCode)status) { Content = new StringContent(JsonSerializer.Serialize(new { title, errors }), System.Text.Encoding.UTF8, "application/problem+json") };
    private static HttpResponseMessage Reply(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch { "/api/auth/login" => Login(), "/api/organiser/dashboard" => WebApiFactory.Json(OrganiserTests.DashboardJson), "/api/events/2" => WebApiFactory.Json(EventBrowsingTests.DetailsJson), "/api/enrolments/8" => WebApiFactory.Json(RosterTests.EntryJson), "/api/enrolments/8/result" => WebApiFactory.Json(ResultData().ToJsonString()), "/api/events/2/results" => Page(), _ => WebApiFactory.Json(WebApiFactory.EventJson) };
    private static HttpResponseMessage Login(string role = "Organiser") { var response = WebApiFactory.Json(JsonSerializer.Serialize(new { sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20), user = new { userId = 1, firstName = "Thabo", role, isActive = true } })); response.Headers.Add("Set-Cookie", "RaceDay.Session=official-result-token; Path=/; HttpOnly"); return response; }
}
