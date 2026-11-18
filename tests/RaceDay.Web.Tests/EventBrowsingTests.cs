using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace RaceDay.Web.Tests;

// Query, response and Razor security checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class EventBrowsingTests
{
    internal const string DetailsJson = """
        {"eventId":2,"eventName":"Cape Peninsula Cycle Challenge","eventType":"Cycling",
         "distanceKm":80,"eventDateTime":"2027-03-13T22:30:00Z","entryClosingDateTime":"2027-03-01T20:00:00Z",
         "venueName":"Green Point Urban Park","addressLine1":"1 Park Road","city":"Cape Town",
         "province":"Western Cape","status":"Open","description":"A coastal cycling event.",
         "latitude":-33.90,"longitude":18.40,"bannerImageUrl":null,
         "categories":[{"eventCategoryId":3,"eventId":2,"categoryName":"40 km Cycle","distanceKm":40,
         "minimumAge":16,"maximumAge":null,"entryFee":350,"capacity":1500,"isAvailable":true,
         "route":{"routeId":3,"routeName":"Peninsula Short Route","startLocation":"Park start",
         "finishLocation":"Park finish","routeDescription":"Follow the coastal road.",
         "routeMapUrl":"https://example.com/map?route=3&view=full","elevationGainMetres":420}}]}
        """;

    [Fact]
    public async Task PagingAndDateFilters_ArePassedToTheApiAndRetainedInEveryPageLink()
    {
        using var factory = new WebApiFactory();
        string? upstream = null;
        factory.Reply = request =>
        {
            upstream = request.RequestUri!.Query;
            Assert.False(request.Headers.Contains("Cookie"));
            return Page(2, 23);
        };
        using var client = factory.Client();
        using var response = await client.GetAsync("/?page=2&eventType=Cycling&province=Western%20Cape&fromDate=2027-01-01&toDate=2027-12-31");
        string page = await response.Content.ReadAsStringAsync();
        var query = QueryHelpers.ParseQuery(upstream);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("2", query["page"]);
        Assert.Equal("3", query["pageSize"]);
        Assert.Equal("2027-01-01", query["fromDate"]);
        Assert.Equal("2027-12-31", query["toDate"]);
        Assert.Contains("value=\"2027-01-01\"", page);
        Assert.Contains("value=\"2027-12-31\"", page);
        Assert.Contains("href=\"/events/2\"", page);
        Assert.Contains("aria-current=\"page\">2</span>", page);
        string pagination = Regex.Match(page, "<nav class=\"event-pagination\"[\\s\\S]*?</nav>").Value;
        var links = Regex.Matches(pagination, "href=\"([^\"]+)\"");
        Assert.InRange(links.Count, 3, 8);
        foreach (Match link in links)
        {
            var address = new Uri(new Uri("https://localhost"), WebUtility.HtmlDecode(link.Groups[1].Value));
            var values = QueryHelpers.ParseQuery(address.Query);
            Assert.Equal("Cycling", values["eventType"]);
            Assert.Equal("Western Cape", values["province"]);
            Assert.Equal("2027-01-01", values["fromDate"]);
            Assert.Equal("2027-12-31", values["toDate"]);
        }
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("page=not-a-number")]
    [InlineData("page=2147483648")]
    [InlineData("fromDate=not-a-date")]
    [InlineData("toDate=2027-02-30")]
    public async Task InvalidTypedQueryValues_Return400WithoutCallingTheApi(string query)
    {
        using var factory = new WebApiFactory();
        int calls = 0;
        factory.Reply = _ => { calls++; return Page(1, 1); };
        using var client = factory.Client();
        using var response = await client.GetAsync("/?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, calls);
        Assert.Contains("valid dates and a page number", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task RejectedDateRange_ShowsAUsefulMessageAndKeepsTheDates()
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("private-date-error") }
        };
        using var client = factory.Client();
        using var response = await client.GetAsync("/?fromDate=2027-12-31&toDate=2027-01-01");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("final date must not be earlier", page);
        Assert.Contains("value=\"2027-12-31\"", page);
        Assert.DoesNotContain("private-date-error", page);
    }

    [Fact]
    public async Task OutOfRangePages_ReturnAnEmptyStateWithAFilteredFirstPageLink()
    {
        using var factory = new WebApiFactory { Reply = _ => Page(int.MaxValue, 7, empty: true) };
        using var client = factory.Client();
        using var response = await client.GetAsync("/?page=2147483647&eventType=Walking");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No events on this page", page);
        Assert.Contains("First page", page);
        Assert.Contains("eventType=Walking", page);
        Assert.DoesNotContain("No upcoming events yet", page);
        Assert.InRange(Regex.Matches(page, "class=\"page-link\"").Count, 1, 6);
    }

    [Theory]
    [InlineData("inconsistent-page")]
    [InlineData("negative-total")]
    [InlineData("wrong-size")]
    [InlineData("inconsistent-pages")]
    public async Task IncompletePagingResponses_Show503InsteadOfInventingPagination(string error)
    {
        using var factory = new WebApiFactory();
        var body = JsonNode.Parse(WebApiFactory.EventJson)!;
        if (error == "inconsistent-page") body["page"] = 2;
        if (error == "negative-total") body["totalCount"] = -1;
        if (error == "wrong-size") body["pageSize"] = 10;
        if (error == "inconsistent-pages") body["totalPages"] = 5;
        factory.Reply = _ => WebApiFactory.Json(body.ToJsonString());
        using var client = factory.Client();
        using var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Theory]
    [InlineData("Open")]
    [InlineData("Closed")]
    [InlineData("Completed")]
    public async Task PublicDetails_ShowTheApiEventCategoriesAndRouteForAllPublicStatuses(string status)
    {
        using var factory = new WebApiFactory();
        factory.Reply = request =>
        {
            Assert.Equal("/api/events/2", request.RequestUri!.AbsolutePath);
            Assert.False(request.Headers.Contains("Cookie"));
            return WebApiFactory.Json(DetailsJson.Replace("\"Open\"", "\"" + status + "\""));
        };
        using var client = factory.Client();
        using var response = await client.GetAsync("/events/2");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        foreach (string value in new[] { "Cape Peninsula Cycle Challenge", "14 March 2027, 00:30", "SAST",
            "01 March 2027, 22:00", "Green Point Urban Park", "1 Park Road", "A coastal cycling event.",
            "40 km Cycle", "16 years and older", "1500", "Peninsula Short Route", "Park start", "Park finish", "420 m", status })
            Assert.Contains(value, page);
        Assert.Contains("R " + 350m.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("en-ZA")), page);
        Assert.Contains("rel=\"noopener noreferrer\"", page);
        Assert.Contains("route=3&amp;view=full", page);
        Assert.Contains("returnUrl=%2Fevents%2F2", page);
    }

    [Theory]
    [InlineData("/events/0")]
    [InlineData("/events/-2")]
    [InlineData("/events/abc")]
    [InlineData("/events/2147483648")]
    public async Task InvalidEventAddresses_Return404WithoutApiCalls(string route)
    {
        using var factory = new WebApiFactory();
        int calls = 0;
        factory.Reply = _ => { calls++; return WebApiFactory.Json(DetailsJson); };
        using var client = factory.Client();
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task MissingOrHiddenEvents_ReturnAFriendly404()
    {
        using var factory = new WebApiFactory { Reply = _ => new HttpResponseMessage(HttpStatusCode.NotFound) };
        using var client = factory.Client();
        using var response = await client.GetAsync("/events/2");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("server")]
    [InlineData("network")]
    [InlineData("timeout")]
    [InlineData("json")]
    [InlineData("missing")]
    [InlineData("wrong-event")]
    [InlineData("wrong-category-event")]
    [InlineData("null-category")]
    [InlineData("hidden-status")]
    public async Task UnavailableOrIncompleteDetails_Return503WithoutApiDetails(string problem)
    {
        using var factory = new WebApiFactory();
        var details = JsonNode.Parse(DetailsJson)!;
        if (problem == "wrong-event") details["eventId"] = 999;
        if (problem == "wrong-category-event") details["categories"]![0]!["eventId"] = 999;
        if (problem == "null-category") details["categories"]![0] = null;
        if (problem == "hidden-status") details["status"] = "Draft";
        factory.Reply = _ => problem switch
        {
            "server" => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("private-api-error") },
            "network" => throw new HttpRequestException("private-network-error"),
            "timeout" => throw new TaskCanceledException("private-timeout-error"),
            "json" => WebApiFactory.Json("not-json"),
            "missing" => WebApiFactory.Json("{}"),
            _ => WebApiFactory.Json(details.ToJsonString())
        };
        using var client = factory.Client();
        using var response = await client.GetAsync("/events/2");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Event details are temporarily unavailable", page);
        Assert.Contains("href=\"/events/2\"", page);
        Assert.DoesNotContain("private-", page);
    }

    [Fact]
    public async Task NoCategoriesOrRoutes_AreDisplayedAsEmptyStates()
    {
        using var factory = new WebApiFactory();
        var details = JsonNode.Parse(DetailsJson)!;
        details["categories"]![0]!["route"] = null;
        factory.Reply = _ => WebApiFactory.Json(details.ToJsonString());
        using var client = factory.Client();
        Assert.Contains("Route information has not been added", await client.GetStringAsync("/events/2"));
        details["categories"] = new JsonArray();
        Assert.Contains("No categories available", await client.GetStringAsync("/events/2"));
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,private")]
    [InlineData("//outside.example/map")]
    [InlineData("https://user:secret@example.com/map")]
    public async Task UnsafeMapAndBannerAddresses_AreNotRendered(string address)
    {
        using var factory = new WebApiFactory();
        var details = JsonNode.Parse(DetailsJson)!;
        details["bannerImageUrl"] = address;
        details["categories"]![0]!["route"]!["routeMapUrl"] = address;
        details["description"] = "<script>private()</script>";
        details["categories"]![0]!["categoryName"] = "<b>Category</b>";
        factory.Reply = _ => WebApiFactory.Json(details.ToJsonString());
        using var client = factory.Client();
        string page = await client.GetStringAsync("/events/2");
        Assert.DoesNotContain("View route map", page);
        Assert.DoesNotContain("<img", page);
        Assert.Contains("&lt;script&gt;private()&lt;/script&gt;", page);
        Assert.Contains("&lt;b&gt;Category&lt;/b&gt;", page);
        Assert.DoesNotContain("<script>private()", page);
    }

    [Fact]
    public async Task SafeBannerUrls_AreDisplayedWithEncodedAttributes()
    {
        using var factory = new WebApiFactory();
        var details = JsonNode.Parse(DetailsJson)!;
        details["bannerImageUrl"] = "https://images.example.com/banner.png?sig=public&read=1";
        factory.Reply = _ => WebApiFactory.Json(details.ToJsonString());
        using var client = factory.Client();
        string page = await client.GetStringAsync("/events/2");
        Assert.Contains("src=\"https://images.example.com/banner.png?sig=public&amp;read=1\"", page);
        Assert.Contains("alt=\"Banner for Cape Peninsula Cycle Challenge\"", page);
    }

    [Fact]
    public async Task SignedInOrganisers_StillReadOnlyPublicDetailsWithoutForwardingTheirCookie()
    {
        using var factory = new WebApiFactory();
        factory.ProfileReply = _ => WebApiFactory.Profile("Organiser");
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/auth/login")
            {
                var response = WebApiFactory.Json(System.Text.Json.JsonSerializer.Serialize(new
                {
                    sessionExpiresAt = factory.Clock.GetUtcNow().AddMinutes(20),
                    user = new { userId = 3, firstName = "Naledi", role = "Organiser", isActive = true }
                }));
                response.Headers.Add("Set-Cookie", "RaceDay.Session=private-owner-session; path=/");
                return response;
            }
            Assert.False(request.Headers.Contains("Cookie"));
            return request.RequestUri.AbsolutePath == "/api/events/2" ? WebApiFactory.Json(DetailsJson) : Page(1, 1);
        };
        using var client = factory.Client();
        using var login = await AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
        using var response = await client.GetAsync("/events/2");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Organiser home", page);
        Assert.DoesNotContain("private-owner-session", page);
        Assert.DoesNotContain("Sign in to your Participant account", page);
    }

    private static HttpResponseMessage Page(int page, int count, bool empty = false)
    {
        var body = JsonNode.Parse(WebApiFactory.EventJson)!;
        body["page"] = page;
        body["totalCount"] = count;
        body["totalPages"] = (int)(((long)count + 2) / 3);
        if (empty) body["items"] = new JsonArray();
        return WebApiFactory.Json(body.ToJsonString());
    }
}
