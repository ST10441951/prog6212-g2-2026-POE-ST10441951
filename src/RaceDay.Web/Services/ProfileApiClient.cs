using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models.Profiles;

namespace RaceDay.Web.Services;

// Private JSON requests and validation responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// Contract: docs/API-Endpoint-Plan.md, section 6.
// Accessed: 18 November 2026.
public sealed class ProfileApiClient(HttpClient client)
{
    internal async Task<ProfileDetails> Get(WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "api/profile");
        using var response = await Send(request, login, cancellationToken);
        await Check(response, HttpStatusCode.OK, cancellationToken);
        return await Profile(response, login, cancellationToken);
    }

    internal async Task<ProfileDetails> Update(EditProfileForm model, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/profile")
        {
            Content = JsonContent.Create(new { model.FirstName, model.LastName, model.PhoneNumber, model.DateOfBirth })
        };
        using var response = await Send(request, login, cancellationToken);
        await Check(response, HttpStatusCode.OK, cancellationToken);
        return await Profile(response, login, cancellationToken);
    }

    internal async Task ChangePassword(ChangePasswordForm model, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "api/profile/password")
        {
            Content = JsonContent.Create(new { model.CurrentPassword, model.NewPassword, model.ConfirmPassword })
        };
        using var response = await Send(request, login, cancellationToken);
        await Check(response, HttpStatusCode.NoContent, cancellationToken);
    }

    private async Task<HttpResponseMessage> Send(HttpRequestMessage request, WebLogin login, CancellationToken cancellationToken)
    {
        request.Headers.Add("Cookie", login.ApiCookie);
        try { return await client.SendAsync(request, cancellationToken); }
        catch (HttpRequestException error) { throw new ApiUnavailableException("The profile API could not be reached.", error); }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        { throw new ApiUnavailableException("The profile API did not respond in time.", error); }
    }

    private static async Task<ProfileDetails> Profile(HttpResponseMessage response, WebLogin login, CancellationToken cancellationToken)
    {
        ProfileDetails? profile;
        try { profile = await response.Content.ReadFromJsonAsync<ProfileDetails>(cancellationToken); }
        catch (JsonException error) { throw new ApiUnavailableException("The profile response was unreadable.", error); }
        if (profile is null || profile.UserId != login.UserId || string.IsNullOrWhiteSpace(profile.FirstName) ||
            string.IsNullOrWhiteSpace(profile.LastName) || string.IsNullOrWhiteSpace(profile.Email))
            throw new ApiUnavailableException("The API did not return this user's complete profile.");
        if (!profile.IsActive || profile.Role != login.Role) throw new ApiSessionEndedException();
        return profile;
    }

    private static async Task Check(HttpResponseMessage response, HttpStatusCode expected, CancellationToken cancellationToken)
    {
        if (response.StatusCode == expected) return;
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
            throw new ApiSessionEndedException();
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ValidationReply>(cancellationToken);
                throw new AccountFormException(problem?.Errors ?? []);
            }
            catch (JsonException) { throw new AccountFormException(400, "Check the details and try again."); }
        }
        throw new ApiUnavailableException("The profile API returned an unexpected response.");
    }

    private sealed class ValidationReply { public Dictionary<string, string[]>? Errors { get; init; } }
}
