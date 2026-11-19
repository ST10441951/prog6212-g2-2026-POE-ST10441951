using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Xunit;

namespace RaceDay.Web.Tests;

// Multipart forms, role checks and API-only storage writes: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.net.http.multipartformdatacontent
// Contract: src/RaceDay.Api/Controllers/ImagesController.cs.
// Accessed: 19 November 2026.
public sealed class ImageManagementTests
{
    private const string Banner = "/organiser/events/2/banner", Picture = "/profile/picture";
    private static readonly byte[] Image = [137, 80, 78, 71, 13, 10, 26, 10, 42];

    [Theory]
    [InlineData(Picture, "Organiser")]
    [InlineData(Picture + "/remove", "Organiser")]
    [InlineData(Banner, "Participant")]
    [InlineData(Banner + "/remove", "Participant")]
    public async Task GuestsAndWrongRoles_CannotOpenImageForms(string route, string role)
    {
        using var factory = Factory(role); using var client = factory.Client();
        using var guest = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var login = await SignIn(client); using var denied = await client.GetAsync(route); Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    [Theory]
    [InlineData(Picture, "Participant", "/api/profile/picture", "/profile")]
    [InlineData(Banner, "Organiser", "/api/events/2/banner", "/organiser/events/2")]
    public async Task Upload_ForwardsOnlyTheFileAndCurrentApiCookie(string route, string role, string apiRoute, string destination)
    {
        using var factory = Factory(role); int writes = 0; factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Put) return Reply(request, role);
            writes++; Assert.Equal(apiRoute, request.RequestUri!.AbsolutePath); Assert.Equal("RaceDay.Session=image-token", request.Headers.GetValues("Cookie").Single());
            var content = Assert.IsType<MultipartFormDataContent>(request.Content); var part = Assert.Single(content);
            Assert.Equal("imageFile", part.Headers.ContentDisposition!.Name!.Trim('"')); Assert.Equal("runner.png", part.Headers.ContentDisposition.FileName!.Trim('"'));
            Assert.Equal("image/png", part.Headers.ContentType!.MediaType); Assert.Equal(Image, part.ReadAsByteArrayAsync().GetAwaiter().GetResult());
            return Success(route == Picture);
        };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var response = await Upload(client, route, fields: new() { ["UserId"] = "999", ["EventId"] = "999", ["Page.EventId"] = "999", ["BlobName"] = "someone-else" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.Equal(destination, response.Headers.Location?.OriginalString); Assert.Equal(1, writes);
    }

    [Theory]
    [InlineData(400)]
    [InlineData(413)]
    [InlineData(415)]
    public async Task RejectedUploads_ShowTheEncodedApiReasonAndRequireReselection(int status)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? Problem(status, "Image <rejected>") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await Upload(client, Picture); string html = await response.Content.ReadAsStringAsync();
        Assert.Equal(status, (int)response.StatusCode); Assert.Contains("Image &lt;rejected&gt;", html); Assert.Contains("select the file again", html); Assert.DoesNotContain("blob-key", html);
    }

