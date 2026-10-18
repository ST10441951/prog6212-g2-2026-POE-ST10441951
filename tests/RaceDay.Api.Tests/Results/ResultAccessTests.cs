using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Results;
using RaceDay.Api.Models;
using Xunit;

namespace RaceDay.Api.Tests.Results;

// Role/ownership enforcement and real request validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// Accessed: 18 October 2026.
public sealed partial class ResultTests
{
    [Theory]
    [InlineData("GET", "/api/results/mine", "Participant")]
    [InlineData("GET", "/api/enrolments/{id}/result", "Either")]
    [InlineData("GET", "/api/events/{event}/results", "Organiser")]
    [InlineData("POST", "/api/enrolments/{id}/result", "Organiser")]
    [InlineData("PUT", "/api/enrolments/{id}/result", "Organiser")]
    public async Task EveryEndpoint_RejectsMissingExpiredAndWrongRoleSessions(string method, string template, string role)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var entry = await AddEntry();
        await AddResult(entry);
        string path = template.Replace("{id}", entry.EnrolmentId.ToString()).Replace("{event}", entry.EventId.ToString());
        using var missing = await SendResult(client, method, path);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        if (role != "Either")
        {
            await Login(client, role == "Organiser" ? "naledi.mokoena@example.com" : "thabo.ndlovu@example.com");
            using var wrongRole = await SendResult(client, method, path);
            Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
        }
        await Login(client, role == "Participant" ? "naledi.mokoena@example.com" : "thabo.ndlovu@example.com");
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var expired = await SendResult(client, method, path);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task EntryEndpoints_RejectOtherEventOwnersAndMissingIds(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "ayesha.khan@example.com");
        var entry = await AddEntry();
        var original = await AddResult(entry);
        using var rejected = await SendResult(client, method, Path(entry));
        using var missing = await SendResult(client, method, $"/api/enrolments/{int.MaxValue}/result");
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal("Completed", (await context.Results.FindAsync(original.ResultId))!.ResultStatus);
        Assert.Equal(1, await context.Results.CountAsync());
    }

    [Fact]
    public async Task Details_ArePrivateToTheParticipantAndEventOrganiser()
    {
        using var factory = Factory();
        var entry = await AddEntry();
        var original = await AddResult(entry);
        foreach (var (email, status) in new[]
        {
            ("naledi.mokoena@example.com", HttpStatusCode.OK), ("thabo.ndlovu@example.com", HttpStatusCode.OK),
            ("sipho.dlamini@example.com", HttpStatusCode.Forbidden), ("ayesha.khan@example.com", HttpStatusCode.Forbidden)
        })
        {
            using var client = factory.Client();
            await Login(client, email);
            using var response = await client.GetAsync(Path(entry));
            Assert.Equal(status, response.StatusCode);
            if (status != HttpStatusCode.OK) continue;
            Assert.Equal(original.ResultId, (await response.Content.ReadFromJsonAsync<ResultResponse>())!.ResultId);
            await AssertSafe(response);
        }
    }

    [Fact]
    public async Task MissingResults_Return404AndThereIsNoResultDeleteEndpoint()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry();
        using var get = await client.GetAsync(Path(entry));
        using var put = await client.PutAsJsonAsync(Path(entry), Details());
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        using var created = await client.PostAsJsonAsync(Path(entry), Details());
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var deleted = await client.DeleteAsync(Path(entry));
        Assert.Equal(HttpStatusCode.MethodNotAllowed, deleted.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Results.CountAsync());
    }

    [Fact]
    public async Task EventResults_RejectAnotherOwnerAndMissingEvents()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "ayesha.khan@example.com");
        var entry = await AddEntry();
        await AddResult(entry);
        using var rejected = await client.GetAsync($"/api/events/{entry.EventId}/results");
        using var missing = await client.GetAsync($"/api/events/{int.MaxValue}/results");
        Assert.Equal(HttpStatusCode.Forbidden, rejected.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Writes_RejectMalformedNullAndNonJsonBodies(string method)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var entry = await AddEntry();
        await AddResult(entry);
        foreach (string json in new[] { "{", "null", "" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), Path(entry))
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var form = new HttpRequestMessage(new HttpMethod(method), Path(entry))
            { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["resultStatus"] = "Completed" }) };
        using var unsupported = await client.SendAsync(form);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
    }

    [Fact]
    public async Task Swagger_DescribesAllFiveOperationsAndItsExampleWorks()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (path, method, success) in new[]
        {
            ("/api/results/mine", "get", "200"), ("/api/enrolments/{enrolmentId}/result", "get", "200"),
            ("/api/events/{eventId}/results", "get", "200"), ("/api/enrolments/{enrolmentId}/result", "post", "201"),
            ("/api/enrolments/{enrolmentId}/result", "put", "200")
        })
        {
            var operation = paths.GetProperty(path).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("SessionCookie", out _));
            foreach (string code in new[] { success, "401", "403" })
                Assert.True(operation.GetProperty("responses").TryGetProperty(code, out _));
        }
        string example = document.RootElement.GetProperty("components").GetProperty("schemas")
            .GetProperty("ResultWriteRequest").GetProperty("example").GetRawText();
        await Login(client);
        var entry = await AddEntry();
        using var created = await client.PostAsync(Path(entry), new StringContent(example, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        using var updated = await client.PutAsync(created.Headers.Location, new StringContent(example, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
    }

    private static Task<HttpResponseMessage> SendResult(HttpClient client, string method, string path) => method switch
    {
        "POST" => client.PostAsJsonAsync(path, Details()), "PUT" => client.PutAsJsonAsync(path, Details()), _ => client.GetAsync(path)
    };
}
