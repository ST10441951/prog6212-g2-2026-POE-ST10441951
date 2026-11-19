using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Net.Http.Headers;
using RaceDay.Web.Models.Accounts;

namespace RaceDay.Web.Services;

// JSON requests, validation responses and Set-Cookie parsing: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.net.http.headers.setcookieheadervalue.tryparselist?view=aspnetcore-10.0
// Account contract: docs/API-Endpoint-Plan.md, section 5.
// Accessed: 18 November 2026.
public sealed class AccountApiClient(HttpClient client, TimeProvider clock)
{
    internal async Task RegisterAsync(RegisterViewModel model, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/register")
        {
            Content = JsonContent.Create(new
            {
                model.FirstName, model.LastName, model.Email, model.Password, model.Role,
                model.PhoneNumber, model.DateOfBirth
            })
        };
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Created) return;
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new AccountFormException(409, "An account already uses that email address.", "Email");
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw await ValidationError(response, cancellationToken);
        throw new ApiUnavailableException("Account registration is unavailable.");
    }

    internal async Task<WebLogin> LoginAsync(LoginViewModel model, string? previousCookie, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/login")
        {
            Content = JsonContent.Create(new { model.Email, model.Password })
        };
        if (previousCookie is not null) request.Headers.Add("Cookie", previousCookie);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new AccountFormException(401, "The email address or password is incorrect.");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new AccountFormException(403, "This account is inactive.");
        if (response.StatusCode == HttpStatusCode.BadRequest)
            throw await ValidationError(response, cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK)
            throw new ApiUnavailableException("Account login is unavailable.");
        LoginReply? login;
        try { login = await response.Content.ReadFromJsonAsync<LoginReply>(cancellationToken); }
        catch (JsonException error) { throw new ApiUnavailableException("The login response was unreadable.", error); }
        string? cookie = ApiCookie(response, clock.GetUtcNow());
        if (login?.User is not { IsActive: true, UserId: > 0 } account || cookie is null ||
            account.Role is not ("Organiser" or "Participant") || string.IsNullOrWhiteSpace(account.FirstName) ||
            login.SessionExpiresAt <= clock.GetUtcNow() || login.SessionExpiresAt > clock.GetUtcNow().AddMinutes(30))
            throw new ApiUnavailableException("The API did not return a complete login session.");
        return new WebLogin(account.UserId, account.FirstName, account.Role, login.SessionExpiresAt, cookie);
    }

    internal async Task LogoutAsync(string cookie, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/logout");
        request.Headers.Add("Cookie", cookie);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode is not (HttpStatusCode.NoContent or HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden))
            throw new ApiUnavailableException("The API could not confirm logout.");
    }

    // Recheck the current API session; identity never comes from a submitted user ID.
    // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0
    internal async Task<SessionAccount> GetSessionAccountAsync(string cookie, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/profile");
        request.Headers.Add("Cookie", cookie);
        using var response = await SendAsync(request, cancellationToken);
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            throw new ApiSessionEndedException();
        if (response.StatusCode != HttpStatusCode.OK)
            throw new ApiUnavailableException("The API session could not be checked.");
        try
        {
            var account = await response.Content.ReadFromJsonAsync<SessionAccount>(cancellationToken);
            if (account is null || string.IsNullOrWhiteSpace(account.FirstName))
                throw new ApiUnavailableException("The API returned an incomplete account.");
            return account;
        }
        catch (JsonException error) { throw new ApiUnavailableException("The account response was unreadable.", error); }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try { return await client.SendAsync(request, cancellationToken); }
        catch (HttpRequestException error) { throw new ApiUnavailableException("The account API could not be reached.", error); }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        { throw new ApiUnavailableException("The account API did not respond in time.", error); }
    }

    private static string? ApiCookie(HttpResponseMessage response, DateTimeOffset now)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var headers) ||
            !SetCookieHeaderValue.TryParseList(headers.ToList(), out var cookies)) return null;
        // A replacement login expires the old cookie before setting its new value.
        // Ignore only explicit empty deletion cookies; multiple live values remain invalid.
        // Microsoft (2026): https://learn.microsoft.com/en-us/dotnet/api/microsoft.net.http.headers.setcookieheadervalue.expires
        // https://learn.microsoft.com/en-us/dotnet/api/microsoft.net.http.headers.setcookieheadervalue.maxage
        // Accessed: 19 November 2026.
        var matching = cookies!.Where(cookie => cookie.Name == "RaceDay.Session" && !DeletedCookie(cookie, now)).ToArray();
        if (matching.Length != 1) return null;
        string value = HeaderUtilities.RemoveQuotes(matching[0].Value).ToString();
        if (string.IsNullOrWhiteSpace(value) || value.Length > 4096 ||
            value.IndexOfAny(['\r', '\n', ';', ',']) >= 0) return null;
        return "RaceDay.Session=" + value;
    }

    private static bool DeletedCookie(SetCookieHeaderValue cookie, DateTimeOffset now) =>
        HeaderUtilities.RemoveQuotes(cookie.Value).Length == 0 &&
        (cookie.MaxAge is { } age ? age <= TimeSpan.Zero : cookie.Expires is { } expiry && expiry <= now);

    private static async Task<AccountFormException> ValidationError(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ValidationReply>(cancellationToken);
            return new AccountFormException(problem?.Errors ?? []);
        }
        catch (JsonException) { return new AccountFormException(400, "Check the account details and try again."); }
    }

    private sealed class LoginReply
    {
        public required DateTimeOffset SessionExpiresAt { get; init; }
        public required AccountReply User { get; init; }
    }

    private sealed class AccountReply
    {
        public required int UserId { get; init; }
        public required string FirstName { get; init; }
        public required string Role { get; init; }
        public required bool IsActive { get; init; }
    }

    private sealed class ValidationReply
    {
        public Dictionary<string, string[]>? Errors { get; init; }
    }
}

internal sealed class SessionAccount
{
    public required int UserId { get; init; }
    public required string FirstName { get; init; }
    public required string Role { get; init; }
    public required bool IsActive { get; init; }
}

internal sealed class ApiSessionEndedException : Exception;

internal sealed class AccountFormException : Exception
{
    internal int StatusCode { get; }
    internal Dictionary<string, string[]> Errors { get; }
    internal AccountFormException(int statusCode, string message, string field = "")
    {
        StatusCode = statusCode;
        Errors = new() { [field] = [message] };
    }
    internal AccountFormException(Dictionary<string, string[]> errors)
    {
        StatusCode = 400;
        Errors = errors.Count > 0 ? errors : new() { [""] = ["Check the account details and try again."] };
    }
}
