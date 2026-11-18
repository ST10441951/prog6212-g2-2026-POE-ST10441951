using System.Net.Http.Json;
using System.Text.Json;
using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Models.Events;

namespace RaceDay.Web.Services;

// Typed HTTP clients and JSON responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.net.http.json.httpcontentjsonextensions.readfromjsonasync
// https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.webutilities.queryhelpers.addquerystring?view=aspnetcore-10.0
// Response fields follow docs/API-Endpoint-Plan.md, section 7.
// Accessed: 18 November 2026.
public sealed class RaceDayApiClient(HttpClient client)
{
    public async Task<EventPage> GetUpcomingEvents(EventSearch search, CancellationToken cancellationToken)
    {
        try
        {
            var query = new Dictionary<string, string?> { ["page"] = search.Page.ToString(CultureInfo.InvariantCulture), ["pageSize"] = "3" };
            if (search.EventType is not null) query["eventType"] = search.EventType;
            if (search.Province is not null) query["province"] = search.Province;
            if (search.FromDate is { } from) query["fromDate"] = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (search.ToDate is { } to) query["toDate"] = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            using var response = await client.GetAsync(QueryHelpers.AddQueryString("api/events", query), cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                throw new ApiFilterException();
            if (!response.IsSuccessStatusCode)
                throw new ApiUnavailableException("The events API did not return a successful response.");

            var page = await response.Content.ReadFromJsonAsync<EventPage>(cancellationToken);
            if (page is null || page.Items is null || page.Items.Any(item => item is null) ||
                page.Page != search.Page || page.PageSize != 3 || page.TotalCount < 0 ||
                page.TotalPages != (int)(((long)page.TotalCount + 2) / 3) || page.Items.Count > 3)
                throw new ApiUnavailableException("The events API returned an incomplete response.");
            return page;
        }
        catch (HttpRequestException error)
        {
            throw new ApiUnavailableException("The events API could not be reached.", error);
        }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiUnavailableException("The events API did not respond in time.", error);
        }
        catch (JsonException error)
        {
            throw new ApiUnavailableException("The events API returned an unreadable response.", error);
        }
    }

    public async Task<EventDetails?> GetEvent(int eventId, CancellationToken cancellationToken)
    {
        try
        {
            // Public details deliberately send no API cookie, including for a signed-in Organiser.
            using var response = await client.GetAsync($"api/events/{eventId.ToString(CultureInfo.InvariantCulture)}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (response.StatusCode != HttpStatusCode.OK)
                throw new ApiUnavailableException("The event API did not return a successful response.");
            var item = await response.Content.ReadFromJsonAsync<EventDetails>(cancellationToken);
            if (item is null || item.EventId != eventId || string.IsNullOrWhiteSpace(item.EventName) ||
                item.Status is not ("Open" or "Closed" or "Completed") || item.Categories is null ||
                item.Categories.Any(category => category is null || category.EventId != eventId))
                throw new ApiUnavailableException("The event API returned incomplete details.");
            return item;
        }
        catch (HttpRequestException error) { throw new ApiUnavailableException("The event API could not be reached.", error); }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        { throw new ApiUnavailableException("The event API did not respond in time.", error); }
        catch (JsonException error) { throw new ApiUnavailableException("The event API returned unreadable details.", error); }
    }
}

public sealed class ApiUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class ApiFilterException : Exception;
