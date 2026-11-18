using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace RaceDay.Web.Tests;

// Private forms, session invalidation and encoded responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Profiles/.
// Accessed: 18 November 2026.
public sealed class ProfileTests
{
    private const string OldPassword = "ProfileOld!2026", NewPassword = "ProfileNew!2026";
    internal const string ProfileJson = """
        {"userId":3,"firstName":"Naledi","lastName":"Mokoena","email":"naledi@example.com",
         "phoneNumber":"0825550101","dateOfBirth":"1995-06-12","profilePictureUrl":null,
         "role":"Participant","isActive":true,"createdAt":"2026-09-19T12:00:00Z"}
        """;

    [Theory]
    [InlineData("/profile")]
    [InlineData("/profile/edit")]
    [InlineData("/profile/password")]
    public async Task Guests_CannotReadOrWriteProfilePages(string route)
    {
        using var factory = Factory();
        using var client = factory.Client();
        using var response = await client.GetAsync(route);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        if (route != "/profile")
        {
            using var post = await client.PostAsync(route, new FormUrlEncodedContent([]));
            Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        }
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task BothRoles_CanReadTheirOwnProfileAndOpenProtectedForms(string role)
    {
        using var factory = Factory(role);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.GetAsync("/profile?userId=999&role=Admin");
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Naledi Mokoena", page);
        Assert.Contains("naledi@example.com", page);
        Assert.Contains("12 June 1995", page);
        Assert.Contains(role, page);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
        Assert.DoesNotContain("profile-api-token", page);
        foreach (string route in new[] { "/profile/edit", "/profile/password" })
        {
            string form = await client.GetStringAsync(route);
            Assert.Contains("__RequestVerificationToken", form);
        }
    }

    [Theory]
    [InlineData("/profile/edit")]
    [InlineData("/profile/password")]
    public async Task MissingAntiforgery_RejectsWritesBeforeCallingTheApi(string route)
    {
        using var factory = Factory();
        int writes = 0;
        factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Put) writes++;
            return Reply(factory, request);
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.PostAsync(route, new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task ProfileUpdates_SendOnlyEditableFieldsAndRefreshTheSavedName(string role)
    {
        using var factory = Factory(role);
        string firstName = "Naledi";
        factory.ProfileReply = _ => WebApiFactory.Profile(role, firstName);
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath != "/api/profile") return Reply(factory, request, role);
            Assert.Equal("RaceDay.Session=profile-api-token", request.Headers.GetValues("Cookie").Single());
            var body = JsonNode.Parse(ProfileJson)!;
            body["role"] = role;
            if (request.Method == HttpMethod.Put)
            {
                var sent = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert.Equal(new[] { "dateOfBirth", "firstName", "lastName", "phoneNumber" }, sent.RootElement.EnumerateObject().Select(field => field.Name).Order());
                firstName = sent.RootElement.GetProperty("firstName").GetString()!;
                Assert.Equal("1994-02-15", sent.RootElement.GetProperty("dateOfBirth").GetString());
            }
            body["firstName"] = firstName;
            return WebApiFactory.Json(body.ToJsonString());
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        var fields = EditFields();
        fields["FirstName"] = "Updated";
        fields["UserId"] = "999"; fields["Email"] = "changed@example.com"; fields["Role"] = "Admin"; fields["IsActive"] = "false";
        using var response = await AccountTests.PostForm(client, "/profile/edit", fields);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/profile", response.Headers.Location?.OriginalString);
        string page = await client.GetStringAsync("/profile");
        Assert.Contains("Your profile has been updated", page);
        Assert.Contains("Updated <span>(" + role + ")</span>", page);
        Assert.Contains("naledi@example.com", page);
        Assert.DoesNotContain("changed@example.com", page);
    }

    [Fact]
    public async Task BlankOptionalFields_AreSentAsNullToClearThem()
    {
        using var factory = Factory();
        bool checkedBody = false;
        factory.Reply = request =>
        {
            if (request.Method == HttpMethod.Put)
            {
                var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("phoneNumber").ValueKind);
                Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("dateOfBirth").ValueKind);
                checkedBody = true;
                return WebApiFactory.Json(ProfileJson);
            }
            return Reply(factory, request);
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        var fields = EditFields(); fields["PhoneNumber"] = ""; fields["DateOfBirth"] = "";
        using var response = await AccountTests.PostForm(client, "/profile/edit", fields);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.True(checkedBody);
    }

