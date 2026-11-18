using System.Net;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace RaceDay.Web.Tests;

// HTTP assertions, test hosting and Razor encoding: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class HomeTests
{
    [Fact]
    public async Task Home_RendersEventsFromTheConfiguredApi()
    {
        using var factory = new WebApiFactory();
        string? calledAddress = null;
        factory.Settings["Api:BaseUrl"] = "https://events.example.test/raceday/";
        factory.Reply = request =>
        {
            calledAddress = request.RequestUri!.AbsoluteUri;
            Assert.Contains(request.Headers.Accept, value => value.MediaType == "application/json");
            return WebApiFactory.Json(WebApiFactory.EventJson);
        };
        using var client = factory.Client();

        using var response = await client.GetAsync("/");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://events.example.test/raceday/api/events?page=1&pageSize=3", calledAddress);
        Assert.Contains("Cape Peninsula Cycle Challenge", page);
        Assert.Contains("Cape Town, Western Cape", page);
        Assert.Contains("80 <small>km</small>", page);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task Home_EncodesEventNamesInsteadOfRenderingHtml()
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => WebApiFactory.Json(WebApiFactory.EventJson.Replace(
                "Cape Peninsula Cycle Challenge", "<script>alert(1)</script>"))
        };
        using var client = factory.Client();
        string page = await client.GetStringAsync("/");

        Assert.Contains("&lt;script&gt;alert(1)&lt;/script&gt;", page);
        Assert.DoesNotContain("<script>alert(1)</script>", page);
    }

    [Fact]
    public async Task Home_DisplaysTheSouthAfricanDateAtUtcDayBoundary()
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => WebApiFactory.Json(WebApiFactory.EventJson.Replace(
                "2027-03-14T04:00:00Z", "2027-03-13T22:30:00Z"))
        };
        using var client = factory.Client();
        string page = await client.GetStringAsync("/");

        Assert.Contains("<span>14</span>", page);
        Assert.Contains("Mar 2027", page);
    }

    [Fact]
    public async Task Home_ShowsAnEmptyStateWithoutInventingEvents()
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => WebApiFactory.Json("""{"page":1,"pageSize":3,"totalCount":0,"totalPages":0,"items":[]}""")
        };
        using var client = factory.Client();
        using var response = await client.GetAsync("/");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No upcoming events yet", page);
        Assert.DoesNotContain("event-row", page);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(404)]
    [InlineData(500)]
    [InlineData(503)]
    public async Task Home_HidesApiErrorDetails(int status)
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => new HttpResponseMessage((HttpStatusCode)status)
            {
                Content = new StringContent("private-server-error-do-not-display")
            }
        };
        using var client = factory.Client();
        using var response = await client.GetAsync("/");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Events are temporarily unavailable", page);
        Assert.DoesNotContain("private-server-error", page);
        Assert.DoesNotContain("No upcoming events yet", page);
    }

    [Theory]
    [InlineData("not JSON")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("{\"page\":1,\"pageSize\":3,\"totalCount\":0,\"totalPages\":0,\"items\":null}")]
    public async Task Home_HandlesUnreadableOrIncompleteApiResponses(string json)
    {
        using var factory = new WebApiFactory { Reply = _ => WebApiFactory.Json(json) };
        using var client = factory.Client();
        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Events are temporarily unavailable", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Home_HandlesConnectionFailuresAndTimeouts(bool timeout)
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => timeout ? throw new TaskCanceledException("private-timeout")
                : throw new HttpRequestException("private-connection")
        };
        using var client = factory.Client();
        using var response = await client.GetAsync("/");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("Events are temporarily unavailable", page);
        Assert.DoesNotContain("private-", page);
    }

    [Theory]
    [InlineData("/css/site.css", "--race-blue")]
    [InlineData("/lib/bootstrap/bootstrap.min.css", "Bootstrap")]
    public async Task Styles_AreServedLocally(string path, string expected)
    {
        using var factory = new WebApiFactory();
        using var client = factory.Client();
        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains(expected, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownPage_UsesTheSharedLayoutAndReturns404()
    {
        using var factory = new WebApiFactory();
        using var client = factory.Client();
        using var response = await client.GetAsync("/missing-page");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Page not found", page);
        Assert.Contains("site-header", page);
        Assert.Contains("Back to home", page);
    }

    [Fact]
    public async Task UnexpectedFailures_HideExceptionDetails()
    {
        using var factory = new WebApiFactory();
        using var client = factory.Client();
        using var response = await client.GetAsync("/__tests/failure");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("Something went wrong", page);
        Assert.Contains("Request reference:", page);
        Assert.DoesNotContain("private-exception-detail", page);
        Assert.DoesNotContain("InvalidOperationException", page);
    }

    [Fact]
    public async Task Health_DoesNotDependOnTheApi()
    {
        using var factory = new WebApiFactory { Reply = _ => throw new HttpRequestException() };
        using var client = factory.Client();
        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("{\"status\":\"Healthy\"}", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Filters_AreSentToTheApiAndKeptInTheForm()
    {
        using var factory = new WebApiFactory();
        string? calledQuery = null;
        factory.Reply = request =>
        {
            calledQuery = request.RequestUri!.Query;
            return WebApiFactory.Json(WebApiFactory.EventJson);
        };
        using var client = factory.Client();
        string page = await client.GetStringAsync("/?province=Western%20Cape&eventType=Cycling");

        Assert.NotNull(calledQuery);
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(calledQuery);
        Assert.Equal("Cycling", query["eventType"].ToString());
        Assert.Equal("Western Cape", query["province"].ToString());
        Assert.Contains("value=\"Cycling\" selected=\"selected\"", page);
        Assert.Contains("value=\"Western Cape\" selected=\"selected\"", page);
        Assert.Contains("Showing 1 of 1 events", page);
        Assert.Contains("Clear filters", page);
    }

    [Fact]
    public async Task InvalidFilters_ShowAUsefulMessageWithoutApiDetails()
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("private-validation-detail")
            }
        };
        using var client = factory.Client();
        using var response = await client.GetAsync("/?eventType=invalid");
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Choose an event type and province", page);
        Assert.DoesNotContain("private-validation-detail", page);
    }

    [Fact]
    public async Task EmptyFilteredResults_AreDifferentFromAnUnavailableApi()
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => WebApiFactory.Json("""{"page":1,"pageSize":3,"totalCount":0,"totalPages":0,"items":[]}""")
        };
        using var client = factory.Client();
        string page = await client.GetStringAsync("/?eventType=Running");

        Assert.Contains("No events match these filters", page);
        Assert.DoesNotContain("Events are temporarily unavailable", page);
        Assert.DoesNotContain("No upcoming events yet", page);
    }
}

// This controller is registered by the test factory only.
[ApiController]
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public sealed class FailureController : ControllerBase
{
    [HttpGet("/__tests/failure")]
    public IActionResult Fail() => throw new InvalidOperationException("private-exception-detail");
}
