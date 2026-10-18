using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Authentication;
using RaceDay.Api.Contracts.Authentication;
using RaceDay.Api.Contracts.Profiles;
using RaceDay.Api.Controllers;
using RaceDay.Api.Models;
using RaceDay.Api.Tests.Authentication;
using RaceDay.Api.Tests.Database;
using Xunit;

namespace RaceDay.Api.Tests.Profiles;

// Exercise requests through the real middleware and SQL Server provider: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims?view=aspnetcore-10.0
// Accessed: 18 October 2026.
public sealed class ProfileTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private const string Password = "ProfileTest!2026";
    private const string NewPassword = "ChangedProfile!2026";

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task Profile_ReturnsOnlyTheSignedInAccountAndPersistsPermittedChanges(string role)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client, role);
        using var otherClient = factory.Client();
        var other = await Register(otherClient, role);
        using var login = await Login(client, account.Email);
        using var initial = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, initial.StatusCode);
        var before = (await initial.Content.ReadFromJsonAsync<ProfileResponse>())!;
        Assert.Equal(account.UserId, before.UserId);
        Assert.Equal(role, before.Role);
        Assert.True(before.IsActive);
        Assert.Null(before.ProfilePictureUrl);
        Assert.Equal(DateTimeKind.Utc, before.CreatedAt.Kind);
        Assert.True(initial.Headers.CacheControl?.NoStore);
        await AssertSafe(initial);

        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        // Query values and invented headers cannot override identity from the authenticated session.
        client.DefaultRequestHeaders.Add("X-User-Id", other.UserId.ToString());
        using var updated = await client.PutAsJsonAsync($"/api/profile?userId={other.UserId}", Details());
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var body = (await updated.Content.ReadFromJsonAsync<ProfileResponse>())!;
        Assert.Equal(account.UserId, body.UserId);
        Assert.Equal("Updated", body.FirstName);
        Assert.Equal("Student", body.LastName);
        Assert.Equal("0835550100", body.PhoneNumber);
        Assert.Equal(new DateOnly(1998, 2, 12), body.DateOfBirth);
        Assert.Equal(account.Email, body.Email);
        Assert.Equal(role, body.Role);
        Assert.Equal(before.CreatedAt, body.CreatedAt);
        await AssertSafe(updated);

        await using var read = database.CreateContext();
        var saved = (await read.Users.FindAsync(account.UserId))!;
        var unchanged = (await read.Users.FindAsync(other.UserId))!;
        Assert.Equal(body.FirstName, saved.FirstName);
        Assert.Equal(body.LastName, saved.LastName);
        Assert.Equal(body.PhoneNumber, saved.PhoneNumber);
        Assert.Equal(body.DateOfBirth, saved.DateOfBirth);
        Assert.Equal(StoredTimestamp(factory.Clock.GetUtcNow()), saved.UpdatedAt);
        Assert.Equal(before.CreatedAt, DateTime.SpecifyKind(saved.CreatedAt, DateTimeKind.Utc));
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, Password));
        Assert.Equal("Original", unchanged.FirstName);
        Assert.Null(unchanged.UpdatedAt);

        using var fetched = await client.GetAsync($"/api/profile?userId={other.UserId}");
        Assert.Equal("Updated", (await fetched.Content.ReadFromJsonAsync<ProfileResponse>())!.FirstName);
        using var forbiddenRoute = await client.GetAsync($"/api/profile/{other.UserId}");
        Assert.Equal(HttpStatusCode.NotFound, forbiddenRoute.StatusCode);
    }

    [Theory]
    [InlineData("omitted")]
    [InlineData("null")]
    [InlineData("blank")]
    public async Task Update_ClearsOptionalPersonalDetails(string mode)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        var details = Details();
        details.Remove("phoneNumber");
        details.Remove("dateOfBirth");
        if (mode != "omitted")
        {
            details["phoneNumber"] = mode == "blank" ? "   " : null;
            details["dateOfBirth"] = null;
        }
        using var response = await client.PutAsJsonAsync("/api/profile", details);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<ProfileResponse>())!;
        Assert.Null(profile.PhoneNumber);
        Assert.Null(profile.DateOfBirth);
        await using var read = database.CreateContext();
        var saved = (await read.Users.FindAsync(account.UserId))!;
        Assert.Null(saved.PhoneNumber);
        Assert.Null(saved.DateOfBirth);
    }

    [Theory]
    [InlineData("firstName", "")]
    [InlineData("firstName", "   ")]
    [InlineData("firstName", null)]
    [InlineData("lastName", "")]
    [InlineData("lastName", "   ")]
    [InlineData("lastName", null)]
    [InlineData("phoneNumber", "not-a-number")]
    [InlineData("phoneNumber", "123456789012345678901")]
    [InlineData("dateOfBirth", "not-a-date")]
    [InlineData("dateOfBirth", "2099-01-01")]
    public async Task Update_RejectsInvalidFieldsWithoutChangingTheAccount(string field, string? value)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        var details = Details();
        details[field] = value;
        using var response = await client.PutAsJsonAsync("/api/profile", details);
        await AssertInvalidAndUnchanged(response, account.UserId);
    }

    [Theory]
    [InlineData("firstName")]
    [InlineData("lastName")]
    public async Task Update_RejectsMissingAndOverlongNames(string field)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        var details = Details();
        details.Remove(field);
        using var missing = await client.PutAsJsonAsync("/api/profile", details);
        await AssertInvalidAndUnchanged(missing, account.UserId);
        details[field] = new string('x', 51);
        using var tooLong = await client.PutAsJsonAsync("/api/profile", details);
        await AssertInvalidAndUnchanged(tooLong, account.UserId);
    }

    [Fact]
    public async Task Update_AcceptsMaximumNameLengthsAndYesterdayButRejectsTodayUtc()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        var details = Details();
        var today = DateOnly.FromDateTime(factory.Clock.GetUtcNow().UtcDateTime);
        details["dateOfBirth"] = today;
        using var invalid = await client.PutAsJsonAsync("/api/profile", details);
        await AssertInvalidAndUnchanged(invalid, account.UserId);
        details["dateOfBirth"] = today.AddDays(-1);
        details["firstName"] = new string('a', 50);
        details["lastName"] = new string('b', 50);
        using var valid = await client.PutAsJsonAsync("/api/profile", details);
        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.Equal(today.AddDays(-1), (await valid.Content.ReadFromJsonAsync<ProfileResponse>())!.DateOfBirth);
    }

    [Theory]
    [InlineData("userId", 1)]
    [InlineData("roleId", 1)]
    [InlineData("role", "Organiser")]
    [InlineData("isActive", false)]
    [InlineData("email", "replacement@example.com")]
    [InlineData("password", "Injected!2026")]
    [InlineData("passwordHash", "injected")]
    [InlineData("createdAt", "2020-01-01T00:00:00Z")]
    [InlineData("updatedAt", "2020-01-01T00:00:00Z")]
    [InlineData("profilePictureBlobName", "private/object.jpg")]
    public async Task Update_RejectsAccountFieldsOutsideThePlan(string field, object value)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        var details = Details();
        details[field] = value;
        using var response = await client.PutAsJsonAsync("/api/profile", details);
        await AssertInvalidAndUnchanged(response, account.UserId);
    }

    [Theory]
    [InlineData("GET", "/api/profile")]
    [InlineData("PUT", "/api/profile")]
    [InlineData("PUT", "/api/profile/password")]
    public async Task ProfileRoutes_RejectMissingForgedExpiredAndInactiveSessions(string method, string route)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        using var missing = await Send(client, method, route);
        Assert.Equal(HttpStatusCode.Unauthorized, missing.StatusCode);
        using var forgedClient = factory.Client(cookies: false);
        forgedClient.DefaultRequestHeaders.Add("Cookie", "RaceDay.Session=forged");
        forgedClient.DefaultRequestHeaders.Add("X-User-Id", "1");
        using var forged = await Send(forgedClient, method, route);
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        factory.Clock.Advance(TimeSpan.FromMinutes(20));
        using var expired = await Send(client, method, route);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        using var newLogin = await Login(client, account.Email);
        await using (var context = database.CreateContext())
            await context.Users.Where(x => x.UserId == account.UserId).ExecuteUpdateAsync(
                setters => setters.SetProperty(x => x.IsActive, false));
        using var inactive = await Send(client, method, route);
        Assert.Equal(HttpStatusCode.Forbidden, inactive.StatusCode);
    }

    [Theory]
    [InlineData("Organiser")]
    [InlineData("Participant")]
    public async Task PasswordChange_HashesTheReplacementAndEndsOnlyTheCurrentSession(string role)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client, role);
        using var login = await Login(client, account.Email);
        string oldCookie = login.Headers.GetValues("Set-Cookie").Last(x => x.StartsWith("RaceDay.Session=")).Split(';')[0];
        using var otherBrowser = factory.Client();
        using var otherLogin = await Login(otherBrowser, account.Email);
        using var unrelatedBrowser = factory.Client();
        var unrelated = await Register(unrelatedBrowser);
        using var unrelatedLogin = await Login(unrelatedBrowser, unrelated.Email);
        string oldHash;
        await using (var read = database.CreateContext())
            oldHash = (await read.Users.FindAsync(account.UserId))!.PasswordHash;
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        using var changed = await client.PutAsJsonAsync("/api/profile/password", PasswordChange());
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        Assert.Equal("", await changed.Content.ReadAsStringAsync());
        Assert.True(changed.Headers.CacheControl?.NoStore);
        await using (var read = database.CreateContext())
        {
            var saved = (await read.Users.FindAsync(account.UserId))!;
            Assert.NotEqual(oldHash, saved.PasswordHash);
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, NewPassword));
            Assert.Equal(PasswordVerificationResult.Failed,
                new PasswordHasher<User>().VerifyHashedPassword(saved, saved.PasswordHash, Password));
            Assert.Equal(StoredTimestamp(factory.Clock.GetUtcNow()), saved.UpdatedAt);
            Assert.Equal(account.Email, saved.Email);
            Assert.Equal("Original", saved.FirstName);
            Assert.Equal(PasswordVerificationResult.Success,
                new PasswordHasher<User>().VerifyHashedPassword((await read.Users.FindAsync(unrelated.UserId))!,
                    (await read.Users.FindAsync(unrelated.UserId))!.PasswordHash, Password));
        }
        using var signedOut = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, signedOut.StatusCode);
        using var replayClient = factory.Client(cookies: false);
        replayClient.DefaultRequestHeaders.Add("Cookie", oldCookie);
        using var replay = await replayClient.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using var otherSession = await otherBrowser.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, otherSession.StatusCode);
        using var wrongLogin = await Login(client, account.Email);
        Assert.Equal(HttpStatusCode.Unauthorized, wrongLogin.StatusCode);
        using var newLogin = await Login(client, account.Email, NewPassword);
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
        await AssertSafe(newLogin);
    }

    [Theory]
    [InlineData("currentPassword", "WrongCurrent!2026")]
    [InlineData("currentPassword", "")]
    [InlineData("currentPassword", null)]
    [InlineData("newPassword", "")]
    [InlineData("newPassword", null)]
    [InlineData("newPassword", "Aa1!xyz")]
    [InlineData("newPassword", "onlylowercase1!")]
    [InlineData("newPassword", "ONLYUPPERCASE1!")]
    [InlineData("newPassword", "MissingNumber!")]
    [InlineData("newPassword", "MissingSymbol123")]
    [InlineData("confirmPassword", "Different!2026")]
    [InlineData("confirmPassword", "")]
    [InlineData("confirmPassword", null)]
    [InlineData("userId", 1)]
    public async Task PasswordChange_RejectsInvalidBodiesWithoutSavingOrLoggingOut(string field, object? value)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        var request = PasswordChange();
        request[field] = value;
        if (field == "newPassword")
            request["confirmPassword"] = value;
        using var response = await client.PutAsJsonAsync("/api/profile/password", request);
        await AssertInvalidAndUnchanged(response, account.UserId);
        using var stillSignedIn = await client.GetAsync("/api/profile");
        Assert.Equal(HttpStatusCode.OK, stillSignedIn.StatusCode);
    }

    [Theory]
    [InlineData("currentPassword")]
    [InlineData("newPassword")]
    [InlineData("confirmPassword")]
    public async Task PasswordChange_RejectsMissingAndOverlongFields(string field)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        var request = PasswordChange();
        request.Remove(field);
        using var missing = await client.PutAsJsonAsync("/api/profile/password", request);
        await AssertInvalidAndUnchanged(missing, account.UserId);
        request[field] = "Aa1!" + new string('x', 125);
        if (field == "newPassword")
            request["confirmPassword"] = request[field];
        using var tooLong = await client.PutAsJsonAsync("/api/profile/password", request);
        await AssertInvalidAndUnchanged(tooLong, account.UserId);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public async Task PasswordChange_AcceptsBoundaryLengthsAndPreservesSpaces(int length)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        string replacement = " Aa1!" + new string('x', length - 6) + " ";
        using var response = await client.PutAsJsonAsync("/api/profile/password",
            new { currentPassword = Password, newPassword = replacement, confirmPassword = replacement });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        using var trimmed = await Login(client, account.Email, replacement.Trim());
        Assert.Equal(HttpStatusCode.Unauthorized, trimmed.StatusCode);
        using var exact = await Login(client, account.Email, replacement);
        Assert.Equal(HttpStatusCode.OK, exact.StatusCode);
    }

    [Theory]
    [InlineData("/api/profile")]
    [InlineData("/api/profile/password")]
    public async Task ProfileWrites_RejectMalformedJsonNullAndNonJsonBodies(string route)
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        foreach (string json in new[] { "{", "null", "" })
        {
            using var invalid = await client.PutAsync(route, new StringContent(json, Encoding.UTF8, "application/json"));
            await AssertInvalidAndUnchanged(invalid, account.UserId);
        }
        using var form = await client.PutAsync(route,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["firstName"] = "Updated" }));
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, form.StatusCode);
    }

    [Fact]
    public async Task RegistrationLocation_LeadsToTheNewUsersProfileAfterLogin()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var registration = Registration();
        using var created = await client.PostAsJsonAsync("/api/auth/register", registration);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var account = (await created.Content.ReadFromJsonAsync<AccountResponse>())!;
        using var login = await Login(client, account.Email);
        using var profile = await client.GetAsync(created.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        Assert.Equal(account.UserId, (await profile.Content.ReadFromJsonAsync<ProfileResponse>())!.UserId);
    }

    [Fact]
    public async Task ProfileActions_Return404IfTheAccountDisappearsAfterAuthentication()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        await using var context = database.CreateContext();
        await context.Users.Where(x => x.UserId == account.UserId).ExecuteDeleteAsync();
        // Direct action calls reproduce the narrow race after middleware has validated the account.
        var controller = new ProfileController(context, new PasswordHasher<User>(),
            factory.Services.GetRequiredService<SessionStore>(), factory.Clock)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                        new[] { new Claim(ClaimTypes.NameIdentifier, account.UserId.ToString()) }, "test"))
                }
            }
        };
        var get = await controller.Get(default);
        var update = await controller.Update(new UpdateProfileRequest { FirstName = "Test", LastName = "Student" }, default);
        var password = await controller.ChangePassword(new ChangePasswordRequest
        {
            CurrentPassword = Password, NewPassword = NewPassword, ConfirmPassword = NewPassword
        }, default);
        foreach (var result in new[] { get.Result, update.Result, password })
            Assert.Equal(404, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task Swagger_DescribesProfileAccessErrorsAndWorkingRequestExamples()
    {
        using var factory = new AuthenticationApiFactory(database.ConnectionString);
        using var client = factory.Client();
        var account = await Register(client);
        using var login = await Login(client, account.Email);
        using var document = JsonDocument.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"));
        var paths = document.RootElement.GetProperty("paths");
        foreach (var (route, method, success) in new[]
        {
            ("/api/profile", "get", "200"), ("/api/profile", "put", "200"),
            ("/api/profile/password", "put", "204")
        })
        {
            var operation = paths.GetProperty(route).GetProperty(method);
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("summary").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
            Assert.True(operation.GetProperty("security")[0].TryGetProperty("SessionCookie", out _));
            foreach (string code in new[] { success, "401", "403", "404" })
                Assert.True(operation.GetProperty("responses").TryGetProperty(code, out _));
            if (method == "put")
            {
                Assert.True(operation.GetProperty("responses").TryGetProperty("400", out _));
                Assert.True(operation.GetProperty("responses").TryGetProperty("415", out _));
            }
        }
        var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");
        using var update = await client.PutAsync("/api/profile", new StringContent(
            schemas.GetProperty("UpdateProfileRequest").GetProperty("example").GetRawText(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        // The password example uses a seeded demonstration account; keep this temporary account's password separate.
        using var seededLogin = await Login(client, "naledi.mokoena@example.com", "RaceDayDemo!2026");
        Assert.Equal(HttpStatusCode.OK, seededLogin.StatusCode);
        using var changed = await client.PutAsync("/api/profile/password", new StringContent(
            schemas.GetProperty("ChangePasswordRequest").GetProperty("example").GetRawText(), Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
    }

    private static Dictionary<string, object?> Details() => new()
    {
        ["firstName"] = " Updated ", ["lastName"] = " Student ",
        ["phoneNumber"] = " 0835550100 ", ["dateOfBirth"] = "1998-02-12"
    };

    // Part 1 maps UpdatedAt to datetime2(0), which rounds to the nearest whole second.
    private static DateTime StoredTimestamp(DateTimeOffset time) => new(
        (time.UtcTicks + TimeSpan.TicksPerSecond / 2) / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond,
        DateTimeKind.Utc);

    private static Dictionary<string, object?> PasswordChange() => new()
    {
        ["currentPassword"] = Password, ["newPassword"] = NewPassword, ["confirmPassword"] = NewPassword
    };

    private static object Registration(string role = "Participant") => new
    {
        firstName = "Original", lastName = "Student", email = $"profile-{Guid.NewGuid():N}@example.com",
        password = Password, role, phoneNumber = "0825550100", dateOfBirth = "1995-06-12"
    };

    private static async Task<AccountResponse> Register(HttpClient client, string role = "Participant")
    {
        using var response = await client.PostAsJsonAsync("/api/auth/register", Registration(role));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
    }

    private static Task<HttpResponseMessage> Login(HttpClient client, string email, string password = Password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password });

    private static Task<HttpResponseMessage> Send(HttpClient client, string method, string route) =>
        method == "GET" ? client.GetAsync(route) : client.PutAsJsonAsync(route,
            route.EndsWith("password") ? PasswordChange() : Details());

    private async Task AssertInvalidAndUnchanged(HttpResponseMessage response, int userId)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        await AssertSafe(response);
        await using var read = database.CreateContext();
        var user = (await read.Users.FindAsync(userId))!;
        Assert.Equal("Original", user.FirstName);
        Assert.Equal("Student", user.LastName);
        Assert.Equal("0825550100", user.PhoneNumber);
        Assert.Equal(new DateOnly(1995, 6, 12), user.DateOfBirth);
        Assert.Null(user.UpdatedAt);
        Assert.Equal(PasswordVerificationResult.Success,
            new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, Password));
    }

    private static async Task AssertSafe(HttpResponseMessage response)
    {
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("\"passwordHash\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"profilePictureBlobName\"", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Password, body);
        Assert.DoesNotContain(NewPassword, body);
        Assert.DoesNotContain("RaceDayDemo!2026", body);
    }
}
