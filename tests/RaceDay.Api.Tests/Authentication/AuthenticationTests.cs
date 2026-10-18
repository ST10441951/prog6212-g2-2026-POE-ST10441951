using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaceDay.Api.Contracts.Authentication;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Database;
using Xunit;

namespace RaceDay.Api.Tests.Authentication;

// HTTP integration tests and password verification: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.passwordhasher-1.verifyhashedpassword
// Accessed: 18 October 2026.
public sealed class AuthenticationTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private const string Password = "RaceDayTest!2026";
    private const string CookieName = "RaceDay.Session";

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Register_SavesEitherRoleAndHashesThePassword(string role)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var request = Registration(role);
        request["email"] = $"  TEST-{Guid.NewGuid():N}@EXAMPLE.COM  ";
        using var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/profile", response.Headers.Location?.OriginalString);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        var account = (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
        Assert.Equal(role, account.Role);
        Assert.Equal("Test", account.FirstName);
        Assert.Equal("Student", account.LastName);
        Assert.Equal(((string)request["email"]!).Trim().ToLowerInvariant(), account.Email);
        Assert.True(account.IsActive);
        await using var context = database.CreateContext();
        var saved = await context.Users.Include(x => x.Role).SingleAsync(x => x.UserId == account.UserId);
        Assert.Equal(role, saved.Role.RoleName);
        Assert.Equal(new DateOnly(1995, 6, 12), saved.DateOfBirth);
        Assert.Equal("0825550100", saved.PhoneNumber);
        Assert.NotEqual(Password, saved.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed,
            new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, Password));
        await AssertNoCredentials(response);
        using var protectedResponse = await client.GetAsync("/__tests/access/signed-in");
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    [Theory]
    [InlineData("firstName", " ")]
    [InlineData("lastName", "")]
    [InlineData("email", "invalid")]
    [InlineData("password", "Short1!")]
    [InlineData("password", "nouppercase1!")]
    [InlineData("password", "NOLOWERCASE1!")]
    [InlineData("password", "NoDigitsHere!")]
    [InlineData("password", "NoSymbolsHere1")]
    [InlineData("role", "Administrator")]
    [InlineData("role", "participant")]
    [InlineData("role", "")]
    [InlineData("phoneNumber", "not a phone")]
    [InlineData("dateOfBirth", "2999-01-01")]
    public async Task Register_RejectsInvalidFields(string field, string value)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var request = Registration();
        request[field] = value;
        using var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        await using var context = database.CreateContext();
        Assert.False(await context.Users.AnyAsync(x => x.Email == (string)request["email"]!));
    }

    [Theory]
    [InlineData("firstName", 51)]
    [InlineData("lastName", 51)]
    [InlineData("password", 129)]
    [InlineData("phoneNumber", 21)]
    public async Task Register_RejectsOverlongFields(string field, int length)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var request = Registration();
        request[field] = new string('A', length);
        using var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("userId", 1)]
    [InlineData("roleId", 1)]
    [InlineData("isActive", false)]
    [InlineData("passwordHash", "injected")]
    public async Task Register_RejectsFieldsThatMustBeSetByTheServer(string field, object value)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var request = Registration();
        request[field] = value;
        using var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_RejectsTodaysBirthDateAndAcceptsOptionalNullFields()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var request = Registration();
        request["dateOfBirth"] = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        using var invalid = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        request["dateOfBirth"] = null;
        request["phoneNumber"] = " ";
        using var valid = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, valid.StatusCode);
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmailAfterNormalising()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var request = Registration();
        using var first = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        string email = (string)request["email"]!;
        request["email"] = " " + email.ToUpperInvariant() + " ";
        request["role"] = "Organiser";
        using var second = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        await using var context = database.CreateContext();
        Assert.Equal(1, await context.Users.CountAsync(x => x.Email == email));
    }

    [Fact]
    public async Task ConcurrentRegistration_SavesOnlyOneAccount()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var firstClient = factory.Client();
        using var secondClient = factory.Client();
        var request = Registration();
        var responses = await Task.WhenAll(
            firstClient.PostAsJsonAsync("/api/auth/register", request),
            secondClient.PostAsJsonAsync("/api/auth/register", request));
        try
        {
            Assert.Equal(new[] { 201, 409 }, responses.Select(x => (int)x.StatusCode).Order());
            await using var context = database.CreateContext();
            Assert.Equal(1, await context.Users.CountAsync(x => x.Email == (string)request["email"]!));
        }
        finally
        {
            foreach (var response in responses)
                response.Dispose();
        }
    }

    [Theory]
    [InlineData("Organiser", "thabo.ndlovu@example.com")]
    [InlineData("Participant", "naledi.mokoena@example.com")]
    public async Task Login_PreservesIdentityAndEnforcesTheSelectedRole(string role, string email)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = " " + email.ToUpperInvariant() + " ", Password = "RaceDayDemo!2026" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var login = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.Equal(role, login.User.Role);
        Assert.Equal(factory.Clock.GetUtcNow().AddMinutes(20), login.SessionExpiresAt);
        await AssertNoCredentials(response);
        using var ownRole = await client.GetAsync("/__tests/access/" + role.ToLowerInvariant());
        using var otherRole = await client.GetAsync("/__tests/access/" +
            (role == "Organiser" ? "participant" : "organiser"));
        using var identity = await client.GetAsync("/__tests/access/signed-in");
        Assert.Equal(HttpStatusCode.OK, ownRole.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, otherRole.StatusCode);
        Assert.Null(otherRole.Headers.Location);
        Assert.Equal("application/problem+json", otherRole.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await identity.Content.ReadAsStringAsync());
        Assert.Equal(login.User.UserId.ToString(), body.RootElement.GetProperty("userId").GetString());
        Assert.Equal(role, body.RootElement.GetProperty("role").GetString());
    }

    [Fact]
    public async Task Login_StoresIdentityOnServerAndOnlyAProtectedIdInTheCookie()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var response = await Login(client);
        string cookie = Cookie(response);
        string protectedId = cookie[(CookieName.Length + 1)..];
        string key = factory.Services.GetRequiredService<IDataProtectionProvider>()
            .CreateProtector("RaceDay.Authentication.SessionCookie.v1").Unprotect(protectedId);
        Assert.Equal(64, key.Length);
        var cache = factory.Services.GetRequiredService<IDistributedCache>();
        string stored = (await cache.GetStringAsync("RaceDay:Session:" + key))!;
        using var document = JsonDocument.Parse(stored);
        Assert.Equal(new[] { "ExpiresAt", "Role", "UserId" },
            document.RootElement.EnumerateObject().Select(x => x.Name).Order());
        Assert.Equal("Participant", document.RootElement.GetProperty("Role").GetString());
        Assert.True(document.RootElement.GetProperty("UserId").GetInt32() > 0);
        Assert.DoesNotContain("naledi", protectedId);
        await AssertNoCredentials(response);
    }

    [Theory]
    [InlineData("Development", "http", false)]
    [InlineData("Development", "https", true)]
    [InlineData("Production", "https", true)]
    public async Task Login_SetsCookieSecurityFlags(string environment, string scheme, bool secure)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString, environment);
        using var client = factory.Client(scheme: scheme);
        using var response = await Login(client);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string cookie = response.Headers.GetValues("Set-Cookie").Single(x => x.StartsWith(CookieName + "="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(secure, cookie.Contains("; secure", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("missing@example.com", "RaceDayDemo!2026")]
    [InlineData("naledi.mokoena@example.com", "WrongPassword!2026")]
    public async Task Login_RejectsWrongCredentials(string email, string password)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Invalid email or password", body.RootElement.GetProperty("title").GetString());
        await AssertNoCredentials(response);
    }

    [Theory]
    [InlineData("", Password)]
    [InlineData("invalid", Password)]
    [InlineData("naledi.mokoena@example.com", "")]
    public async Task Login_RejectsInvalidFields(string email, string password)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_RejectsInactiveAccountsAfterCheckingThePassword()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        await using (var context = database.CreateContext())
            await context.Users.Where(x => x.UserId == account.UserId).ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.IsActive, false));
        using var wrong = await client.PostAsJsonAsync("/api/auth/login", new { account.Email, Password = "Wrong!2026" });
        using var correct = await client.PostAsJsonAsync("/api/auth/login", new { account.Email, Password });
        Assert.Equal(HttpStatusCode.Unauthorized, wrong.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, correct.StatusCode);
        Assert.False(correct.Headers.Contains("Set-Cookie"));
    }

    [Fact]
    public async Task Login_RehashesAnOlderHashWithoutExposingIt()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        string oldHash;
        await using (var context = database.CreateContext())
        {
            var user = (await context.Users.FindAsync(account.UserId))!;
            var oldHasher = new PasswordHasher<User>(Options.Create(new PasswordHasherOptions { IterationCount = 1000 }));
            user.PasswordHash = oldHash = oldHasher.HashPassword(user, Password);
            await context.SaveChangesAsync();
        }
        using var response = await client.PostAsJsonAsync("/api/auth/login", new { account.Email, Password });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await using var read = database.CreateContext();
        var updated = (await read.Users.FindAsync(account.UserId))!;
        Assert.NotEqual(oldHash, updated.PasswordHash);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(updated, updated.PasswordHash, Password));
        await AssertNoCredentials(response);
    }

    [Fact]
    public async Task Logout_RejectsReplaysAndASecondLogout()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var login = await Login(client);
        string cookie = Cookie(login);
        using var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal("", await logout.Content.ReadAsStringAsync());
        using var second = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        using var replayClient = factory.Client(cookies: false);
        replayClient.DefaultRequestHeaders.Add("Cookie", cookie);
        using var replay = await replayClient.GetAsync("/__tests/access/signed-in");
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task LoginAgain_ReplacesTheSessionAndInvalidatesItsOldCookie()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var first = await Login(client);
        string oldCookie = Cookie(first);
        using var second = await Login(client, "thabo.ndlovu@example.com");
        Assert.NotEqual(oldCookie, Cookie(second));
        using var current = await client.GetAsync("/__tests/access/organiser");
        Assert.Equal(HttpStatusCode.OK, current.StatusCode);
        using var replayClient = factory.Client(cookies: false);
        replayClient.DefaultRequestHeaders.Add("Cookie", oldCookie);
        using var replay = await replayClient.GetAsync("/__tests/access/signed-in");
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Fact]
    public async Task ExpiredSession_IsRejectedWithoutWaiting()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var login = await Login(client);
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var expired = await client.GetAsync("/__tests/access/signed-in");
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
    }

    [Fact]
    public async Task LoggingOutOneBrowser_DoesNotEndAnotherBrowsersSession()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var first = factory.Client();
        using var second = factory.Client();
        using var firstLogin = await Login(first);
        using var secondLogin = await Login(second);
        Assert.NotEqual(Cookie(firstLogin), Cookie(secondLogin));
        using var logout = await first.PostAsync("/api/auth/logout", null);
        using var stillSignedIn = await second.GetAsync("/__tests/access/signed-in");
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.OK, stillSignedIn.StatusCode);
    }

    [Theory]
    [InlineData("disabled", HttpStatusCode.Forbidden)]
    [InlineData("deleted", HttpStatusCode.Unauthorized)]
    [InlineData("roleChanged", HttpStatusCode.Unauthorized)]
    public async Task AccountChanges_RevokeExistingAccess(string change, HttpStatusCode expected)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await client.PostAsJsonAsync("/api/auth/login", new { account.Email, Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        await using (var context = database.CreateContext())
        {
            var user = (await context.Users.FindAsync(account.UserId))!;
            if (change == "deleted")
                context.Users.Remove(user);
            else if (change == "disabled")
                user.IsActive = false;
            else
                user.RoleId = 1;
            await context.SaveChangesAsync();
        }
        using var rejected = await client.GetAsync("/__tests/access/signed-in");
        Assert.Equal(expected, rejected.StatusCode);
        using var replay = await client.GetAsync("/__tests/access/signed-in");
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
    }

    [Theory]
    [InlineData("/api/auth/register")]
    [InlineData("/api/auth/login")]
    public async Task Authentication_RejectsMalformedJsonAndFormBodies(string endpoint)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var malformed = await client.PostAsync(endpoint, new StringContent("{", Encoding.UTF8, "application/json"));
        using var form = await client.PostAsync(endpoint,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["email"] = "test@example.com" }));
        Assert.Equal(HttpStatusCode.BadRequest, malformed.StatusCode);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, form.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoints_RejectMissingOrTamperedCookiesAndClaimHeaders()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client(cookies: false);
        client.DefaultRequestHeaders.Add("X-User-Id", "1");
        client.DefaultRequestHeaders.Add("X-Role", "Organiser");
        client.DefaultRequestHeaders.Add("Cookie", CookieName + "=forged");
        using var response = await client.GetAsync("/__tests/access/organiser");
        using var logout = await client.PostAsync("/api/auth/logout", null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, logout.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Swagger_DescribesAllThreeAuthenticationEndpointsWithoutTestRoutes()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        Assert.Equal(24, paths.EnumerateObject().Count());
        foreach (string endpoint in new[] { "register", "login", "logout" })
        {
            var operation = paths.GetProperty("/api/auth/" + endpoint).GetProperty("post");
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("responses").TryGetProperty(endpoint == "register" ? "201" :
                endpoint == "login" ? "200" : "204", out _));
        }
        Assert.True(paths.GetProperty("/api/auth/logout").GetProperty("post")
            .GetProperty("security")[0].TryGetProperty("SessionCookie", out _));
        Assert.Equal("cookie", document.RootElement.GetProperty("components")
            .GetProperty("securitySchemes").GetProperty("SessionCookie").GetProperty("in").GetString());
    }

    [Fact]
    public async Task Swagger_RequestExamplesWorkWithoutChangingTheirValues()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        using var registration = await client.PostAsync("/api/auth/register", new StringContent(
            schemas.GetProperty("RegisterRequest").GetProperty("example").GetRawText(), Encoding.UTF8, "application/json"));
        using var login = await client.PostAsync("/api/auth/login", new StringContent(
            schemas.GetProperty("LoginRequest").GetProperty("example").GetRawText(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.Created, registration.StatusCode);
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    private static Dictionary<string, object?> Registration(string role = "Participant") => new()
    {
        ["firstName"] = " Test ", ["lastName"] = " Student ",
        ["email"] = $"test-{Guid.NewGuid():N}@example.com", ["password"] = Password,
        ["role"] = role, ["phoneNumber"] = "0825550100", ["dateOfBirth"] = "1995-06-12"
    };

    private static async Task<AccountResponse> Register(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", Registration());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string email = "naledi.mokoena@example.com") =>
        client.PostAsJsonAsync("/api/auth/login", new { email, Password = "RaceDayDemo!2026" });

    private static string Cookie(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response.Headers.GetValues("Set-Cookie").Last(x => x.StartsWith(CookieName + "=")).Split(';')[0];
    }

    private static async Task AssertNoCredentials(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"password\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"passwordHash\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Password, body);
        Assert.DoesNotContain("RaceDayDemo!2026", body);
    }
}
