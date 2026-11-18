using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace RaceDay.Web.Tests;

// HTTP form tests, antiforgery tokens and cookie handling: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class AccountTests
{
    private const string Password = "AccountTest!2026";

    [Theory]
    [InlineData("/account/login")]
    [InlineData("/account/register")]
    public async Task AccountPages_ContainProtectedFormsAndAreNotCached(string route)
    {
        using var factory = new WebApiFactory();
        using var client = factory.Client();
        using var response = await client.GetAsync(route);
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("__RequestVerificationToken", page);
        Assert.Contains("type=\"password\"", page);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.Contains("site-header", page);
    }

    [Theory]
    [InlineData("/account/login")]
    [InlineData("/account/register")]
    [InlineData("/account/logout")]
    public async Task AccountPosts_RejectMissingAntiforgeryTokensBeforeCallingTheApi(string route)
    {
        using var factory = new WebApiFactory();
        int apiCalls = 0;
        factory.Reply = _ => { apiCalls++; return WebApiFactory.Json("{}"); };
        using var client = factory.Client();
        using var response = await client.PostAsync(route, new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, apiCalls);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Registration_SendsOnlyApiFieldsForBothRolesAndRedirectsToLogin(string role)
    {
        using var factory = new WebApiFactory();
        JsonElement? sent = null;
        factory.Reply = request =>
        {
            Assert.Equal("/api/auth/register", request.RequestUri!.AbsolutePath);
            Assert.Equal(HttpMethod.Post, request.Method);
            sent = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult()).RootElement.Clone();
            return new HttpResponseMessage(HttpStatusCode.Created);
        };
        using var client = factory.Client();
        var fields = Registration(role);
        fields["UserId"] = "999";
        fields["IsActive"] = "false";
        using var response = await PostForm(client, "/account/register", fields);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login", response.Headers.Location?.OriginalString);
        Assert.NotNull(sent);
        Assert.Equal(role, sent.Value.GetProperty("role").GetString());
        Assert.Equal("1995-06-12", sent.Value.GetProperty("dateOfBirth").GetString());
        Assert.Equal("0825550100", sent.Value.GetProperty("phoneNumber").GetString());
        Assert.Equal(new[] { "dateOfBirth", "email", "firstName", "lastName", "password", "phoneNumber", "role" },
            sent.Value.EnumerateObject().Select(property => property.Name).Order());
        Assert.DoesNotContain(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith("RaceDay.Web.Session="));
        string page = await client.GetStringAsync("/account/login");
        Assert.Contains("Your account has been created", page);
        Assert.DoesNotContain(Password, page);
    }

    [Theory]
    [InlineData("FirstName", "")]
    [InlineData("Email", "invalid-email")]
    [InlineData("ConfirmPassword", "Different!2026")]
    public async Task InvalidRegistration_StaysOnTheFormAndDoesNotSendCredentials(string field, string value)
    {
        using var factory = new WebApiFactory();
        int calls = 0;
        factory.Reply = _ => { calls++; return new HttpResponseMessage(HttpStatusCode.Created); };
        using var client = factory.Client();
        var fields = Registration("Participant");
        fields[field] = value;
        using var response = await PostForm(client, "/account/register", fields);
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, calls);
        Assert.Contains("field-validation-error", page);
        Assert.DoesNotContain(Password, page);
        Assert.DoesNotContain("value=\"Different!2026\"", page);
    }

    [Fact]
    public async Task DuplicateEmail_Returns409WithAFieldMessageAndBlankPasswords()
    {
        using var factory = new WebApiFactory { Reply = _ => new HttpResponseMessage(HttpStatusCode.Conflict) };
        using var client = factory.Client();
        using var response = await PostForm(client, "/account/register", Registration("Participant"));
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("An account already uses that email address", page);
        Assert.DoesNotContain(Password, page);
    }

    [Fact]
    public async Task ApiValidation_IsShownByFieldAndEncoded()
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = WebApiFactory.Json("""{"errors":{"Password":["Use a stronger password."],"DateOfBirth":["<script>private()</script>"]}}""").Content
            }
        };
        using var client = factory.Client();
        using var response = await PostForm(client, "/account/register", Registration("Participant"));
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Use a stronger password.", page);
        Assert.Contains("&lt;script&gt;private()&lt;/script&gt;", page);
        Assert.DoesNotContain("<script>private()", page);
        Assert.DoesNotContain(Password, page);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Login_CreatesAnIsolatedServerSessionWithoutExposingTheApiCookie(string role)
    {
        using var factory = new WebApiFactory();
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login"
            ? LoginReply(factory, role, "Naledi", "private-api-cookie") : WebApiFactory.Json(WebApiFactory.EventJson);
        using var client = factory.Client();
        using var response = await PostForm(client, "/account/login", LoginFields());

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        string cookie = response.Headers.GetValues("Set-Cookie").Single(value => value.StartsWith("RaceDay.Web.Session="));
        Assert.Contains("httponly", cookie.ToLowerInvariant());
        Assert.Contains("samesite=strict", cookie.ToLowerInvariant());
        Assert.Contains("secure", cookie.ToLowerInvariant());
        Assert.DoesNotContain("private-api-cookie", cookie);
        string page = await client.GetStringAsync("/");
        Assert.Contains($"Naledi <span>({role})</span>", page);
        Assert.Contains("Sign out", page);
        Assert.DoesNotContain("private-api-cookie", page);
        Assert.DoesNotContain(Password, page);
        using var otherClient = factory.Client();
        Assert.DoesNotContain("signed-in-user", await otherClient.GetStringAsync("/"));
    }

    [Theory]
    [InlineData(401, "The email address or password is incorrect.")]
    [InlineData(403, "This account is inactive.")]
    public async Task RejectedLogin_HasAFriendlyMessageAndDoesNotCreateASession(int status, string message)
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("private-api-error") }
        };
        using var client = factory.Client();
        using var response = await PostForm(client, "/account/login", LoginFields());
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Contains(message, page);
        Assert.DoesNotContain(Password, page);
        Assert.DoesNotContain("private-api-error", page);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var values) && values.Any(value => value.StartsWith("RaceDay.Web.Session=")));
    }

    [Theory]
    [InlineData("/account/login", 500)]
    [InlineData("/account/login", 503)]
    [InlineData("/account/register", 500)]
    [InlineData("/account/register", 503)]
    public async Task AccountApiFailures_ReturnAFriendly503(string route, int status)
    {
        using var factory = new WebApiFactory
        {
            Reply = _ => new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent("private-server-error") }
        };
        using var client = factory.Client();
        using var response = await PostForm(client, route, route.EndsWith("register") ? Registration("Participant") : LoginFields());
        string page = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("The account service is temporarily unavailable", page);
        Assert.DoesNotContain("private-server-error", page);
        Assert.DoesNotContain(Password, page);
    }

    [Theory]
    [InlineData("missing-cookie")]
    [InlineData("wrong-cookie")]
    [InlineData("duplicate-cookie")]
    [InlineData("expired")]
    [InlineData("unknown-role")]
    [InlineData("inactive")]
    [InlineData("missing-user")]
    [InlineData("invalid-json")]
    public async Task IncompleteLoginResponses_CannotCreateALocalSession(string problem)
    {
        using var factory = new WebApiFactory();
        factory.Reply = _ =>
        {
            var response = LoginReply(factory, problem == "unknown-role" ? "Administrator" : "Participant");
            if (problem is "missing-cookie" or "wrong-cookie") response.Headers.Remove("Set-Cookie");
            if (problem == "wrong-cookie") response.Headers.Add("Set-Cookie", "Wrong.Session=token; path=/");
            if (problem == "duplicate-cookie") response.Headers.Add("Set-Cookie", "RaceDay.Session=second-token; path=/");
            if (problem == "expired") response.Content = WebApiFactory.Json(JsonSerializer.Serialize(new
            {
                sessionExpiresAt = factory.Clock.GetUtcNow().AddMinutes(-1),
                user = new { userId = 3, firstName = "Naledi", role = "Participant", isActive = true }
            })).Content;
            if (problem == "inactive") response.Content = WebApiFactory.Json(JsonSerializer.Serialize(new
            {
                sessionExpiresAt = factory.Clock.GetUtcNow().AddMinutes(20),
                user = new { userId = 3, firstName = "Naledi", role = "Participant", isActive = false }
            })).Content;
            if (problem == "missing-user") response.Content = WebApiFactory.Json("""{"sessionExpiresAt":"2027-01-01T00:00:00Z","user":null}""").Content;
            if (problem == "invalid-json") response.Content = new StringContent("not JSON");
            return response;
        };
        using var client = factory.Client();
        using var response = await PostForm(client, "/account/login", LoginFields());

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.False(response.Headers.TryGetValues("Set-Cookie", out var values) && values.Any(value => value.StartsWith("RaceDay.Web.Session=")));
    }

    [Fact]
    public async Task AnotherLogin_RotatesTheMvcCookieAndPassesTheOldApiCookieOnlyForThatBrowser()
    {
        using var factory = new WebApiFactory();
        var receivedCookies = new List<string>();
        int logins = 0;
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/auth/login") return WebApiFactory.Json(WebApiFactory.EventJson);
            receivedCookies.Add(request.Headers.TryGetValues("Cookie", out var values) ? string.Join(";", values) : "");
            logins++;
            return LoginReply(factory, "Participant", logins == 1 ? "Naledi" : "Sipho", $"api-token-{logins}");
        };
        using var client = factory.Client();
        using var first = await PostForm(client, "/account/login", LoginFields());
        string oldCookie = SessionCookie(first);
        using var second = await PostForm(client, "/account/login", LoginFields());
        string newCookie = SessionCookie(second);

        Assert.NotEqual(oldCookie, newCookie);
        Assert.Equal(new[] { "", "RaceDay.Session=api-token-1" }, receivedCookies);
        Assert.Contains("Sipho <span>(Participant)</span>", await client.GetStringAsync("/"));
        using var replay = factory.Client();
        replay.DefaultRequestHeaders.Add("Cookie", oldCookie);
        Assert.DoesNotContain("signed-in-user", await replay.GetStringAsync("/"));
    }

    [Theory]
    [InlineData(204)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(503)]
    public async Task Logout_UsesTheCurrentApiCookieAndAlwaysClearsTheLocalSession(int upstreamStatus)
    {
        using var factory = new WebApiFactory();
        string? logoutCookie = null;
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/auth/login") return LoginReply(factory);
            if (request.RequestUri.AbsolutePath == "/api/auth/logout")
            {
                logoutCookie = string.Join(";", request.Headers.GetValues("Cookie"));
                return new HttpResponseMessage((HttpStatusCode)upstreamStatus);
            }
            return WebApiFactory.Json(WebApiFactory.EventJson);
        };
        using var client = factory.Client();
        using var login = await PostForm(client, "/account/login", LoginFields());
        string cookie = SessionCookie(login);
        using var response = await PostForm(client, "/account/logout", []);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login", response.Headers.Location?.OriginalString);
        Assert.Equal("RaceDay.Session=api-session-token", logoutCookie);
        Assert.DoesNotContain("signed-in-user", await client.GetStringAsync("/"));
        using var replay = factory.Client();
        replay.DefaultRequestHeaders.Add("Cookie", cookie);
        Assert.DoesNotContain("signed-in-user", await replay.GetStringAsync("/"));
    }

    [Fact]
    public async Task ExpiredSessions_LoseTheirSignedInHeader()
    {
        using var factory = new WebApiFactory();
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/auth/login"
            ? LoginReply(factory) : WebApiFactory.Json(WebApiFactory.EventJson);
        using var client = factory.Client();
        using var login = await PostForm(client, "/account/login", LoginFields());
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        factory.Clock.Advance(TimeSpan.FromMinutes(21));

        Assert.DoesNotContain("signed-in-user", await client.GetStringAsync("/"));
    }

    [Fact]
    public async Task Logout_CannotBeInvokedByGet()
    {
        using var factory = new WebApiFactory();
        using var client = factory.Client();
        using var response = await client.GetAsync("/account/logout");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [Fact]
    public async Task TamperedSessionCookies_DoNotSignInAVisitor()
    {
        using var factory = new WebApiFactory();
        using var client = factory.Client();
        client.DefaultRequestHeaders.Add("Cookie", "RaceDay.Web.Session=invalid-cookie-value");

        Assert.DoesNotContain("signed-in-user", await client.GetStringAsync("/"));
    }

    internal static async Task<HttpResponseMessage> PostForm(HttpClient client, string route, Dictionary<string, string> fields)
    {
        string formRoute = route.EndsWith("logout") ? "/" : route;
        using var formResponse = await client.GetAsync(formRoute);
        string page = await formResponse.Content.ReadAsStringAsync();
        string tag = Regex.Match(page, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value;
        string token = WebUtility.HtmlDecode(Regex.Match(tag, "value=\"([^\"]+)\"").Groups[1].Value);
        Assert.NotEmpty(token);
        var body = new Dictionary<string, string>(fields) { ["__RequestVerificationToken"] = token };
        return await client.PostAsync(route, new FormUrlEncodedContent(body));
    }

    internal static Dictionary<string, string> LoginFields() => new()
    {
        ["Email"] = "naledi.mokoena@example.com", ["Password"] = Password
    };

    private static Dictionary<string, string> Registration(string role) => new()
    {
        ["FirstName"] = "Test", ["LastName"] = "Participant", ["Email"] = "test@example.com",
        ["Password"] = Password, ["ConfirmPassword"] = Password, ["Role"] = role,
        ["PhoneNumber"] = "0825550100", ["DateOfBirth"] = "1995-06-12"
    };

    private static HttpResponseMessage LoginReply(WebApiFactory factory, string role = "Participant",
        string name = "Naledi", string token = "api-session-token")
    {
        var response = WebApiFactory.Json(JsonSerializer.Serialize(new
        {
            sessionExpiresAt = factory.Clock.GetUtcNow().AddMinutes(20),
            user = new { userId = 3, firstName = name, role, isActive = true }
        }));
        response.Headers.Add("Set-Cookie", $"RaceDay.Session={token}; path=/; secure; httponly; samesite=strict");
        return response;
    }

    private static string SessionCookie(HttpResponseMessage response) => response.Headers.GetValues("Set-Cookie")
        .Last(value => value.StartsWith("RaceDay.Web.Session=")).Split(';')[0];
}
