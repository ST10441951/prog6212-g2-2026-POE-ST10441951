using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Enrolments;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Authentication;
using Xunit;

namespace RaceDay.Api.Tests.Enrolments;

// Request validation and role/ownership checks through the HTTP pipeline: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// Accessed: 18 October 2026.
public sealed partial class EnrolmentTests
{
    [Theory]
    [InlineData("POST", "/api/enrolments", "Participant")]
    [InlineData("GET", "/api/enrolments/mine", "Participant")]
    [InlineData("GET", "/api/enrolments/{id}", "Either")]
    [InlineData("PUT", "/api/enrolments/{id}/cancel", "Participant")]
    [InlineData("PUT", "/api/enrolments/{id}/reactivate", "Participant")]
    [InlineData("DELETE", "/api/enrolments/{id}", "Participant")]
    [InlineData("GET", "/api/events/{event}/enrolments", "Organiser")]
    [InlineData("PUT", "/api/enrolments/{id}/bib-number", "Organiser")]
    public async Task EveryOperation_RejectsMissingExpiredAndWrongRoleSessions(string method, string template, string role)
    {
        using var factory = Factory();
        using var client = factory.Client();
        var category = await AddCategory();
        var entry = await AddEntry(category, status: "Cancelled");
        string path = template.Replace("{id}", entry.EnrolmentId.ToString()).Replace("{event}", category.EventId.ToString());
        using var missing = await SendEntry(client, method, path, category);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        if (role != "Either")
        {
            await Login(client, role == "Organiser" ? "naledi.mokoena@example.com" : "thabo.ndlovu@example.com");
            using var wrongRole = await SendEntry(client, method, path, category);
            Assert.Equal(HttpStatusCode.Forbidden, wrongRole.StatusCode);
        }
        await Login(client, role == "Organiser" ? "thabo.ndlovu@example.com" : "naledi.mokoena@example.com");
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var expired = await SendEntry(client, method, path, category);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Theory]
    [InlineData("GET", "")]
    [InlineData("PUT", "/cancel")]
    [InlineData("PUT", "/reactivate")]
    [InlineData("DELETE", "")]
    public async Task ParticipantControls_RejectAnotherParticipantsEntryAndMissingIds(string method, string suffix)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "sipho.dlamini@example.com");
        var category = await AddCategory();
        var entry = await AddEntry(category, bib: "A100");
        using var forbidden = await SendEntry(client, method, $"/api/enrolments/{entry.EnrolmentId}{suffix}", category);
        using var missing = await SendEntry(client, method, $"/api/enrolments/{int.MaxValue}{suffix}", category);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var context = database.CreateContext();
        var saved = (await context.Enrolments.FindAsync(entry.EnrolmentId))!;
        Assert.Equal("Confirmed", saved.Status);
        Assert.Equal("A100", saved.BibNumber);
        Assert.Null(saved.UpdatedAt);
    }

    [Fact]
    public async Task Details_AreLimitedToTheParticipantAndEventOwnerIncludingHistoricalResults()
    {
        using var factory = Factory();
        var category = await AddCategory("Completed");
        var entry = await AddEntry(category);
        await AddResult(entry);
        foreach (var (email, expected) in new[]
        {
            ("naledi.mokoena@example.com", HttpStatusCode.OK), ("thabo.ndlovu@example.com", HttpStatusCode.OK),
            ("sipho.dlamini@example.com", HttpStatusCode.Forbidden), ("ayesha.khan@example.com", HttpStatusCode.Forbidden)
        })
        {
            using var client = factory.Client();
            await Login(client, email);
            using var response = await client.GetAsync($"/api/enrolments/{entry.EnrolmentId}");
            Assert.Equal(expected, response.StatusCode);
            if (expected != HttpStatusCode.OK) continue;
            var saved = (await response.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
            Assert.Equal("Completed", saved.Result!.ResultStatus);
            Assert.Equal(3600, saved.Result.FinishTimeSeconds);
            Assert.Equal(1, saved.Result.OverallPosition);
            Assert.Equal(1, saved.Result.CategoryPosition);
            Assert.Equal(category.EventId, saved.EventId);
            await AssertSafe(response);
        }
    }

    [Theory]
    [InlineData("GET", "event")]
    [InlineData("PUT", "bib")]
    public async Task OrganiserControls_RejectAnotherEventsOwnerAndMissingIds(string method, string operation)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "thabo.ndlovu@example.com");
        var category = await AddCategory(owner: 2);
        var entry = await AddEntry(category);
        string forbiddenPath = operation == "event" ? $"/api/events/{category.EventId}/enrolments" : $"/api/enrolments/{entry.EnrolmentId}/bib-number";
        string missingPath = operation == "event" ? $"/api/events/{int.MaxValue}/enrolments" : $"/api/enrolments/{int.MaxValue}/bib-number";
        using var forbidden = await SendEntry(client, method, forbiddenPath, category);
        using var missing = await SendEntry(client, method, missingPath, category);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        await using var context = database.CreateContext();
        Assert.Null((await context.Enrolments.FindAsync(entry.EnrolmentId))!.BibNumber);
    }

    [Theory]
    [InlineData("eventId", null)]
    [InlineData("eventId", 0)]
    [InlineData("eventId", -1)]
    [InlineData("eventId", 1.5)]
    [InlineData("eventId", 2147483648L)]
    [InlineData("eventCategoryId", null)]
    [InlineData("eventCategoryId", 0)]
    [InlineData("eventCategoryId", -1)]
    [InlineData("eventCategoryId", "category")]
    [InlineData("eventCategoryId", 1.5)]
    [InlineData("eventCategoryId", 2147483648L)]
    public async Task IdFields_AreRequiredPositiveIntegers(string field, object? value)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        var entry = await AddEntry(category, status: "Cancelled");
        var request = new Dictionary<string, object?> { ["eventId"] = category.EventId, ["eventCategoryId"] = category.EventCategoryId };
        if (value is null) request.Remove(field); else request[field] = value;
        using var response = await client.PostAsJsonAsync("/api/enrolments", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        if (field == "eventCategoryId")
        {
            request.Remove("eventId");
            using var reactivate = await client.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/reactivate", request);
            Assert.Equal(HttpStatusCode.BadRequest, reactivate.StatusCode);
        }
        await using var context = database.CreateContext();
        Assert.Equal("Cancelled", (await context.Enrolments.FindAsync(entry.EnrolmentId))!.Status);
        Assert.Equal(1, await context.Enrolments.CountAsync());
    }

    [Theory]
    [InlineData("participantUserId")]
    [InlineData("enrolmentId")]
    [InlineData("status")]
    [InlineData("roleId")]
    [InlineData("enrolmentDate")]
    [InlineData("updatedAt")]
    [InlineData("result")]
    public async Task JsonWrites_RejectIdentityStatusTimestampsAndOtherUnexpectedFields(string field)
    {
        using var factory = Factory();
        using var participant = factory.Client();
        using var organiser = factory.Client();
        await Login(participant);
        await Login(organiser, "thabo.ndlovu@example.com");
        var category = await AddCategory();
        var entry = await AddEntry(category, status: "Cancelled");
        var create = new Dictionary<string, object?> { ["eventId"] = category.EventId, ["eventCategoryId"] = category.EventCategoryId, [field] = 99 };
        var reactivate = new Dictionary<string, object?> { ["eventCategoryId"] = category.EventCategoryId, [field] = 99 };
        var bib = new Dictionary<string, object?> { ["bibNumber"] = "A100", [field] = 99 };
        using var posted = await participant.PostAsJsonAsync("/api/enrolments", create);
        using var reactivated = await participant.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/reactivate", reactivate);
        using var assigned = await organiser.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/bib-number", bib);
        Assert.Equal(HttpStatusCode.BadRequest, posted.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, reactivated.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, assigned.StatusCode);
        await using var context = database.CreateContext();
        var saved = (await context.Enrolments.FindAsync(entry.EnrolmentId))!;
        Assert.Equal(3, saved.ParticipantUserId);
        Assert.Equal("Cancelled", saved.Status);
        Assert.Null(saved.BibNumber);
        Assert.Null(saved.UpdatedAt);
    }

    [Theory]
    [InlineData("event")]
    [InlineData("category")]
    [InlineData("mismatch")]
    public async Task Create_RejectsMissingAndCrossEventCategoryIds(string reason)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client);
        var category = await AddCategory();
        int eventId = reason == "event" ? int.MaxValue : category.EventId;
        int categoryId = reason == "category" ? int.MaxValue : reason == "mismatch" ? (await AddCategory()).EventCategoryId : category.EventCategoryId;
        using var response = await client.PostAsJsonAsync("/api/enrolments", new { eventId, eventCategoryId = categoryId });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await using var context = database.CreateContext();
        Assert.Empty(await context.Enrolments.ToListAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("A 100")]
    [InlineData("-A100")]
    [InlineData("A/100")]
    [InlineData("AAAAAAAAAAAAAAAAAAAAA")]
    public async Task BibNumbers_RejectInvalidValues(string? bibNumber)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "thabo.ndlovu@example.com");
        var entry = await AddEntry(await AddCategory(), bib: "A100");
        using var response = await client.PutAsJsonAsync($"/api/enrolments/{entry.EnrolmentId}/bib-number", new { bibNumber });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal("A100", (await context.Enrolments.FindAsync(entry.EnrolmentId))!.BibNumber);
    }

    [Fact]
    public async Task BibNumbers_AreUniqueWithinTheEventIncludingCancelledHistoryAndCanBeCorrected()
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, "thabo.ndlovu@example.com");
        var category = await AddCategory("Completed");
        var first = await AddEntry(category, status: "Cancelled", bib: "A100");
        var second = await AddEntry(category, participant: 4);
        using var duplicate = await client.PutAsJsonAsync($"/api/enrolments/{second.EnrolmentId}/bib-number", new { bibNumber = " a100 " });
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        using var same = await client.PutAsJsonAsync($"/api/enrolments/{first.EnrolmentId}/bib-number", new { bibNumber = "a100" });
        Assert.Equal(HttpStatusCode.OK, same.StatusCode);
        var other = await AddEntry(await AddCategory());
        using var reuse = await client.PutAsJsonAsync($"/api/enrolments/{other.EnrolmentId}/bib-number", new { bibNumber = "a100" });
        Assert.Equal(HttpStatusCode.OK, reuse.StatusCode);
        using var corrected = await client.PutAsJsonAsync($"/api/enrolments/{second.EnrolmentId}/bib-number", new { bibNumber = new string('B', 20) });
        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        var saved = (await corrected.Content.ReadFromJsonAsync<EnrolmentResponse>())!;
        Assert.Equal(new string('B', 20), saved.BibNumber);
        Assert.Equal("Confirmed", saved.Status);
        Assert.Equal(second.EnrolmentDate, saved.EnrolmentDate);
        Assert.Equal(4, saved.ParticipantUserId);
        Assert.Equal(Now.UtcDateTime, saved.UpdatedAt);
    }

    [Theory]
    [InlineData("POST", "create")]
    [InlineData("PUT", "reactivate")]
    [InlineData("PUT", "bib-number")]
    public async Task JsonWrites_RejectMalformedNullAndNonJsonBodies(string method, string action)
    {
        using var factory = Factory();
        using var client = factory.Client();
        await Login(client, action == "bib-number" ? "thabo.ndlovu@example.com" : "naledi.mokoena@example.com");
        var category = await AddCategory();
        var entry = await AddEntry(category, status: "Cancelled");
        string path = action == "create" ? "/api/enrolments" : $"/api/enrolments/{entry.EnrolmentId}/{action}";
        foreach (string json in new[] { "{", "null", "" })
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), path)
                { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        using var form = new HttpRequestMessage(new HttpMethod(method), path)
            { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["status"] = "Confirmed" }) };
        using var unsupported = await client.SendAsync(form);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, unsupported.StatusCode);
    }

    private static Task<HttpResponseMessage> SendEntry(HttpClient client, string method, string path, EventCategory category) => method switch
    {
        "POST" => client.PostAsJsonAsync(path, EntryBody(category)),
        "DELETE" => client.DeleteAsync(path), "GET" => client.GetAsync(path),
        _ when path.EndsWith("/bib-number", StringComparison.Ordinal) => client.PutAsJsonAsync(path, new { bibNumber = "A100" }),
        _ when path.EndsWith("/reactivate", StringComparison.Ordinal) => client.PutAsJsonAsync(path, new { eventCategoryId = category.EventCategoryId }),
        _ => client.PutAsync(path, null)
    };
}
