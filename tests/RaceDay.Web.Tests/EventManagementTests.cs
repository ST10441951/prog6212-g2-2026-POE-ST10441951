using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace RaceDay.Web.Tests;

// Protected writes, form validation and private response checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Events/.
// Accessed: 18 November 2026.
public sealed class EventManagementTests
{
    [Theory]
    [InlineData("/organiser/events/create")]
    [InlineData("/organiser/events/2")]
    [InlineData("/organiser/events/2/edit")]
    [InlineData("/organiser/events/2/delete")]
    public async Task GuestsAndParticipants_CannotOpenEventManagementPages(string route)
    {
        using var factory = Factory("Participant"); using var client = factory.Client();
        using var guest = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var login = await SignIn(client); using var participant = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Forbidden, participant.StatusCode);
    }

    [Theory]
    [InlineData("create")]
    [InlineData("2/edit")]
    [InlineData("2/delete")]
    public async Task MissingAntiforgery_PreventsAllEventWrites(string action)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method != HttpMethod.Get && request.RequestUri!.AbsolutePath != "/api/auth/login") writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.PostAsync("/organiser/events/" + action, new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Fact]
    public async Task CreatingAnEvent_SendsOnlyEditableFieldsWithAnExplicitSastOffset()
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Post || request.RequestUri!.AbsolutePath != "/api/events") return Reply(request);
            writes++; Assert.Equal("RaceDay.Session=event-write-token", request.Headers.GetValues("Cookie").Single());
            var body = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!;
            Assert.Equal(12, body.AsObject().Count); Assert.Null(body["status"]); Assert.Null(body["organiserUserId"]); Assert.Null(body["eventId"]); Assert.Null(body["bannerImageBlobName"]);
            Assert.Equal("2027-03-14T00:30:17+02:00", body["eventDateTime"]!.GetValue<string>());
            Assert.Equal("2027-03-13T22:30:17Z", DateTimeOffset.Parse(body["eventDateTime"]!.GetValue<string>()).UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ssZ"));
            return Event("Draft", HttpStatusCode.Created);
        };
        using var client = factory.Client(); using var login = await SignIn(client); var fields = Fields(); fields["Status"] = "Open"; fields["OrganiserUserId"] = "999"; fields["EventId"] = "999"; fields["BannerImageBlobName"] = "foreign";
        using var response = await AccountTests.PostForm(client, "/organiser/events/create", fields);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal("/organiser/events/2", response.Headers.Location?.OriginalString); Assert.Equal(1, writes);
        string saved = await client.GetStringAsync("/organiser/events/2"); Assert.Contains("Draft event has been created", saved);
    }

    [Fact]
    public async Task EditForms_LoadSastValuesAndUpdatesUseTheRouteId()
    {
        using var factory = Factory(); bool updated = false; factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Put) return Reply(request);
            updated = true; Assert.Equal("/api/events/2", request.RequestUri!.AbsolutePath);
            var body = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!;
            Assert.Equal(13, body.AsObject().Count); Assert.Equal("Closed", body["status"]!.GetValue<string>());
            Assert.Null(body["eventId"]); Assert.Null(body["organiserUserId"]);
            return Event("Closed");
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        string form = await client.GetStringAsync("/organiser/events/2/edit"); Assert.Contains("2027-03-14T00:30:00", form); Assert.Contains("SAST", form);
        var fields = Fields(); fields["Status"] = "Closed"; fields["EventId"] = "999"; fields["OrganiserUserId"] = "999";
        using var response = await AccountTests.PostForm(client, "/organiser/events/2/edit", fields);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.True(updated);
        Assert.Equal("/organiser/events/2", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData("/organiser/events/99")]
    [InlineData("/organiser/events/99/edit")]
    [InlineData("/organiser/events/99/delete")]
    public async Task PublicEventsOwnedBySomeoneElse_DoNotOpenPrivateForms(string route)
    {
        using var factory = Factory(); int details = 0; factory.Reply = request => { if (request.RequestUri!.AbsolutePath == "/api/events/99") details++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(0, details);
        using var own = await client.GetAsync("/organiser"); Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        if (route.EndsWith("edit") || route.EndsWith("delete"))
        {
            // A valid token from the layout must not grant another event's ownership.
            using var post = await AccountTests.PostForm(client, route, Fields()); Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        }
    }

    [Theory]
    [InlineData("EventName", "")]
    [InlineData("DistanceKm", "0")]
    [InlineData("EventDateTime", "invalid")]
    public async Task InvalidFormValues_AreRejectedBeforeCreation(string field, string value)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.RequestUri!.AbsolutePath == "/api/events") writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); var fields = Fields(); fields[field] = value;
        using var response = await AccountTests.PostForm(client, "/organiser/events/create", fields); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Fact]
    public async Task ApiFieldErrors_PreserveValuesAndEncodeMessages()
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/events"
            ? Problem(400, "Invalid details", new() { ["EntryClosingDateTime"] = ["Entries must close before the event starts."], ["Latitude"] = ["<script>invalid</script>"] }) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, "/organiser/events/create", Fields()); string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Contains("Entries must close before", page); Assert.Contains("&lt;script&gt;invalid&lt;/script&gt;", page); Assert.DoesNotContain("<script>", page); Assert.Contains("Test Road Race", page);
    }

    [Theory]
    [InlineData("edit")]
    [InlineData("delete")]
    public async Task HistoryAndStatusConflicts_KeepTheFormAndApiExplanation(string action)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete
            ? Problem(409, "Saved event history prevents this change") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, "/organiser/events/2/" + action, Fields()); string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Contains("Saved event history prevents this change", page); Assert.DoesNotContain("has been updated", page);
    }

    [Fact]
    public async Task Deletion_RequiresConfirmationAndUsesDeleteWithoutABody()
    {
        using var factory = Factory(); int deletes = 0; factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Delete) return Reply(request);
            deletes++; Assert.Equal("/api/events/2", request.RequestUri!.AbsolutePath); Assert.Null(request.Content); Assert.Equal("RaceDay.Session=event-write-token", request.Headers.GetValues("Cookie").Single()); return new HttpResponseMessage(HttpStatusCode.NoContent);
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        string page = await client.GetStringAsync("/organiser/events/2/delete"); Assert.Contains("Delete permanently", page); Assert.Contains("Keep event", page); Assert.Equal(0, deletes);
        using var response = await AccountTests.PostForm(client, "/organiser/events/2/delete", []); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal("/organiser/events", response.Headers.Location?.OriginalString); Assert.Equal(1, deletes);
    }

    [Theory]
    [InlineData("create", "server")]
    [InlineData("2/edit", "network")]
    [InlineData("2/delete", "timeout")]
    public async Task UnconfirmedWrites_Show503AndAdviseCheckingSavedEvents(string action, string failure)
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" || request.Method == HttpMethod.Get ? Reply(request) : failure switch
        { "network" => throw new HttpRequestException("private failure"), "timeout" => throw new TaskCanceledException("private timeout"), _ => new HttpResponseMessage(HttpStatusCode.InternalServerError) };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, "/organiser/events/" + action, Fields()); string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("Check My events before trying again", page); Assert.DoesNotContain("private failure", page); Assert.DoesNotContain("has been created", page);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong-id")]
    [InlineData("foreign-category")]
    [InlineData("invalid-json")]
    public async Task IncompleteEventResponses_AreNotDisplayed(string failure)
    {
        var body = JsonNode.Parse(EventBrowsingTests.DetailsJson)!;
        if (failure == "missing") body.AsObject().Remove("description"); if (failure == "wrong-id") body["eventId"] = 99; if (failure == "foreign-category") body["categories"]![0]!["eventId"] = 99;
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/events/2" ? WebApiFactory.Json(failure == "invalid-json" ? "{" : body.ToJsonString()) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync("/organiser/events/2"); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    public async Task WriteRejections_KeepTheCorrectStatusAndSessionBehaviour(int status)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? Problem(status, "Rejected") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, "/organiser/events/2/edit", Fields());
        Assert.Equal(status == 401 ? 302 : status, (int)response.StatusCode); using var next = await client.GetAsync("/organiser"); Assert.Equal(status == 401 ? HttpStatusCode.Redirect : HttpStatusCode.OK, next.StatusCode);
    }

    private static WebApiFactory Factory(string role = "Organiser") => new() { Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" ? Login(role) : Reply(request), ProfileReply = _ => WebApiFactory.Profile(role, "Thabo", 1) };
    private static Task<HttpResponseMessage> SignIn(HttpClient client) => AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
    internal static Dictionary<string, string> Fields() => new()
    {
        ["EventName"] = "Test Road Race",
        ["EventType"] = "Running",
        ["Description"] = "A community road race.",
        ["DistanceKm"] = "10",
        ["EventDateTime"] = "2027-03-14T00:30:17",
        ["EntryClosingDateTime"] = "2027-03-01T22:00:00",
        ["VenueName"] = "Sports Park",
        ["AddressLine1"] = "1 Park Road",
        ["City"] = "Durban",
        ["Province"] = "KwaZulu-Natal",
        ["Latitude"] = "-29.85",
        ["Longitude"] = "31.02",
        ["Status"] = "Open"
    };
    private static HttpResponseMessage Event(string status = "Open", HttpStatusCode code = HttpStatusCode.OK)
    { var body = JsonNode.Parse(EventBrowsingTests.DetailsJson)!; body["status"] = status; var reply = WebApiFactory.Json(body.ToJsonString()); reply.StatusCode = code; return reply; }
    private static HttpResponseMessage Problem(int status, string title, Dictionary<string, string[]>? errors = null) => new((HttpStatusCode)status) { Content = JsonContent(title, errors) };
    private static System.Net.Http.StringContent JsonContent(string title, Dictionary<string, string[]>? errors) => new(JsonSerializer.Serialize(new { title, errors }), System.Text.Encoding.UTF8, "application/problem+json");
    private static HttpResponseMessage Reply(HttpRequestMessage request) => request.RequestUri!.AbsolutePath switch
    { "/api/auth/login" => Login(), "/api/organiser/dashboard" => WebApiFactory.Json(OrganiserTests.DashboardJson), "/api/events/2" => Event(), _ => WebApiFactory.Json(WebApiFactory.EventJson) };
    private static HttpResponseMessage Login(string role = "Organiser")
    { var reply = WebApiFactory.Json(JsonSerializer.Serialize(new { sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20), user = new { userId = 1, firstName = "Thabo", role, isActive = true } })); reply.Headers.Add("Set-Cookie", "RaceDay.Session=event-write-token; Path=/; HttpOnly"); return reply; }
}