    [Theory]
    [InlineData("FirstName", "")]
    [InlineData("PhoneNumber", "invalid-phone")]
    [InlineData("DateOfBirth", "not-a-date")]
    public async Task InvalidProfileFields_DoNotReachTheApi(string field, string value)
    {
        using var factory = Factory();
        int writes = 0;
        factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; return Reply(factory, request); };
        using var client = factory.Client();
        using var login = await SignIn(client);
        var fields = EditFields(); fields[field] = value;
        using var response = await AccountTests.PostForm(client, "/profile/edit", fields);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, writes);
    }

    [Theory]
    [InlineData("Participant")]
    [InlineData("Organiser")]
    public async Task PasswordSuccess_SendsOnlyPasswordFieldsAndEndsTheSession(string role)
    {
        using var factory = Factory(role);
        factory.Reply = request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/profile/password")
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("RaceDay.Session=profile-api-token", request.Headers.GetValues("Cookie").Single());
                var body = JsonDocument.Parse(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
                Assert.Equal(new[] { "confirmPassword", "currentPassword", "newPassword" }, body.RootElement.EnumerateObject().Select(field => field.Name).Order());
                Assert.Equal(OldPassword, body.RootElement.GetProperty("currentPassword").GetString());
                Assert.Equal(NewPassword, body.RootElement.GetProperty("newPassword").GetString());
                return new HttpResponseMessage(HttpStatusCode.NoContent);
            }
            return Reply(factory, request, role);
        };
        using var client = factory.Client();
        using var login = await SignIn(client);
        string oldCookie = login.Headers.GetValues("Set-Cookie").Last(value => value.StartsWith("RaceDay.Web.Session=")).Split(';')[0];
        var fields = PasswordFields(); fields["UserId"] = "999"; fields["Email"] = "changed@example.com";
        using var response = await AccountTests.PostForm(client, "/profile/password", fields);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/account/login", response.Headers.Location?.OriginalString);
        string page = await client.GetStringAsync("/account/login");
        Assert.Contains("Your password has been changed", page);
        Assert.DoesNotContain(OldPassword, page); Assert.DoesNotContain(NewPassword, page);
        using var replay = factory.Client(); replay.DefaultRequestHeaders.Add("Cookie", oldCookie);
        using var rejected = await replay.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.Redirect, rejected.StatusCode);
    }

    [Theory]
    [InlineData("CurrentPassword", "")]
    [InlineData("NewPassword", "short")]
    [InlineData("ConfirmPassword", "Different!2026")]
    public async Task InvalidPasswords_AreClearedWithoutCallingTheApi(string field, string value)
    {
        using var factory = Factory();
        int writes = 0;
        factory.Reply = request => { if (request.Method == HttpMethod.Put) writes++; return Reply(factory, request); };
        using var client = factory.Client();
        using var login = await SignIn(client);
        var fields = PasswordFields(); fields[field] = value;
        using var response = await AccountTests.PostForm(client, "/profile/password", fields);
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, writes);
        Assert.DoesNotContain(OldPassword, page); Assert.DoesNotContain(NewPassword, page);
        Assert.DoesNotContain("Different!2026", page);
    }

    [Theory]
    [InlineData("/profile/edit", "DateOfBirth")]
    [InlineData("/profile/password", "CurrentPassword")]
    [InlineData("/profile/password", "NewPassword")]
    public async Task ApiValidation_IsShownByFieldAndEncoded(string route, string field)
    {
        using var factory = Factory();
        factory.Reply = request => request.Method == HttpMethod.Put ? new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = WebApiFactory.Json(JsonSerializer.Serialize(new { errors = new Dictionary<string, string[]> { [field] = ["<script>Check this field</script>"] } })).Content
        } : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, route, route.EndsWith("edit") ? EditFields() : PasswordFields());
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("&lt;script&gt;Check this field&lt;/script&gt;", page);
        Assert.DoesNotContain("<script>Check", page);
        Assert.DoesNotContain(OldPassword, page); Assert.DoesNotContain(NewPassword, page);
    }

    [Theory]
    [InlineData("other-user")]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("json")]
    public async Task IncompleteOrOtherProfiles_AreNotDisplayed(string problem)
    {
        using var factory = Factory();
        var body = JsonNode.Parse(ProfileJson)!; body["userId"] = 999; body["email"] = "private-other@example.com";
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/profile"
            ? WebApiFactory.Json(problem switch { "missing" => "{}", "null" => "null", "json" => "not-json", _ => body.ToJsonString() }) : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain("private-other@example.com", await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    public async Task RejectedProfileSessions_ClearLocalAccess(int status)
    {
        using var factory = Factory();
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/profile"
            ? new HttpResponseMessage((HttpStatusCode)status) : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var again = await client.GetAsync("/profile/password");
        Assert.Equal(HttpStatusCode.Redirect, again.StatusCode);
    }

    [Theory]
    [InlineData("inactive")]
    [InlineData("role-changed")]
    public async Task ChangedAccountsDuringAProfileRead_EndTheLocalSession(string change)
    {
        using var factory = Factory();
        var body = JsonNode.Parse(ProfileJson)!;
        if (change == "inactive") body["isActive"] = false;
        else body["role"] = "Organiser";
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/profile"
            ? WebApiFactory.Json(body.ToJsonString()) : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var denied = await client.GetAsync("/profile/password");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
    }

    [Theory]
    [InlineData("server")]
    [InlineData("network")]
    [InlineData("timeout")]
    public async Task UncertainPasswordChanges_EndTheSessionWithoutClaimingSuccess(string problem)
    {
        using var factory = Factory();
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/profile/password" ? problem switch
        {
            "network" => throw new HttpRequestException("private-network-error"),
            "timeout" => throw new TaskCanceledException("private-timeout-error"),
            _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        } : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        using var response = await AccountTests.PostForm(client, "/profile/password", PasswordFields());
        string page = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("You have been signed out", page);
        Assert.DoesNotContain("signed-in-user", page);
        Assert.DoesNotContain(OldPassword, page); Assert.DoesNotContain(NewPassword, page);
        Assert.DoesNotContain("private-", page);
        using var denied = await client.GetAsync("/profile");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://user:secret@example.com/picture.png")]
    public async Task UnsafePictureUrls_AreNotRendered(string picture)
    {
        using var factory = Factory();
        var body = JsonNode.Parse(ProfileJson)!; body["profilePictureUrl"] = picture;
        factory.Reply = request => request.RequestUri!.AbsolutePath == "/api/profile" ? WebApiFactory.Json(body.ToJsonString()) : Reply(factory, request);
        using var client = factory.Client();
        using var login = await SignIn(client);
        string page = await client.GetStringAsync("/profile");
        Assert.DoesNotContain("<img", page);
        Assert.Contains("profile-initial", page);
    }

    private static WebApiFactory Factory(string role = "Participant")
    {
        var factory = new WebApiFactory();
        factory.ProfileReply = _ => WebApiFactory.Profile(role);
        factory.Reply = request => Reply(factory, request, role);
        return factory;
    }

    private static HttpResponseMessage Reply(WebApiFactory factory, HttpRequestMessage request, string role = "Participant")
    {
        if (request.RequestUri!.AbsolutePath == "/api/auth/login")
        {
            var response = WebApiFactory.Json(JsonSerializer.Serialize(new
            {
                sessionExpiresAt = factory.Clock.GetUtcNow().AddMinutes(20),
                user = new { userId = 3, firstName = "Naledi", role, isActive = true }
            }));
            response.Headers.Add("Set-Cookie", "RaceDay.Session=profile-api-token; path=/");
            return response;
        }
        if (request.RequestUri.AbsolutePath == "/api/profile")
            return WebApiFactory.Json(ProfileJson.Replace("\"Participant\"", "\"" + role + "\""));
        return WebApiFactory.Json(WebApiFactory.EventJson);
    }

    private static Dictionary<string, string> EditFields() => new()
    {
        ["FirstName"] = "Naledi", ["LastName"] = "Mokoena", ["PhoneNumber"] = "0825550101", ["DateOfBirth"] = "1994-02-15"
    };

    private static Dictionary<string, string> PasswordFields() => new()
    {
        ["CurrentPassword"] = OldPassword, ["NewPassword"] = NewPassword, ["ConfirmPassword"] = NewPassword
    };

    private static Task<HttpResponseMessage> SignIn(HttpClient client) =>
        AccountTests.PostForm(client, "/account/login", AccountTests.LoginFields());
}
