using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Contracts.Images;
using RaceDay.Api.Contracts.Profiles;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Authentication;
using RaceDay.Api.Tests.Database;
using SkiaSharp;
using Xunit;

namespace RaceDay.Api.Tests.Images;

// Real HTTP, SQL Server and isolated blob checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite
// Image fixtures: https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skimage.encode
// Accessed: 18 October 2026.
public sealed partial class ImageTests(SqlServerFixture database, BlobStorageFixture storage)
    : IClassFixture<SqlServerFixture>, IClassFixture<BlobStorageFixture>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        await using var context = database.CreateContext();
        await context.Users.ExecuteUpdateAsync(x => x.SetProperty(u => u.ProfilePictureBlobName, (string?)null));
        await context.Users.ExecuteUpdateAsync(x => x.SetProperty(u => u.IsActive, true));
        await context.Events.ExecuteUpdateAsync(x => x.SetProperty(e => e.BannerImageBlobName, (string?)null));
        await context.Events.Where(x => x.EventId > 3).ExecuteDeleteAsync();
        await foreach (var blob in storage.Container.GetBlobsAsync()) await storage.Container.DeleteBlobIfExistsAsync(blob.Name);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(false, "png")]
    [InlineData(false, "jpg")]
    [InlineData(true, "png")]
    [InlineData(true, "jpeg")]
    public async Task Upload_SavesOnlyAGeneratedNameAndReturnsAReadablePrivateImage(bool banner, string extension)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        using var form = Form(Pixels(extension), $"../../private.{extension}", Mime(extension));
        using var response = await client.PutAsync(Path(banner), form);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        string url = await Url(response, banner);
        string name = (await SavedName(banner))!;
        Assert.Matches(banner ? @"^event-banners/event-1/[a-f0-9]{32}\.(png|jpg)$" : @"^profile-pictures/user-3/[a-f0-9]{32}\.(png|jpg)$", name);
        Assert.DoesNotContain("private", name);
        Assert.True(name.Length <= 255);
        var blob = storage.Container.GetBlobClient(name);
        var properties = (await blob.GetPropertiesAsync()).Value;
        Assert.Equal(Mime(extension), properties.ContentType);
        Assert.InRange(properties.ContentLength, 1, 5 * 1024 * 1024);
        Assert.Equal(Azure.Storage.Blobs.Models.PublicAccessType.None, (await storage.Container.GetAccessPolicyAsync()).Value.BlobPublicAccess);
        using var publicClient = new HttpClient();
        using var image = await publicClient.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, image.StatusCode);
        Assert.Equal(Mime(extension), image.Content.Headers.ContentType?.MediaType);
        using var bitmap = SKBitmap.Decode(await image.Content.ReadAsByteArrayAsync());
        Assert.Equal(32, bitmap.Width);
        Assert.Equal(20, bitmap.Height);
        using var unsigned = await publicClient.GetAsync(blob.Uri);
        Assert.False(unsigned.IsSuccessStatusCode);
        var query = QueryHelpers.ParseQuery(new Uri(url).Query);
        Assert.Equal("r", query["sp"]);
        Assert.Equal("b", query["sr"]);
        Assert.True(query.ContainsKey("sig"));
        Assert.InRange(DateTimeOffset.Parse(query["se"].ToString()) - factory.Clock.GetUtcNow(), TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));
        Assert.DoesNotContain("AccountKey", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        await AssertReadResponse(client, banner, url);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Replacement_RemovesThePreviousObjectAndKeepsUnrelatedRecords(bool banner)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        using var first = await Upload(client, banner);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        string originalName = (await SavedName(banner))!;
        using var next = await Upload(client, banner, "jpg");
        Assert.Equal(HttpStatusCode.OK, next.StatusCode);
        string replacement = (await SavedName(banner))!;
        Assert.NotEqual(originalName, replacement);
        Assert.False((await storage.Container.GetBlobClient(originalName).ExistsAsync()).Value);
        Assert.True((await storage.Container.GetBlobClient(replacement).ExistsAsync()).Value);
        Assert.Single(await Objects());
        await using var context = database.CreateContext();
        Assert.Equal(4, await context.Users.CountAsync());
        Assert.Equal(3, await context.Events.CountAsync());
        Assert.Equal(4, await context.Enrolments.CountAsync());
        Assert.Equal(2, await context.Results.CountAsync());
        Assert.NotNull(banner ? (await context.Events.FindAsync(1))!.UpdatedAt : (await context.Users.FindAsync(3))!.UpdatedAt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removal_DeletesTheObjectClearsSqlAndIsRepeatable(bool banner)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        using var created = await Upload(client, banner);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        string name = (await SavedName(banner))!;
        using var removed = await client.DeleteAsync(Path(banner));
        Assert.Equal(HttpStatusCode.NoContent, removed.StatusCode);
        Assert.Empty(await removed.Content.ReadAsByteArrayAsync());
        Assert.Null(await SavedName(banner));
        Assert.False((await storage.Container.GetBlobClient(name).ExistsAsync()).Value);
        using var repeated = await client.DeleteAsync(Path(banner));
        Assert.Equal(HttpStatusCode.NoContent, repeated.StatusCode);
        await AssertReadResponse(client, banner, null);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Removal_AlsoClearsAnAlreadyMissingBlob(bool banner)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        using var created = await Upload(client, banner);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        await storage.Container.DeleteBlobIfExistsAsync((await SavedName(banner))!);
        using var response = await client.DeleteAsync(Path(banner));
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await SavedName(banner));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Reads_RefreshTheSignedUrlWithoutWritingItToSql(bool banner)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        using var created = await Upload(client, banner);
        string initial = await Url(created, banner);
        string originalName = (await SavedName(banner))!;
        factory.Clock.Advance(TimeSpan.FromMinutes(10));
        string? refreshed = banner ? (await client.GetFromJsonAsync<EventDetailsResponse>("/api/events/1"))!.BannerImageUrl
            : (await client.GetFromJsonAsync<ProfileResponse>("/api/profile"))!.ProfilePictureUrl;
        Assert.NotEqual(initial, refreshed);
        Assert.Equal(new Uri(initial).AbsolutePath, new Uri(refreshed!).AbsolutePath);
        Assert.Equal(originalName, await SavedName(banner));
        Assert.DoesNotContain("?", originalName);
    }

    [Fact]
    public async Task Banners_AppearInPublicAndOwnedListsWithoutLeakingRawStorageFields()
    {
        using var factory = Factory();
        using var owner = factory.Client();
        await Login(owner, true);
        var item = await AddEvent("Open");
        using var uploaded = await Upload(owner, true, eventId: item.EventId);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        string url = await Url(uploaded, true);
        using var anonymous = factory.Client(false);
        var list = (await anonymous.GetFromJsonAsync<EventPageResponse>("/api/events"))!;
        Assert.Equal(url, Assert.Single(list.Items, x => x.EventId == item.EventId).BannerImageUrl);
        var own = (await owner.GetFromJsonAsync<EventPageResponse>("/api/events/mine"))!;
        Assert.Equal(url, Assert.Single(own.Items, x => x.EventId == item.EventId).BannerImageUrl);
        using var details = await anonymous.GetAsync($"/api/events/{item.EventId}");
        string json = await details.Content.ReadAsStringAsync();
        Assert.DoesNotContain("bannerBlobName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bannerImageBlobName", json, StringComparison.OrdinalIgnoreCase);
    }

    private AuthenticationApiFactory Factory()
    {
        var factory = new AuthenticationApiFactory(database.ConnectionString);
        factory.Settings["ImageStorage:ConnectionString"] = storage.Connection;
        factory.Settings["ImageStorage:ContainerName"] = storage.Container.Name;
        return factory;
    }

    private static string Path(bool banner, int eventId = 1) => banner ? $"/api/events/{eventId}/banner" : "/api/profile/picture";
    private static string Mime(string extension) => extension == "png" ? "image/png" : "image/jpeg";
    private static MultipartFormDataContent Form(byte[] bytes, string filename = "sample.png", string mime = "image/png", string field = "imageFile")
    {
        var form = new MultipartFormDataContent();
        var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue(mime);
        form.Add(content, field, filename);
        return form;
    }

    private static byte[] Pixels(string extension = "png", int width = 32, int height = 20)
    {
        using var bitmap = new SKBitmap(width, height);
        bitmap.Erase(SKColors.ForestGreen);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(extension == "png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static async Task<HttpResponseMessage> Upload(HttpClient client, bool banner, string extension = "png", int eventId = 1)
    {
        using var form = Form(Pixels(extension), "sample." + extension, Mime(extension));
        return await client.PutAsync(Path(banner, eventId), form);
    }

    private static async Task Login(HttpClient client, bool organiser, string? email = null)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = email ?? (organiser ? "thabo.ndlovu@example.com" : "naledi.mokoena@example.com"), password = "RaceDayDemo!2026"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<string> Url(HttpResponseMessage response, bool banner) => banner
        ? (await response.Content.ReadFromJsonAsync<EventBannerResponse>())!.BannerImageUrl
        : (await response.Content.ReadFromJsonAsync<ProfilePictureResponse>())!.ProfilePictureUrl;

    private async Task<string?> SavedName(bool banner)
    {
        await using var context = database.CreateContext();
        return banner ? (await context.Events.FindAsync(1))!.BannerImageBlobName : (await context.Users.FindAsync(3))!.ProfilePictureBlobName;
    }

    private async Task<List<string>> Objects()
    {
        var names = new List<string>();
        await foreach (var blob in storage.Container.GetBlobsAsync()) names.Add(blob.Name);
        return names;
    }

    private static async Task AssertReadResponse(HttpClient client, bool banner, string? url)
    {
        if (banner) Assert.Equal(url, (await client.GetFromJsonAsync<EventDetailsResponse>("/api/events/1"))!.BannerImageUrl);
        else Assert.Equal(url, (await client.GetFromJsonAsync<ProfileResponse>("/api/profile"))!.ProfilePictureUrl);
    }

    private async Task<Event> AddEvent(string status = "Draft")
    {
        var item = new Event
        {
            OrganiserUserId = 1, EventName = "Image check", EventType = "Running", Description = "Community event.",
            DistanceKm = 10, EventDateTime = DateTime.UtcNow.AddDays(30), EntryClosingDateTime = DateTime.UtcNow.AddDays(29),
            VenueName = "Stadium", AddressLine1 = "1 Main Road", City = "Johannesburg", Province = "Gauteng",
            Status = status, CreatedAt = DateTime.UtcNow
        };
        await using var context = database.CreateContext();
        context.Events.Add(item);
        await context.SaveChangesAsync();
        return item;
    }
}
