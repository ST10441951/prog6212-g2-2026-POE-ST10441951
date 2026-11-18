using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Xunit;

namespace RaceDay.Web.Tests;

// Entry state changes, safe forms and ownership: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Controllers/EnrolmentsController.cs.
// Accessed: 18 November 2026.
public sealed class EntryChangeTests
{
    [Theory]
    [InlineData("cancel")]
    [InlineData("reactivate")]
    [InlineData("delete")]
    public async Task Changes_UseTheExistingEntryAndCorrectVerbThenShowSavedState(string action)
    {
        using var factory = Factory();
        string status = action == "reactivate" ? "Cancelled" : "Confirmed";
        int category = 3, changes = 0;
        bool deleted = false;
        factory.Reply = request =>
        {
            string path = request.RequestUri!.AbsolutePath;
            if (path == "/api/enrolments/12" && request.Method == HttpMethod.Get)
                return deleted ? new HttpResponseMessage(HttpStatusCode.NotFound) : Entry(status, category);
            if (path.StartsWith("/api/enrolments") && request.Method != HttpMethod.Get)
            {
                changes++;
                Assert.Equal("RaceDay.Session=entry-change-token", request.Headers.GetValues("Cookie").Single());
                if (action == "delete")
                {
                    Assert.Equal("/api/enrolments/12", path);
                    Assert.Equal(HttpMethod.Delete, request.Method);
                    Assert.Null(request.Content);
                    deleted = true;
                    return new HttpResponseMessage(HttpStatusCode.NoContent);
                }
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("/api/enrolments/12/" + action, path);
                if (action == "reactivate")
                {
                    var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                    Assert.Equal(new[] { "eventCategoryId" }, body.RootElement.EnumerateObject().Select(field => field.Name));
                    category = body.RootElement.GetProperty("eventCategoryId").GetInt32();
                    Assert.Equal(4, category);
                    status = "Confirmed";
                }
                else { Assert.Null(request.Content); status = "Cancelled"; }
                return Entry(status, category);
            }
            if (path == "/api/enrolments/mine")
                return WebApiFactory.Json(JsonSerializer.Serialize(new
                {
                    page = 1, pageSize = 10, totalCount = 0, totalPages = 0, items = Array.Empty<object>()
                }));
            return Reply(factory, request);
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var confirmation = await client.GetAsync("/entries/12/" + action);
        string form = await confirmation.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, confirmation.StatusCode);
        Assert.Equal(0, changes);
        Assert.Contains("__RequestVerificationToken", form);
        Assert.Contains("no-store", confirmation.Headers.CacheControl!.ToString());
        if (action == "delete") Assert.Contains("permanently removes", form);
        using var response = await Post(client, action, new()
        {
            ["EventCategoryId"] = "4", ["EventId"] = "999", ["ParticipantUserId"] = "999", ["EnrolmentId"] = "999", ["Status"] = "Cancelled"
        });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        string destination = action == "delete" ? "/entries" : "/entries/12";
        Assert.Equal(destination, response.Headers.Location?.OriginalString);
        string page = await client.GetStringAsync(destination);
        Assert.Contains(action == "delete" ? "Your entry has been deleted" : action == "cancel" ? "Your entry has been cancelled" : "Your entry has been reactivated", page);
        if (action != "delete") Assert.Contains(status, page);
        Assert.Equal(1, changes);
        await client.GetStringAsync(destination);
        Assert.Equal(1, changes);
        if (deleted)
        {
            using var missing = await client.GetAsync("/entries/12");
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("reactivate")]
    [InlineData("delete")]
    public async Task GuestsOrganisersAndMissingAntiforgery_CannotChangeAnEntry(string action)
    {
        using var factory = Factory(role: "Organiser");
        using var organiser = factory.Client();
        using var guest = await organiser.PostAsync("/entries/12/" + action, new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        using var organiserLogin = await SignIn(organiser);
        using var deniedPage = await organiser.GetAsync("/entries/12/" + action);
        using var deniedPost = await organiser.PostAsync("/entries/12/" + action, new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Forbidden, deniedPage.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, deniedPost.StatusCode);
        using var participantFactory = Factory();
        using var participant = participantFactory.Client();
        using var participantLogin = await SignIn(participant);
        using var csrf = await participant.PostAsync("/entries/12/" + action, new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, csrf.StatusCode);
    }

    [Theory]
    [InlineData("cancel", 401)]
    [InlineData("cancel", 403)]
    [InlineData("cancel", 404)]
    [InlineData("delete", 401)]
    [InlineData("delete", 403)]
    [InlineData("delete", 404)]
    [InlineData("reactivate", 401)]
    [InlineData("reactivate", 403)]
    [InlineData("reactivate", 404)]
    public async Task EntryAccess_IsCheckedBeforeAnyWrite(string action, int status)
    {
        using var factory = Factory();
        int writes = 0;
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/enrolments/12") return Problem(status, "private-other-entry");
            if (request.RequestUri.AbsolutePath.StartsWith("/api/enrolments") && request.Method != HttpMethod.Get) writes++;
            return Reply(factory, request);
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await Post(client, action, new() { ["EventCategoryId"] = "3" });
        Assert.Equal(status == 401 ? HttpStatusCode.Redirect : (HttpStatusCode)status, response.StatusCode);
        Assert.Equal(0, writes);
        Assert.DoesNotContain("private-other-entry", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("cancel", "Cancellation requires a Confirmed entry, open entries and no result")]
    [InlineData("delete", "Deletion requires a Confirmed entry, open entries and no result")]
    [InlineData("reactivate", "This category is full")]
    [InlineData("reactivate", "<script>private()</script>")]
    public async Task ApiConflicts_KeepTheEntryAndShowEncodedReasons(string action, string reason)
    {
        using var factory = Factory(action == "reactivate" ? "Cancelled" : "Confirmed");
        factory.Reply = request => request.Method == HttpMethod.Put || request.Method == HttpMethod.Delete
            ? Problem(409, reason) : Reply(factory, request, action == "reactivate" ? "Cancelled" : "Confirmed");
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await Post(client, action, new() { ["EventCategoryId"] = "3" });
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains(WebUtility.HtmlEncode(reason), page);
        Assert.DoesNotContain("<script>private()", page);
        Assert.DoesNotContain("Your entry has been", page);
        if (action == "reactivate")
            Assert.Contains("selected=\"selected\"", Regex.Match(page, "<option[^>]*value=\"3\"[^>]*>").Value);
    }

    [Theory]
    [InlineData("cancel", "server")]
    [InlineData("cancel", "network")]
    [InlineData("cancel", "timeout")]
    [InlineData("cancel", "unexpected")]
    [InlineData("delete", "server")]
    [InlineData("delete", "network")]
    [InlineData("delete", "timeout")]
    [InlineData("delete", "unexpected")]
    [InlineData("reactivate", "server")]
    [InlineData("reactivate", "network")]
    [InlineData("reactivate", "timeout")]
    [InlineData("reactivate", "unexpected")]
    public async Task UnconfirmedChanges_Show503AndNeverClaimSuccess(string action, string failure)
    {
        using var factory = Factory();
        int writes = 0;
        factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Put && request.Method != HttpMethod.Delete) return Reply(factory, request);
            writes++;
            return failure switch
            {
                "network" => throw new HttpRequestException("private-network-error"),
                "timeout" => throw new TaskCanceledException("private-timeout-error"),
                "unexpected" => new HttpResponseMessage(HttpStatusCode.Accepted),
                _ => Problem(500, "private-service-error")
            };
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await Post(client, action, new() { ["EventCategoryId"] = "3" });
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, writes);
        Assert.Contains("Check My entries before trying again", page);
        Assert.DoesNotContain("private-", page);
        Assert.DoesNotContain("Your entry has been", page);
    }

    [Theory]
    [InlineData("cancel", "user")]
    [InlineData("cancel", "entry")]
    [InlineData("cancel", "event")]
    [InlineData("cancel", "status")]
    [InlineData("reactivate", "user")]
    [InlineData("reactivate", "entry")]
    [InlineData("reactivate", "event")]
    [InlineData("reactivate", "status")]
    [InlineData("reactivate", "category")]
    public async Task InconsistentChangeResponses_AreNotPresentedAsSaved(string action, string mismatch)
    {
        using var factory = Factory();
        factory.Reply = request =>
        {
            if (request.Method != HttpMethod.Put) return Reply(factory, request);
            var body = JsonNode.Parse(EntryTests.EntryJson)!;
            body["status"] = action == "cancel" ? "Cancelled" : "Confirmed";
            if (mismatch == "user") body["participantUserId"] = 999;
            if (mismatch == "entry") body["enrolmentId"] = 999;
            if (mismatch == "event") body["eventId"] = 999;
            if (mismatch == "category") body["eventCategoryId"] = 999;
            if (mismatch == "status") body["status"] = "Unknown";
            return WebApiFactory.Json(body.ToJsonString());
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await Post(client, action, new() { ["EventCategoryId"] = "3" });
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("Your entry has been", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("invalid")]
    public async Task InvalidReactivationSelections_DoNotReachTheWriteEndpoint(string value)
    {
        using var factory = Factory("Cancelled");
        int changes = 0;
        factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Put) changes++;
            return Reply(factory, request, "Cancelled");
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await Post(client, "reactivate", new() { ["EventCategoryId"] = value });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, changes);
        Assert.Contains("field-validation-error", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData("cancel")]
    [InlineData("delete")]
    public async Task CancelledEntries_KeepTheirHistoryAndHaveNoCancelOrDeleteSubmitButton(string action)
    {
        using var factory = Factory("Cancelled");
        using var client = factory.Client();
        using var login = await SignIn(client);
        string page = await client.GetStringAsync("/entries/12/" + action);
        Assert.Contains("Only a Confirmed entry", page);
        Assert.DoesNotContain(action == "cancel" ? ">Cancel entry</button>" : ">Delete permanently</button>", page);
    }

    [Theory]
    [InlineData("hidden-event")]
    [InlineData("closed-event")]
    [InlineData("no-categories")]
    public async Task Reactivation_HandlesUnavailablePublicCategoriesWithoutLosingPrivateHistory(string condition)
    {
        using var factory = Factory("Cancelled");
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/events/2") return Reply(factory, request, "Cancelled");
            if (condition == "hidden-event") return new HttpResponseMessage(HttpStatusCode.NotFound);
            var body = JsonNode.Parse(EventBrowsingTests.DetailsJson)!;
            if (condition == "closed-event") body["status"] = "Closed";
            else body["categories"] = new JsonArray();
            return WebApiFactory.Json(body.ToJsonString());
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.GetAsync("/entries/12/reactivate");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("No categories are currently available", page);
        Assert.DoesNotContain(">Reactivate entry</button>", page);
        Assert.Contains("href=\"/entries/12\"", page);
    }

    [Fact]
    public async Task StaleConfirmation_StillUsesTheApiRuleAtSubmission()
    {
        using var factory = Factory();
        bool cancelled = false;
        factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Put) return Problem(409, "The entry has already changed");
            return Reply(factory, request, cancelled ? "Cancelled" : "Confirmed");
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        await client.GetStringAsync("/entries/12/cancel");
        cancelled = true;
        using var response = await Post(client, "cancel", []);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("already changed", await response.Content.ReadAsStringAsync());
    }

    private static WebApiFactory Factory(string status = "Confirmed", string role = "Participant")
    {
        var factory = new WebApiFactory();
        factory.ProfileReply = _ => WebApiFactory.Profile(role);
        factory.Reply = request => Reply(factory, request, status, role);
        return factory;
    }

    private static HttpResponseMessage Reply(WebApiFactory factory, HttpRequestMessage request, string status = "Confirmed", string role = "Participant")
    {
        if (request.RequestUri!.AbsolutePath == "/api/auth/login")
        {
            var response = WebApiFactory.Json(JsonSerializer.Serialize(new
            {
                sessionExpiresAt = factory.Clock.GetUtcNow().AddMinutes(20),
                user = new { userId = 3, firstName = "Naledi", role, isActive = true }
            }));
            response.Headers.Add("Set-Cookie", "RaceDay.Session=entry-change-token; path=/");
            return response;
        }
        if (request.RequestUri.AbsolutePath == "/api/enrolments/12") return Entry(status);
        if (request.RequestUri.AbsolutePath == "/api/events/2") return WebApiFactory.Json(EventBrowsingTests.DetailsJson);
        return WebApiFactory.Json(WebApiFactory.EventJson);
    }

    private static HttpResponseMessage Entry(string status, int category = 3)
    {
        var body = JsonNode.Parse(EntryTests.EntryJson)!;
        body["status"] = status;
        body["eventCategoryId"] = category;
        return WebApiFactory.Json(body.ToJsonString());
    }

    private static HttpResponseMessage Problem(int status, string title) => new((HttpStatusCode)status)
    {
        Content = WebApiFactory.Json(JsonSerializer.Serialize(new { title, status })).Content
    };

    private static Task<HttpResponseMessage> SignIn(HttpClient client) =>
        AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());

    private static async Task<HttpResponseMessage> Post(HttpClient client, string action, Dictionary<string, string> fields)
    {
        string page = await client.GetStringAsync("/");
        string tag = Regex.Match(page, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value;
        string token = WebUtility.HtmlDecode(Regex.Match(tag, "value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        return await client.PostAsync("/entries/12/" + action,
            new FormUrlEncodedContent(new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token }));
    }
}
