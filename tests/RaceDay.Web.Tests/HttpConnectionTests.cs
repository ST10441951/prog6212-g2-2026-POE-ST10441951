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
}
