using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace RaceDay.Web.Tests;

// Exercise the real HTTP handler rather than replacing it with a test handler.
// Microsoft (2026): https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// Dynamic test-server port: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class HttpConnectionTests
{
    [Fact]
    public async Task ApiCookies_AreNotSavedOrSharedBetweenVisitors()
    {
        var cookies = new ConcurrentQueue<string>();
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var upstream = builder.Build();
        upstream.MapGet("/api/events", (HttpContext context) =>
        {
            cookies.Enqueue(context.Request.Headers.Cookie.ToString());
            context.Response.Cookies.Append("RaceDay.Session", "another-users-session");
            return Results.Content(WebApiFactory.EventJson, "application/json");
        });
        await upstream.StartAsync();
        using var factory = new WebApiFactory { Reply = null };
        factory.Settings["Api:BaseUrl"] = Address(upstream);
        using var first = factory.Client();
        using var second = factory.Client();
        first.DefaultRequestHeaders.Add("Cookie", "unrelated-browser-cookie=private");

        using var firstResponse = await first.GetAsync("/");
        using var secondResponse = await second.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);
        Assert.Equal(2, cookies.Count);
        Assert.All(cookies, Assert.Empty);
        Assert.False(firstResponse.Headers.Contains("Set-Cookie"));
        Assert.False(secondResponse.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task ApiRedirects_AreNotFollowed()
    {
        int redirectedRequests = 0;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var upstream = builder.Build();
        upstream.MapGet("/api/events", () => Results.Redirect("/unexpected-target"));
        upstream.MapGet("/unexpected-target", () =>
        {
            Interlocked.Increment(ref redirectedRequests);
            return Results.Content(WebApiFactory.EventJson, "application/json");
        });
        await upstream.StartAsync();
        using var factory = new WebApiFactory { Reply = null };
        factory.Settings["Api:BaseUrl"] = Address(upstream);
        using var client = factory.Client();

        using var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(0, redirectedRequests);
    }

    private static string Address(WebApplication application) => application.Services
        .GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single() + "/";

    [Fact]
    public async Task AccountClient_DoesNotShareAnUpstreamLoginBetweenBrowsers()
    {
        var cookies = new ConcurrentQueue<string>();
        int accountNumber = 0;
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        await using var upstream = builder.Build();
        upstream.MapPost("/api/auth/login", (HttpContext context) =>
        {
            cookies.Enqueue(context.Request.Headers.Cookie.ToString());
            int number = Interlocked.Increment(ref accountNumber);
            context.Response.Cookies.Append("RaceDay.Session", $"upstream-token-{number}");
            return Results.Json(new
            {
                sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20),
                user = new { userId = number, firstName = $"Visitor{number}", role = "Participant", isActive = true }
            });
        });
        upstream.MapGet("/api/events", () => Results.Content(WebApiFactory.EventJson, "application/json"));
        upstream.MapGet("/api/profile", (HttpContext context) =>
        {
            string token = context.Request.Cookies["RaceDay.Session"] ?? "";
            int number = token == "upstream-token-1" ? 1 : token == "upstream-token-2" ? 2 : 0;
            return number == 0 ? Results.Unauthorized() : Results.Json(new
            {
                userId = number, firstName = $"Visitor{number}", lastName = "Test", email = $"visitor{number}@example.com",
                role = "Participant", isActive = true, createdAt = "2026-11-18T12:00:00Z"
            });
        });
        upstream.MapGet("/api/enrolments/{id:int}", (int id, HttpContext context) =>
        {
            string token = context.Request.Cookies["RaceDay.Session"] ?? "";
            int number = token == "upstream-token-1" ? 1 : token == "upstream-token-2" ? 2 : 0;
            if (number == 0 || number != id) return Results.StatusCode(403);
            return Results.Json(new
            {
                enrolmentId = id, participantUserId = number, participantName = $"Visitor{number}",
                eventId = 2, eventName = "Private test entry", eventDateTime = "2027-03-14T04:00:00Z",
                eventCategoryId = 3, categoryName = "40 km Cycle", categoryDistanceKm = 40,
                enrolmentDate = "2026-11-18T12:00:00Z", status = "Confirmed"
            });
        });
        await upstream.StartAsync();
        using var factory = new WebApiFactory { Reply = null };
        factory.Settings["Api:BaseUrl"] = Address(upstream);
        using var first = factory.Client();
        using var second = factory.Client();
        using var firstLogin = await AccountTests.PostForm(first, "/account/login", AccountTests.LoginFields());
        using var secondLogin = await AccountTests.PostForm(second, "/account/login", AccountTests.LoginFields());

        Assert.Equal(HttpStatusCode.Redirect, firstLogin.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, secondLogin.StatusCode);
        Assert.Equal(2, cookies.Count);
        Assert.All(cookies, Assert.Empty);
        string firstPage = await first.GetStringAsync("/");
        string secondPage = await second.GetStringAsync("/");
        Assert.Contains("Visitor1", firstPage);
        Assert.DoesNotContain("Visitor2", firstPage);
        Assert.Contains("Visitor2", secondPage);
        Assert.DoesNotContain("Visitor1", secondPage);
        string firstProfile = await first.GetStringAsync("/profile");
        string secondProfile = await second.GetStringAsync("/profile");
        Assert.Contains("visitor1@example.com", firstProfile);
        Assert.DoesNotContain("visitor2@example.com", firstProfile);
        Assert.Contains("visitor2@example.com", secondProfile);
        Assert.DoesNotContain("visitor1@example.com", secondProfile);
        Assert.Contains("Visitor1", await first.GetStringAsync("/entries/1"));
        Assert.Contains("Visitor2", await second.GetStringAsync("/entries/2"));
        using var unrelated = await first.GetAsync("/entries/2");
        Assert.Equal(HttpStatusCode.Forbidden, unrelated.StatusCode);
    }
}
