using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace RaceDay.Api.Tests.Images;

// Requests use the real session and role middleware: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// Accessed: 18 October 2026.
public sealed partial class ImageTests
{
    [Theory]
    [InlineData(false, "PUT")]
    [InlineData(false, "DELETE")]
    [InlineData(true, "PUT")]
    [InlineData(true, "DELETE")]
    public async Task AllImageOperations_RejectMissingExpiredAndWrongRoleSessions(bool banner, string method)
    {
        using var factory = Factory();
        using var anonymous = factory.Client();
        using var missing = await Send(anonymous, method, banner);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        using var client = factory.Client();
        await Login(client, !banner);
        using var wrongRole = await Send(client, method, banner);
        Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
        await Login(client, banner);
        factory.Clock.Advance(TimeSpan.FromMinutes(21));
        using var expired = await Send(client, method, banner);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        Assert.Empty(await Objects());
    }

    [Theory]
    [InlineData(false, "PUT")]
    [InlineData(false, "DELETE")]
    [InlineData(true, "PUT")]
    [InlineData(true, "DELETE")]
    public async Task InactiveAccounts_AreRejectedAndTheirSessionIsRevoked(bool banner, string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, banner);
        await using var context = database.CreateContext();
        await context.Users.Where(x => x.UserId == (banner ? 1 : 3)).ExecuteUpdateAsync(x => x.SetProperty(u => u.IsActive, false));
        using var first = await Send(client, method, banner);
        using var repeated = await Send(client, method, banner);
        Assert.Equal(HttpStatusCode.Forbidden, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, repeated.StatusCode);
        Assert.Empty(await Objects());
    }

    [Theory]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task BannerOperations_RejectAnotherOwnerAndMissingEvents(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, true, "ayesha.khan@example.com");
        using var foreign = await Send(client, method, true);
        using var missing = await Send(client, method, true, int.MaxValue);
        Assert.Equal(HttpStatusCode.Forbidden, foreign.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Null(await SavedName(true));
        Assert.Empty(await Objects());
    }

    [Fact]
    public async Task PictureUpload_CannotChangeAnotherParticipantsImage()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, false);
        using var uploaded = await Upload(client, false);
        Assert.Equal(HttpStatusCode.OK, uploaded.StatusCode);
        await using var context = database.CreateContext();
        Assert.NotNull((await context.Users.FindAsync(3))!.ProfilePictureBlobName);
        Assert.Null((await context.Users.FindAsync(4))!.ProfilePictureBlobName);
        using var target = await client.PutAsync("/api/profile/4/picture", Form(Pixels()));
        Assert.Equal(HttpStatusCode.NotFound, target.StatusCode);
    }

    [Fact]
    public async Task Swagger_ListsAllFourOperationsWithBinaryMultipartInputsAndResponses()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var swagger = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = swagger.RootElement.GetProperty("paths");
        foreach (string path in new[] { "/api/profile/picture", "/api/events/{eventId}/banner" })
        {
            foreach (string method in new[] { "put", "delete" })
            {
                var operation = paths.GetProperty(path).GetProperty(method);
                Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
                Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
                Assert.True(operation.GetProperty("security")[0].TryGetProperty("SessionCookie", out _));
                foreach (string status in new[] { method == "put" ? "200" : "204", "401", "403", "404", "503" })
                    Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
                if (method == "delete") continue;
                foreach (string status in new[] { "400", "413", "415" })
                    Assert.True(operation.GetProperty("responses").TryGetProperty(status, out _));
                var schema = operation.GetProperty("requestBody").GetProperty("content").GetProperty("multipart/form-data").GetProperty("schema");
                var properties = schema.GetProperty("properties");
                var image = properties.EnumerateObject().Single(x => x.Name.Equals("imageFile", StringComparison.OrdinalIgnoreCase)).Value;
                Assert.Equal("string", image.GetProperty("type").GetString());
                Assert.Equal("binary", image.GetProperty("format").GetString());
            }
        }
    }

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, bool banner, int eventId = 1) =>
        method == "PUT" ? Upload(client, banner, eventId: eventId) : client.DeleteAsync(Path(banner, eventId));

    [Fact]
    public async Task Swagger_ExactlyMatchesAllFortyPlannedBusinessOperations()
    {
        using var factory = Factory();
        using var client = factory.Client();
        string plan = await File.ReadAllTextAsync(System.IO.Path.Combine(AppContext.BaseDirectory, "Planning", "API-Endpoint-Plan.md"));
        var expected = Regex.Matches(plan, @"(?m)^\|\s*(GET|POST|PUT|DELETE)\s*\|\s*`([^`]+)`")
            .Select(x => $"{x.Groups[1].Value} {x.Groups[2].Value}").Order().ToArray();
        Assert.Equal(40, expected.Length);
        using var swagger = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var actual = swagger.RootElement.GetProperty("paths").EnumerateObject()
            .Where(x => x.Name.StartsWith("/api/", StringComparison.Ordinal))
            .SelectMany(path => path.Value.EnumerateObject()
                .Where(operation => operation.Name is "get" or "post" or "put" or "delete")
                .Select(operation => $"{operation.Name.ToUpperInvariant()} {path.Name}")).Order().ToArray();
        Assert.Equal(expected, actual);
    }
}
