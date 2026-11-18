using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Models.Entries;

namespace RaceDay.Web.Services;

// Per-request cookies, JSON and API problem responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// Contract: docs/API-Endpoint-Plan.md, section 10.
// Accessed: 18 November 2026.
public sealed class EntryApiClient(HttpClient client)
{
    internal async Task<EntryDetails> Create(int eventId, int categoryId, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/enrolments")
        {
            Content = JsonContent.Create(new { eventId, eventCategoryId = categoryId })
        };
        using var response = await Send(request, login, cancellationToken);
        await Check(response, HttpStatusCode.Created, cancellationToken);
        var entry = await Read<EntryDetails>(response, cancellationToken);
        if (!Owned(entry, login) || entry!.EventId != eventId || entry.EventCategoryId != categoryId || entry.Status != "Confirmed")
            throw new ApiUnavailableException("The API did not return the confirmed entry.");
        return entry;
    }

    internal async Task<EntryDetails> Details(int enrolmentId, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/enrolments/{enrolmentId.ToString(CultureInfo.InvariantCulture)}");
        using var response = await Send(request, login, cancellationToken);
        await Check(response, HttpStatusCode.OK, cancellationToken);
        var entry = await Read<EntryDetails>(response, cancellationToken);
        if (!Owned(entry, login) || entry!.EnrolmentId != enrolmentId)
            throw new ApiUnavailableException("The API returned inconsistent entry details.");
        return entry;
    }

    internal async Task<EntryPage> Mine(EntrySearch search, WebLogin login, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = search.Page.ToString(CultureInfo.InvariantCulture), ["pageSize"] = "10"
        };
        if (search.Status is not null) query["status"] = search.Status;
        using var request = new HttpRequestMessage(HttpMethod.Get, QueryHelpers.AddQueryString("api/enrolments/mine", query));
        using var response = await Send(request, login, cancellationToken);
        await Check(response, HttpStatusCode.OK, cancellationToken);
        var page = await Read<EntryPage>(response, cancellationToken);
        if (page is null || page.Page != search.Page || page.PageSize != 10 || page.TotalCount < 0 ||
            page.TotalPages != (int)(((long)page.TotalCount + 9) / 10) || page.Items is null ||
            page.Items.Count > 10 || page.Items.Any(entry => !Owned(entry, login)))
            throw new ApiUnavailableException("The API returned inconsistent entry history.");
        return page;
    }

    private static bool Owned(EntryDetails? entry, WebLogin login) => entry is { EnrolmentId: > 0, EventId: > 0, EventCategoryId: > 0 } &&
        entry.ParticipantUserId == login.UserId && entry.Status is "Confirmed" or "Cancelled" &&
        !string.IsNullOrWhiteSpace(entry.EventName) && !string.IsNullOrWhiteSpace(entry.CategoryName);

    private async Task<HttpResponseMessage> Send(HttpRequestMessage request, WebLogin login, CancellationToken cancellationToken)
    {
        request.Headers.Add("Cookie", login.ApiCookie);
        try { return await client.SendAsync(request, cancellationToken); }
        catch (HttpRequestException error) { throw new ApiUnavailableException("The entry service could not be reached.", error); }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        { throw new ApiUnavailableException("The entry service did not respond in time.", error); }
    }

    private static async Task<T?> Read<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try { return await response.Content.ReadFromJsonAsync<T>(cancellationToken); }
        catch (JsonException error) { throw new ApiUnavailableException("The entry response was unreadable.", error); }
    }

    private static async Task Check(HttpResponseMessage response, HttpStatusCode expected, CancellationToken cancellationToken)
    {
        if (response.StatusCode == expected) return;
        if (response.StatusCode == HttpStatusCode.Unauthorized) throw new ApiSessionEndedException();
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new EntryRequestException(403, "Your account cannot access this entry.");
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound or HttpStatusCode.Conflict)
        {
            string message = "The entry request could not be completed. Check the event and category, then try again.";
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<EntryProblem>(cancellationToken);
                if (problem?.Title is { Length: > 0 and <= 300 } title) message = title;
            }
            catch (JsonException) { }
            throw new EntryRequestException((int)response.StatusCode, message);
        }
        throw new ApiUnavailableException("The entry service returned an unexpected response.");
    }

    private sealed class EntryProblem { public string? Title { get; init; } }
}

internal sealed class EntryRequestException(int statusCode, string message) : Exception(message)
{
    internal int StatusCode { get; } = statusCode;
}
