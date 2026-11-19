using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace RaceDay.Web.Tests;

// Private forms, route safety and write protection: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Contracts: src/RaceDay.Api/Contracts/Categories/ and Contracts/Routes/.
// Accessed: 18 November 2026.
public sealed class CategoryManagementTests
{
    private const string Root = "/organiser/events/2/categories";

    [Theory]
    [InlineData("")]
    [InlineData("/create")]
    [InlineData("/3/edit")]
    [InlineData("/3/delete")]
    [InlineData("/3/route")]
    [InlineData("/3/route/create")]
    [InlineData("/3/route/edit")]
    [InlineData("/3/route/delete")]
    public async Task GuestsAndParticipants_CannotUseManagementPages(string suffix)
    {
        using var factory = Factory("Participant"); using var client = factory.Client();
        using var guest = await client.GetAsync(Root + suffix); Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var login = await SignIn(client); using var participant = await client.GetAsync(Root + suffix); Assert.Equal(HttpStatusCode.Forbidden, participant.StatusCode);
    }

    [Theory]
    [InlineData("/create")]
    [InlineData("/3/edit")]
    [InlineData("/3/delete")]
    [InlineData("/3/route/create")]
    [InlineData("/3/route/edit")]
    [InlineData("/3/route/delete")]
    public async Task AntiforgeryIsRequiredBeforeAnyWrite(string suffix)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method != HttpMethod.Get && request.RequestUri!.AbsolutePath != "/api/auth/login") writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.PostAsync(Root + suffix, new FormUrlEncodedContent([])); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Fact]
    public async Task CategoryList_UsesThePrivateEndpointAndShowsUnavailableRecords()
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/organiser/events/2/categories") return Reply(request);
            Assert.Equal("RaceDay.Session=category-api-token", request.Headers.GetValues("Cookie").Single()); Assert.Empty(request.RequestUri.Query);
            var category = Category(); category["isAvailable"] = false; return WebApiFactory.Json("[" + category.ToJsonString() + "]");
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await client.GetAsync(Root + "?organiserUserId=999"); string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Contains("Unavailable", page); Assert.Contains("40 km Cycle", page); Assert.Contains("R 350,00", page); Assert.Contains("1500", page); Assert.Contains("Peninsula Short Route", page); Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CategoryWrites_SendOnlySevenAllowedFields(bool editing)
    {
        using var factory = Factory(); bool saved = false; factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Get || request.RequestUri!.AbsolutePath == "/api/auth/login") return Reply(request);
            saved = true; Assert.Equal(editing ? HttpMethod.Put : HttpMethod.Post, request.Method); Assert.Equal(editing ? "/api/events/2/categories/3" : "/api/events/2/categories", request.RequestUri.AbsolutePath);
            Assert.Equal("RaceDay.Session=category-api-token", request.Headers.GetValues("Cookie").Single());
            var body = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!; Assert.Equal(7, body.AsObject().Count); Assert.Null(body["eventId"]); Assert.Null(body["eventCategoryId"]); Assert.Null(body["organiserUserId"]); Assert.False(body["isAvailable"]!.GetValue<bool>());
            var response = WebApiFactory.Json(Category().ToJsonString()); response.StatusCode = editing ? HttpStatusCode.OK : HttpStatusCode.Created; return response;
        };
        using var client = factory.Client(); using var login = await SignIn(client); var fields = CategoryFields(); fields["EventId"] = "999"; fields["EventCategoryId"] = "999"; fields["OrganiserUserId"] = "999";
        using var response = await AccountTests.PostForm(client, Root + (editing ? "/3/edit" : "/create"), fields); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal(Root, response.Headers.Location?.OriginalString); Assert.True(saved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RouteWrites_SendOnlySixAllowedFieldsAndPreserveTheLink(bool editing)
    {
        using var factory = Factory(hasRoute: editing); bool saved = false; factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Get || request.RequestUri!.AbsolutePath == "/api/auth/login") return Reply(request, editing);
            saved = true; Assert.Equal(editing ? HttpMethod.Put : HttpMethod.Post, request.Method); Assert.Equal("/api/events/2/categories/3/route", request.RequestUri.AbsolutePath);
            var body = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!; Assert.Equal(6, body.AsObject().Count); Assert.Null(body["routeId"]); Assert.Null(body["eventCategoryId"]); Assert.Null(body["eventId"]);
            var response = WebApiFactory.Json(Category()["route"]!.ToJsonString()); response.StatusCode = editing ? HttpStatusCode.OK : HttpStatusCode.Created; return response;
        };
        using var client = factory.Client(); using var login = await SignIn(client); var fields = RouteFields(); fields["RouteId"] = "999"; fields["EventCategoryId"] = "999"; fields["EventId"] = "999";
        using var response = await AccountTests.PostForm(client, Root + "/3/route/" + (editing ? "edit" : "create"), fields); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal(Root + "/3/route", response.Headers.Location?.OriginalString); Assert.True(saved);
    }

    [Theory]
    [InlineData("/3/delete", "/api/events/2/categories/3")]
    [InlineData("/3/route/delete", "/api/events/2/categories/3/route")]
    public async Task DeleteConfirmation_IsReadOnlyUntilAProtectedPost(string suffix, string upstream)
    {
        using var factory = Factory(); int deletes = 0; factory.Reply = request =>
        { if (request.Method != HttpMethod.Delete) return Reply(request); deletes++; Assert.Equal(upstream, request.RequestUri!.AbsolutePath); Assert.Null(request.Content); return new HttpResponseMessage(HttpStatusCode.NoContent); };
        using var client = factory.Client(); using var login = await SignIn(client); string page = await client.GetStringAsync(Root + suffix); Assert.Contains("Keep ", page); Assert.Equal(0, deletes);
        using var response = await AccountTests.PostForm(client, Root + suffix, []); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal(1, deletes);
    }

    [Theory]
    [InlineData("/create")]
    [InlineData("/3/edit")]
    [InlineData("/3/route/edit")]
    public async Task InvalidInputs_AreRejectedWithoutCallingTheWriteEndpoint(string suffix)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method != HttpMethod.Get && request.RequestUri!.AbsolutePath != "/api/auth/login") writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); var fields = suffix.Contains("route") ? RouteFields() : CategoryFields(); fields[suffix.Contains("route") ? "RouteName" : "CategoryName"] = "";
        using var response = await AccountTests.PostForm(client, Root + suffix, fields); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("/3/edit")]
    [InlineData("/3/delete")]
    [InlineData("/3/route/edit")]
    public async Task ApiConflicts_KeepThePageAndShowAnEncodedReason(string suffix)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Get || request.RequestUri!.AbsolutePath == "/api/auth/login" ? Reply(request) : Problem(409, "Saved entries <protect> this record");
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + suffix, suffix.Contains("route") ? RouteFields() : CategoryFields());
        string page = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.Conflict, response.StatusCode); Assert.Contains("Saved entries &lt;protect&gt; this record", page);
    }

    [Fact]
    public async Task ApiValidationErrors_PreserveInputsAndMapToTheEditableField()
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("{\"errors\":{\"MaximumAge\":[\"Minimum age cannot exceed maximum age.\"]}}", System.Text.Encoding.UTF8, "application/problem+json") } : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/3/edit", CategoryFields());
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); string page = await response.Content.ReadAsStringAsync(); Assert.Contains("Minimum age cannot exceed maximum age", page); Assert.Contains("10 km Open", page);
    }

    [Theory]
    [InlineData("/99/edit")]
    [InlineData("/99/delete")]
    [InlineData("/99/route")]
    [InlineData("/99/route/edit")]
    public async Task CategoryIdsFromAnotherEvent_AreRejectedBeforeAnyWrite(string suffix)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method != HttpMethod.Get && request.RequestUri!.AbsolutePath != "/api/auth/login") writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root + suffix); Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        if (!suffix.EndsWith("/route")) { using var post = await AccountTests.PostForm(client, Root + suffix, CategoryFields()); Assert.Equal(HttpStatusCode.NotFound, post.StatusCode); }
        Assert.Equal(0, writes);
    }

    [Fact]
    public async Task OtherOrganisers_CannotOpenOrSubmitChangesForAPublicEvent()
    {
        using var factory = Factory(); factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/organiser/dashboard" ? WebApiFactory.Json(OrganiserTests.EmptyDashboard) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using var post = await AccountTests.PostForm(client, Root + "/create", CategoryFields()); Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:secret@example.com/map")]
    public async Task UnsafeStoredMapLinks_AreNotRendered(string url)
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/events/2") return Reply(request);
            var item = Details(); item["categories"]![0]!["route"]!["routeMapUrl"] = url; return WebApiFactory.Json(item.ToJsonString());
        };
        using var client = factory.Client(); using var login = await SignIn(client); string page = await client.GetStringAsync(Root + "/3/route"); Assert.DoesNotContain("Open route map", page); Assert.DoesNotContain(url, page);
    }

    [Theory]
    [InlineData("foreign-category")]
    [InlineData("duplicate-category")]
    [InlineData("invalid-json")]
    public async Task MalformedPrivateCategoryLists_AreNotShown(string failure)
    {
        using var factory = Factory(); factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/organiser/events/2/categories") return Reply(request);
            var category = Category(); if (failure == "foreign-category") category["eventId"] = 999;
            return WebApiFactory.Json(failure == "invalid-json" ? "{" : "[" + category.ToJsonString() + (failure == "duplicate-category" ? "," + category.ToJsonString() : "") + "]");
        };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await client.GetAsync(Root); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("/create", "server")]
    [InlineData("/3/route/edit", "network")]
    [InlineData("/3/delete", "timeout")]
    public async Task UncertainWrites_Show503AndAdviseCheckingSavedData(string suffix, string failure)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Get || request.RequestUri!.AbsolutePath == "/api/auth/login" ? Reply(request) : failure switch
        { "network" => throw new HttpRequestException("private network"), "timeout" => throw new TaskCanceledException("private timeout"), _ => new HttpResponseMessage(HttpStatusCode.InternalServerError) };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + suffix, suffix.Contains("route") ? RouteFields() : CategoryFields()); string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("Check the saved categories and routes", page); Assert.DoesNotContain("private network", page); Assert.DoesNotContain("has been created", page);
    }

    [Fact]
    public async Task EndedApiSessions_ClearLocalAccess()
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? new HttpResponseMessage(HttpStatusCode.Unauthorized) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await AccountTests.PostForm(client, Root + "/3/edit", CategoryFields()); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var next = await client.GetAsync(Root); Assert.Equal(HttpStatusCode.Redirect, next.StatusCode);
    }

    private static WebApiFactory Factory(string role = "Organiser", bool hasRoute = true) => new() { Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login" ? Login(role) : Reply(request, hasRoute), ProfileReply = _ => WebApiFactory.Profile(role, "Thabo", 1) };
    private static Task<HttpResponseMessage> SignIn(HttpClient client) => AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
    private static JsonNode Details(bool route = true) { var item = JsonNode.Parse(EventBrowsingTests.DetailsJson)!; if (!route) item["categories"]![0]!["route"] = null; return item; }
    private static JsonNode Category() => Details()["categories"]![0]!.DeepClone();
    private static HttpResponseMessage Problem(int code, string title) => new((HttpStatusCode)code) { Content = new StringContent(JsonSerializer.Serialize(new { title }), System.Text.Encoding.UTF8, "application/problem+json") };
    private static HttpResponseMessage Reply(HttpRequestMessage request, bool hasRoute = true) => request.RequestUri!.AbsolutePath switch
    { "/api/auth/login" => Login(), "/api/organiser/dashboard" => WebApiFactory.Json(OrganiserTests.DashboardJson), "/api/events/2" => WebApiFactory.Json(Details(hasRoute).ToJsonString()), "/api/organiser/events/2/categories" => WebApiFactory.Json("[" + Category().ToJsonString() + "]"), _ => WebApiFactory.Json(WebApiFactory.EventJson) };
    internal static Dictionary<string, string> CategoryFields() => new() { ["CategoryName"] = "10 km Open", ["DistanceKm"] = "10", ["MinimumAge"] = "18", ["MaximumAge"] = "60", ["EntryFee"] = "100", ["Capacity"] = "500", ["IsAvailable"] = "false" };
    internal static Dictionary<string, string> RouteFields() => new() { ["RouteName"] = "Coastal Route", ["StartLocation"] = "Park start", ["FinishLocation"] = "Park finish", ["RouteDescription"] = "Follow the marked road.", ["RouteMapUrl"] = "https://example.com/map", ["ElevationGainMetres"] = "150" };
    private static HttpResponseMessage Login(string role = "Organiser")
    { var response = WebApiFactory.Json(JsonSerializer.Serialize(new { sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20), user = new { userId = 1, firstName = "Thabo", role, isActive = true } })); response.Headers.Add("Set-Cookie", "RaceDay.Session=category-api-token; Path=/; HttpOnly"); return response; }
}
