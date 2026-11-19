using System.Text.Encodings.Web;
using System.Text.Json.Nodes;
using Xunit;

namespace RaceDay.Web.Tests;

// Render API-provided image URLs through the existing safe-link check.
// Microsoft (2026): https://learn.microsoft.com/en-us/aspnet/core/security/cross-site-scripting?view=aspnetcore-10.0
// MDN (2026): https://developer.mozilla.org/en-US/docs/Web/HTML/Reference/Elements/img
// Accessed: 19 November 2026.
public sealed class EventThumbnailTests
{
    [Theory]
    [InlineData("https://images.example.test/banner.png?read=1&version=2")]
    [InlineData("http://127.0.0.1:10000/devstoreaccount1/review/banner.png")]
    public async Task Home_DisplaysTheApiBannerWithoutChangingTheEventLink(string url)
    {
        using var factory = Factory(url);
        using var client = factory.Client();
        string page = await client.GetStringAsync("/");

        Assert.Contains("class=\"event-thumbnail\"", page);
        Assert.Contains("src=\"" + HtmlEncoder.Default.Encode(url) + "\"", page);
        Assert.Contains("loading=\"lazy\"", page);
        Assert.Contains("referrerpolicy=\"no-referrer\"", page);
        Assert.Contains("alt=\"Banner for Cape Peninsula Cycle Challenge\"", page);
        Assert.Contains("href=\"/events/2\"", page);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,invalid")]
    [InlineData("file:///C:/private/banner.png")]
    [InlineData("/untrusted-relative-banner.png")]
    [InlineData("https://user:password@images.example.test/banner.png")]
    [InlineData("not a URL")]
    public async Task Home_KeepsTheEventUsableWhenItsBannerIsAbsentOrUnsafe(string? url)
    {
        using var factory = Factory(url);
        using var client = factory.Client();
        string page = await client.GetStringAsync("/");

        Assert.DoesNotContain("event-thumbnail", page);
        Assert.Contains("Cape Peninsula Cycle Challenge", page);
        Assert.Contains("href=\"/events/2\"", page);
        Assert.DoesNotContain("password@", page);
        Assert.DoesNotContain("javascript:", page);
    }

    private static WebApiFactory Factory(string? url)
    {
        var data = JsonNode.Parse(WebApiFactory.EventJson)!;
        data["items"]![0]!["bannerImageUrl"] = url;
        return new WebApiFactory { Reply = _ => WebApiFactory.Json(data.ToJsonString()) };
    }
}