    [Theory]
    [InlineData(0, 400)]
    [InlineData(5242881, 413)]
    public async Task EmptyAndOversizedFiles_DoNotReachApi(int size, int status)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await Upload(client, Picture, new byte[size]); Assert.Equal(status, (int)response.StatusCode); Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("multiple")]
    public async Task Upload_RequiresExactlyOneImageFile(string mode)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client);
        using var content = new MultipartFormDataContent(); content.Add(new StringContent(await Token(client, Picture)), "__RequestVerificationToken");
        if (mode == "multiple") { content.Add(new ByteArrayContent(Image), "ImageFile", "one.png"); content.Add(new ByteArrayContent(Image), "OtherFile", "two.png"); }
        using var response = await client.PostAsync(Picture, content); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("upload")]
    [InlineData("remove")]
    public async Task MissingAntiforgery_PreventsImageWrites(string action)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete) writes++; return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); using var content = new MultipartFormDataContent(); content.Add(new ByteArrayContent(Image), "ImageFile", "test.png");
        using var response = await client.PostAsync(Picture + (action == "remove" ? "/remove" : ""), content); Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData(Picture, "Participant")]
    [InlineData(Banner, "Organiser")]
    public async Task Removal_IsAReadOnlyConfirmationFollowedByBodylessDelete(string route, string role)
    {
        using var factory = Factory(role); int writes = 0; factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Delete) return Reply(request, role);
            writes++; Assert.Null(request.Content); Assert.Equal("RaceDay.Session=image-token", request.Headers.GetValues("Cookie").Single()); return new(HttpStatusCode.NoContent);
        };
        using var client = factory.Client(); using var login = await SignIn(client); string html = await client.GetStringAsync(route + "/remove"); Assert.Contains("Keep image", html); Assert.Equal(0, writes);
        using var removed = await AccountTests.PostForm(client, route + "/remove", []); Assert.Equal(HttpStatusCode.Redirect, removed.StatusCode); Assert.Equal(1, writes);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:secret@example.com/image.png")]
    public async Task UnreadableUploadConfirmation_AdvisesCheckingSavedImage(string url)
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? WebApiFactory.Json(url == "null" ? "null" : JsonSerializer.Serialize(new { profilePictureUrl = url })) : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await Upload(client, Picture); string html = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("Check the current image", html); Assert.DoesNotContain("javascript:", html); Assert.DoesNotContain("secret@example", html);
    }

    [Theory]
    [InlineData("upload")]
    [InlineData("remove")]
    public async Task StorageFailures_AreFriendlyAndDoNotAutomaticallyRetry(string action)
    {
        using var factory = Factory(); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete) { writes++; return Problem(503, "private-storage-key"); } return Reply(request); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = action == "upload" ? await Upload(client, Picture) : await AccountTests.PostForm(client, Picture + "/remove", []); string html = await response.Content.ReadAsStringAsync(); Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode); Assert.Contains("Check the current image", html); Assert.DoesNotContain("private-storage-key", html); Assert.Equal(1, writes);
    }

    [Fact]
    public async Task UnownedBanner_DoesNotReachImageWrite()
    {
        using var factory = Factory("Organiser"); int writes = 0; factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; return Reply(request, "Organiser"); };
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await Upload(client, "/organiser/events/99/banner"); Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode); Assert.Equal(0, writes);
    }

    [Fact]
    public async Task ExpiredApiUpload_ClearsMvcSession()
    {
        using var factory = Factory(); factory.Reply = request => request.Method == HttpMethod.Put ? Problem(401, "expired") : Reply(request);
        using var client = factory.Client(); using var login = await SignIn(client); using var response = await Upload(client, Picture); Assert.Equal(HttpStatusCode.Redirect, response.StatusCode); Assert.StartsWith("/account/login", response.Headers.Location?.OriginalString); Assert.DoesNotContain("signed-in-user", await client.GetStringAsync("/"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EventForms_SaveEventThenUploadBannerWithoutAddingFileToJson(bool edit)
    {
        using var factory = Factory("Organiser"); var calls = new List<string>(); factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/events" || request.Method == HttpMethod.Put && request.RequestUri!.AbsolutePath == "/api/events/2")
            {
                calls.Add("event"); var body = JsonNode.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult())!; Assert.False(body.AsObject().ContainsKey("bannerImage"));
                var data = JsonNode.Parse(EventBrowsingTests.DetailsJson)!; data["status"] = edit ? "Open" : "Draft"; var response = WebApiFactory.Json(data.ToJsonString()); response.StatusCode = edit ? HttpStatusCode.OK : HttpStatusCode.Created; return response;
            }
            if (request.RequestUri!.AbsolutePath == "/api/events/2/banner" && request.Method == HttpMethod.Put) { calls.Add("banner"); return Success(false); }
            return Reply(request, "Organiser");
        };
        using var client = factory.Client(); using var login = await SignIn(client); using var saved = await Upload(client, edit ? "/organiser/events/2/edit" : "/organiser/events/create", fields: EventManagementTests.Fields(), fileField: "BannerImage"); Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode); Assert.Equal("/organiser/events/2", saved.Headers.Location?.OriginalString); Assert.Equal(new[] { "event", "banner" }, calls);
    }

    [Theory]
    [InlineData(415)]
    [InlineData(503)]
    public async Task BannerFailure_AfterCreationOpensSavedEventsImageForm(int status)
    {
        using var factory = Factory("Organiser"); int creates = 0; factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Post && request.RequestUri!.AbsolutePath == "/api/events") { creates++; var data = JsonNode.Parse(EventBrowsingTests.DetailsJson)!; data["status"] = "Draft"; var response = WebApiFactory.Json(data.ToJsonString()); response.StatusCode = HttpStatusCode.Created; return response; }
            if (request.Method == HttpMethod.Put && request.RequestUri!.AbsolutePath.EndsWith("/banner")) return Problem(status, "Rejected <banner>");
            return Reply(request, "Organiser");
        };
        using var client = factory.Client(); using var login = await SignIn(client); using var saved = await Upload(client, "/organiser/events/create", fields: EventManagementTests.Fields(), fileField: "BannerImage"); Assert.Equal(HttpStatusCode.Redirect, saved.StatusCode); Assert.Equal(Banner, saved.Headers.Location?.OriginalString); string page = await client.GetStringAsync(Banner); Assert.Contains("event was saved", page, StringComparison.OrdinalIgnoreCase); Assert.Equal(1, creates);
    }

    private static WebApiFactory Factory(string role = "Participant") => new() { Reply = request => Reply(request, role), ProfileReply = _ => WebApiFactory.Profile(role, role == "Organiser" ? "Thabo" : "Naledi", role == "Organiser" ? 1 : 3) };
    private static Task<HttpResponseMessage> SignIn(HttpClient client) => AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
    private static HttpResponseMessage Reply(HttpRequestMessage request, string role = "Participant") => request.RequestUri!.AbsolutePath switch
    {
        "/api/auth/login" => Login(role), "/api/profile" => WebApiFactory.Json(ProfileTests.ProfileJson),
        "/api/organiser/dashboard" => WebApiFactory.Json(OrganiserTests.DashboardJson), "/api/events/2" => WebApiFactory.Json(EventBrowsingTests.DetailsJson),
        _ => WebApiFactory.Json(WebApiFactory.EventJson)
    };
    private static HttpResponseMessage Login(string role)
    { var response = WebApiFactory.Json(JsonSerializer.Serialize(new { sessionExpiresAt = DateTimeOffset.UtcNow.AddMinutes(20), user = new { userId = role == "Organiser" ? 1 : 3, firstName = role == "Organiser" ? "Thabo" : "Naledi", role, isActive = true } })); response.Headers.Add("Set-Cookie", "RaceDay.Session=image-token; Path=/; HttpOnly"); return response; }
    private static HttpResponseMessage Success(bool picture) => WebApiFactory.Json(JsonSerializer.Serialize(picture ? new Dictionary<string, string> { ["profilePictureUrl"] = "https://storage.example.test/profile.png?readonly=1" } : new() { ["bannerImageUrl"] = "https://storage.example.test/banner.png?readonly=1" }));
    private static HttpResponseMessage Problem(int status, string title) => new((HttpStatusCode)status) { Content = new StringContent(JsonSerializer.Serialize(new { title }), Encoding.UTF8, "application/problem+json") };
    internal static async Task<string> Token(HttpClient client, string route)
    {
        using var form = await client.GetAsync(route); string html = await form.Content.ReadAsStringAsync(); string tag = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value; string token = WebUtility.HtmlDecode(Regex.Match(tag, "value=\"([^\"]+)\"").Groups[1].Value); Assert.NotEmpty(token); return token;
    }
    internal static async Task<HttpResponseMessage> Upload(HttpClient client, string route, byte[]? bytes = null, Dictionary<string, string>? fields = null, string fileField = "ImageFile")
    {
        using var content = new MultipartFormDataContent(); content.Add(new StringContent(await Token(client, route)), "__RequestVerificationToken");
        foreach (var field in fields ?? []) content.Add(new StringContent(field.Value), field.Key);
        using var file = new ByteArrayContent(bytes ?? Image); file.Headers.ContentType = new MediaTypeHeaderValue("image/png"); content.Add(file, fileField, "runner.png"); return await client.PostAsync(route, content);
    }
}
