using System.Net.Http.Json;
using System.Text.Json;
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
    public async Task<EventPage> GetUpcomingEvents(string? eventType, string? province, CancellationToken cancellationToken)
    {
        try
        {
            var query = new Dictionary<string, string?> { ["page"] = "1", ["pageSize"] = "3" };
            if (eventType is not null) query["eventType"] = eventType;
            if (province is not null) query["province"] = province;
            using var response = await client.GetAsync(QueryHelpers.AddQueryString("api/events", query), cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                throw new ApiFilterException();
            if (!response.IsSuccessStatusCode)
                throw new ApiUnavailableException("The events API did not return a successful response.");

            var page = await response.Content.ReadFromJsonAsync<EventPage>(cancellationToken);
            if (page is null || page.Items is null || page.Items.Any(item => item is null))
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
}

public sealed class ApiUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);

public sealed class ApiFilterException : Exception;
