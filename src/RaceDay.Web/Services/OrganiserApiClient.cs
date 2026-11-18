using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using RaceDay.Web.Models.Events;
using RaceDay.Web.Models.Organiser;

namespace RaceDay.Web.Services;

// Private typed requests and JSON responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// Contract: docs/API-Endpoint-Plan.md, section 7.
// Accessed: 18 November 2026.
public sealed partial class OrganiserApiClient(HttpClient client)
{
    internal async Task<OrganiserDashboard> Dashboard(WebLogin login, CancellationToken cancellationToken)
    {
        using var response = await Send("api/organiser/dashboard", login, cancellationToken);
        var dashboard = await Read<OrganiserDashboard>(response, cancellationToken);
        if (dashboard is null || dashboard.Events is null || dashboard.TotalEvents != dashboard.Events.Count ||
            dashboard.UpcomingEvents < 0 || dashboard.UpcomingEvents > dashboard.TotalEvents ||
            !Counts(dashboard.TotalEnrolments, dashboard.ConfirmedEnrolments, dashboard.CancelledEnrolments) ||
            dashboard.Events.Any(item => item is null || item.EventId <= 0 || string.IsNullOrWhiteSpace(item.EventName) ||
                !Status(item.Status) || !Counts(item.TotalEnrolments, item.ConfirmedEnrolments, item.CancelledEnrolments)) ||
            dashboard.Events.Select(item => item.EventId).Distinct().Count() != dashboard.Events.Count ||
            dashboard.Events.Sum(item => (long)item.TotalEnrolments) != dashboard.TotalEnrolments ||
            dashboard.Events.Sum(item => (long)item.ConfirmedEnrolments) != dashboard.ConfirmedEnrolments ||
            dashboard.Events.Sum(item => (long)item.CancelledEnrolments) != dashboard.CancelledEnrolments)
            throw new ApiUnavailableException("The API returned an inconsistent Organiser dashboard.");
        return dashboard;
    }

    internal async Task<EventPage> Events(OwnedEventSearch search, WebLogin login, CancellationToken cancellationToken)
    {
        var query = new Dictionary<string, string?>
        {
            ["page"] = search.Page.ToString(CultureInfo.InvariantCulture),
            ["pageSize"] = "10"
        };
        if (search.Status is not null) query["status"] = search.Status;
        using var response = await Send(QueryHelpers.AddQueryString("api/events/mine", query), login, cancellationToken);
        var page = await Read<EventPage>(response, cancellationToken);
        if (page is null || page.Page != search.Page || page.PageSize != 10 || page.TotalCount < 0 ||
            page.TotalPages != (int)(((long)page.TotalCount + 9) / 10) || page.Items is null ||
            page.Items.Count != Math.Min(10L, Math.Max(0L, page.TotalCount - ((long)page.Page - 1) * 10)) ||
            page.Items.Any(item => item is null || item.EventId <= 0 || string.IsNullOrWhiteSpace(item.EventName) || !Status(item.Status)) ||
            page.Items.Select(item => item.EventId).Distinct().Count() != page.Items.Count)
            throw new ApiUnavailableException("The API returned inconsistent owned events.");
        return page;
    }

    private static bool Counts(int total, int confirmed, int cancelled) =>
        total >= 0 && confirmed >= 0 && cancelled >= 0 && (long)confirmed + cancelled == total;
    private static bool Status(string status) => status is "Draft" or "Open" or "Closed" or "Cancelled" or "Completed";

    private async Task<HttpResponseMessage> Send(string route, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, route);
        request.Headers.Add("Cookie", login.ApiCookie);
        HttpResponseMessage response;
        try { response = await client.SendAsync(request, cancellationToken); }
        catch (HttpRequestException error) { throw new ApiUnavailableException("The Organiser API could not be reached.", error); }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        { throw new ApiUnavailableException("The Organiser API did not respond in time.", error); }
        if (response.StatusCode == HttpStatusCode.OK) return response;
        using (response)
        {
            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden) throw new ApiSessionEndedException();
            if (response.StatusCode == HttpStatusCode.BadRequest) throw new OrganiserFilterException();
            throw new ApiUnavailableException("The Organiser API returned an unexpected response.");
        }
    }

    private static async Task<T?> Read<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try { return await response.Content.ReadFromJsonAsync<T>(cancellationToken); }
        catch (JsonException error) { throw new ApiUnavailableException("The Organiser response was unreadable.", error); }
    }
}

internal sealed class OrganiserFilterException : Exception { }
