using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace RaceDay.Web.Tests;

// Browser-style session, role, redirect and failure checks: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/preventing-open-redirects?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class AccessTests
{
    [Theory]
    [InlineData("/organiser")]
    [InlineData("/participant")]
    [InlineData("/__tests/default-private")]
    public async Task Guests_AreRedirectedBeforeProtectedActionsRun(string route)
    {
        using var factory = new WebApiFactory();
        using var client = factory.Client();
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login?returnUrl=" + Uri.EscapeDataString(route), response.Headers.Location?.OriginalString);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }

    [Theory]
    [InlineData("Organiser", "/organiser", "Organiser home", "/participant")]
    [InlineData("Participant", "/participant", "My account", "/organiser")]
    public async Task RoleHomes_UseVerifiedIdentityAndRejectTheOtherRole(string role, string route, string label, string otherRoute)
    {
        using var factory = Factory(role);
        using var client = factory.Client();
        using var login = await SignIn(client);
        Assert.Equal(route, login.Headers.Location?.OriginalString);
        using var response = await client.GetAsync(route);
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Welcome, Naledi", page);
        Assert.Contains(label, page);
        Assert.Contains($"href=\"{route}\"", page);
        Assert.DoesNotContain($"href=\"{otherRoute}\"", page);
        Assert.Contains("aria-current=\"page\"", page);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        using var forbidden = await client.GetAsync(otherRoute);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        Assert.Null(forbidden.Headers.Location);
        Assert.Contains("Your account does not have access", await forbidden.Content.ReadAsStringAsync());
        using var ownAgain = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.OK, ownAgain.StatusCode);
    }

    [Fact]
    public async Task DefaultMvcProtection_UsesTheSessionIdentityInsteadOfSubmittedIdsAndRoles()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        string page = await client.GetStringAsync("/__tests/default-private?userId=999&role=Organiser");
        Assert.Equal("3:Participant", page);
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    public async Task RejectedApiSessions_ClearLocalAccessAndCannotBeReplayed(int status)
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        string cookie = LocalCookie(login);
        factory.ProfileReply = _ => new HttpResponseMessage((HttpStatusCode)status);
        using var response = await client.GetAsync("/participant");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("RaceDay.Web.Session=;"));
        factory.ProfileReply = _ => WebApiFactory.Profile();
        using var replay = factory.Client();
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        using var rejected = await replay.GetAsync("/participant");
        Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
    }

    [Theory]
    [InlineData("role")]
    [InlineData("user")]
    [InlineData("inactive")]
    [InlineData("invalid-role")]
    public async Task ChangedAccounts_CannotRetainOrGainPrivileges(string change)
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        factory.ProfileReply = _ => WebApiFactory.Profile(
            role: change == "role" ? "Organiser" : change == "invalid-role" ? "Admin" : "Participant",
            userId: change == "user" ? 999 : 3, active: change != "inactive");
        using var response = await client.GetAsync("/participant");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var organiser = await client.GetAsync("/organiser");
        Assert.Equal(HttpStatusCode.Redirect, organiser.StatusCode);
    }

    [Theory]
    [InlineData("server-error")]
    [InlineData("network")]
    [InlineData("timeout")]
    [InlineData("json")]
    [InlineData("incomplete")]
    [InlineData("null")]
    [InlineData("redirect")]
    public async Task UnverifiableSessions_DenyAccessWith503AndRecoverWithoutAnotherLogin(string problem)
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        factory.ProfileReply = _ => problem switch
        {
            "network" => throw new HttpRequestException("private-network-error"),
            "timeout" => throw new TaskCanceledException("private-timeout"),
            "json" => WebApiFactory.Json("not-json"),
            "incomplete" => WebApiFactory.Json("{}"),
            "null" => WebApiFactory.Json("null"),
            "redirect" => new HttpResponseMessage(HttpStatusCode.Redirect),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        };
        using var response = await client.GetAsync("/participant");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Contains("Service temporarily unavailable", page);
        Assert.DoesNotContain("Welcome,", page);
        Assert.DoesNotContain("private-", page);
        Assert.DoesNotContain("signed-in-user", page);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var values) && values.Any(value => value.StartsWith("RaceDay.Web.Session=;")));
        factory.ProfileReply = _ => WebApiFactory.Profile();
        using var recovered = await client.GetAsync("/participant");
        Assert.Equal(HttpStatusCode.OK, recovered.StatusCode);
    }

    [Fact]
    public async Task PublicNavigation_DoesNotDisplayAStaleIdentityWhenChecksFail()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        factory.ProfileReply = _ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        string page = await client.GetStringAsync("/");
        Assert.Contains("Account checks are temporarily unavailable", page);
        Assert.DoesNotContain("signed-in-user", page);
        Assert.DoesNotContain("href=\"/participant\"", page);
    }

    [Fact]
    public async Task UpdatedNames_AreRefreshedAndEncodedWithoutExtendingTheSession()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        factory.Clock.Advance(TimeSpan.FromMinutes(19));
        factory.ProfileReply = _ => WebApiFactory.Profile(name: "<script>private()</script>");
        string page = await client.GetStringAsync("/participant");
        Assert.Contains("&lt;script&gt;private()&lt;/script&gt;", page);
        Assert.DoesNotContain("<script>private()", page);
        factory.Clock.Advance(TimeSpan.FromMinutes(2));
        using var expired = await client.GetAsync("/participant");
        Assert.Equal(HttpStatusCode.Redirect, expired.StatusCode);
    }

    [Fact]
    public async Task ExpiryDuringAnApiCheck_StillRejectsProtectedAccess()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        factory.ProfileReply = _ =>
        {
            factory.Clock.Advance(TimeSpan.FromMinutes(21));
            return WebApiFactory.Profile();
        };
        using var response = await client.GetAsync("/participant");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task ASlowApiCheck_CannotRestoreASessionEndedByAnotherRequest()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        int checks = 0;
        factory.ProfileReply = _ =>
        {
            if (Interlocked.Increment(ref checks) == 1)
            {
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Review request did not finish.");
            }
            return WebApiFactory.Profile();
        };
        var pending = Task.Run(() => client.GetAsync("/participant"));
        try
        {
            Assert.True(entered.Wait(TimeSpan.FromSeconds(10)));
            using var sameSession = factory.Client();
            sameSession.DefaultRequestHeaders.Add("Cookie", LocalCookie(login));
            using var logout = await AccountTests.PostForm(sameSession, "/account/logout", []);
            Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        }
        finally { release.Set(); }
        using var response = await pending;
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task TwoBrowsers_CheckOnlyTheirOwnApiCookies()
    {
        using var factory = Factory();
        int logins = 0;
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login"
            ? Login(factory, "Participant", ++logins) : WebApiFactory.Json(WebApiFactory.EventJson);
        factory.ProfileReply = request => request.Headers.GetValues("Cookie").Single() switch
        {
            "RaceDay.Session=token-1" => WebApiFactory.Profile(name: "Visitor1", userId: 1),
            "RaceDay.Session=token-2" => WebApiFactory.Profile(name: "Visitor2", userId: 2),
            _ => throw new InvalidOperationException("Unexpected API cookie")
        };
        using var first = factory.Client();
        using var second = factory.Client();
        using var firstLogin = await SignIn(first);
        using var secondLogin = await SignIn(second);
        Assert.Contains("Welcome, Visitor1", await first.GetStringAsync("/participant"));
        Assert.Contains("Welcome, Visitor2", await second.GetStringAsync("/participant"));
        using var logout = await AccountTests.PostForm(first, "/account/logout", []);
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        using var rejected = await first.GetAsync("/participant");
        Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
        Assert.Contains("Welcome, Visitor2", await second.GetStringAsync("/participant"));
    }

    [Theory]
    [InlineData("https://outside.example/")]
    [InlineData("//outside.example/")]
    [InlineData("/\\outside.example/")]
    [InlineData("javascript:alert(1)")]
    public async Task Login_RejectsExternalReturnDestinationsOnGetAndPost(string address)
    {
        using var factory = Factory();
        using var client = factory.Client();
        string page = await client.GetStringAsync("/account/login?returnUrl=" + Uri.EscapeDataString(address));
        Assert.DoesNotContain("name=\"returnUrl\"", page);
        var fields = AccountTests.LoginFields();
        fields["returnUrl"] = address;
        using var response = await AccountTests.PostForm(client, "/account/login", fields);
        Assert.Equal("/participant", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task LocalReturnDestination_SurvivesValidationAndLogin()
    {
        using var factory = Factory();
        using var client = factory.Client();
        const string address = "/?eventType=Running&province=Gauteng";
        string page = await client.GetStringAsync("/account/login?returnUrl=" + Uri.EscapeDataString(address));
        Assert.Contains("name=\"returnUrl\"", page);
        Assert.Contains("&amp;province=Gauteng", page);
        var fields = AccountTests.LoginFields();
        fields["returnUrl"] = address;
        fields["Password"] = "";
        using var invalid = await AccountTests.PostForm(client, "/account/login", fields);
        Assert.Contains("name=\"returnUrl\"", await invalid.Content.ReadAsStringAsync());
        fields["Password"] = AccountTests.LoginFields()["Password"];
        using var response = await AccountTests.PostForm(client, "/account/login", fields);
        Assert.Equal(address, response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task ProtectedPosts_RequireTheCorrectRoleAndAntiforgery()
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var guest = await client.PostAsync("/__tests/participant-write", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Redirect, guest.StatusCode);
        Assert.Equal("/account/login", guest.Headers.Location?.OriginalString);
        using var login = await SignIn(client);
        using var missingToken = await client.PostAsync("/__tests/participant-write", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, missingToken.StatusCode);
        using var allowed = await AccountTests.PostForm(client, "/__tests/participant-write", []);
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
        using var organiserFactory = Factory("Organiser");
        using var organiser = organiserFactory.Client();
        using var organiserLogin = await SignIn(organiser);
        using var forbidden = await organiser.PostAsync("/__tests/participant-write", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    private static WebApiFactory Factory(string role = "Participant")
    {
        var factory = new WebApiFactory();
        factory.Reply = request => request.RequestUri!.AbsolutePath switch
        {
            "/api/auth/login" => Login(factory, role),
            "/api/auth/logout" => new HttpResponseMessage(HttpStatusCode.NoContent),
            _ => WebApiFactory.Json(WebApiFactory.EventJson)
        };
        factory.ProfileReply = _ => WebApiFactory.Profile(role);
        return factory;
    }

    private static Task<HttpResponseMessage> SignIn(HttpClient client) =>
        AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());

    private static HttpResponseMessage Login(WebApiFactory factory, string role, int id = 3)
    {
        var response = WebApiFactory.Json(JsonSerializer.Serialize(new
        {
            sessionExpiresAt = factory.Clock.GetUtcNow().AddMinutes(20),
            user = new { userId = id, firstName = "Naledi", role, isActive = true }
        }));
        response.Headers.Add("Set-Cookie", $"RaceDay.Session=token-{id}; path=/; httponly");
        return response;
    }

    private static string LocalCookie(HttpResponseMessage response) => response.Headers.GetValues("Set-Cookie")
        .Last(value => value.StartsWith("RaceDay.Web.Session=")).Split(';')[0];
}

// These actions exist only in the test host, to verify default protection and safe writes.
public sealed class AccessProbeController : Controller
{
    [HttpGet("/__tests/default-private")]
    public IActionResult Identity() => Content(User.FindFirstValue(ClaimTypes.NameIdentifier) + ":" + User.FindFirstValue(ClaimTypes.Role));

    [Authorize(Roles = "Participant")]
    [HttpGet("/__tests/participant-write")]
    public IActionResult Form() => View("~/Views/Account/Login.cshtml", new RaceDay.Web.Models.Accounts.LoginViewModel());

    [Authorize(Roles = "Participant")]
    [HttpPost("/__tests/participant-write")]
    public IActionResult Write() => NoContent();
}
