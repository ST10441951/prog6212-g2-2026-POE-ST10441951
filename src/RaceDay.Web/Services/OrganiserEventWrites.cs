using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using RaceDay.Web.Models.Events;
using RaceDay.Web.Models.Organiser;

namespace RaceDay.Web.Services;

// Explicit JSON fields, HTTP writes and date offsets: Microsoft (2026).
// https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient
// https://learn.microsoft.com/en-us/dotnet/api/system.datetimeoffset.-ctor?view=net-10.0
// https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0
// Contract: docs/API-Endpoint-Plan.md, section 7.
// Accessed: 18 November 2026.
public sealed partial class OrganiserApiClient
{
    internal async Task<EventDetails> OwnedEvent(int eventId, WebLogin login, CancellationToken cancellationToken)
    {
        // Public event reads do not identify ownership. Verify membership in the private dashboard first.
        var dashboard = await Dashboard(login, cancellationToken);
        if (!dashboard.Events.Any(item => item.EventId == eventId)) throw new EventWriteException(403, "You do not manage this event.");
        using var request = new HttpRequestMessage(HttpMethod.Get, EventRoute(eventId));
        using var response = await EventRequest(request, HttpStatusCode.OK, login, cancellationToken);
        return await EventResponse(response, eventId, cancellationToken);
    }

    internal async Task<EventDetails> CreateEvent(CreateEventForm model, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/events") { Content = JsonContent.Create(EventValues(model)) };
        using var response = await EventRequest(request, HttpStatusCode.Created, login, cancellationToken);
        var item = await EventResponse(response, null, cancellationToken);
        if (item.Status != "Draft") throw new ApiUnavailableException("The API did not confirm a new Draft event.");
        return item;
    }

    internal async Task<EventDetails> UpdateEvent(int eventId, EditEventForm model, WebLogin login, CancellationToken cancellationToken)
    {
        var values = EventValues(model); values["status"] = model.Status;
        using var request = new HttpRequestMessage(HttpMethod.Put, EventRoute(eventId)) { Content = JsonContent.Create(values) };
        using var response = await EventRequest(request, HttpStatusCode.OK, login, cancellationToken);
        return await EventResponse(response, eventId, cancellationToken);
    }

    internal async Task DeleteEvent(int eventId, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, EventRoute(eventId));
        using var response = await EventRequest(request, HttpStatusCode.NoContent, login, cancellationToken);
    }

    private static string EventRoute(int eventId) => $"api/events/{eventId.ToString(CultureInfo.InvariantCulture)}";
    private static Dictionary<string, object?> EventValues(CreateEventForm model) => new()
    {
        ["eventName"] = model.EventName,
        ["eventType"] = model.EventType,
        ["description"] = model.Description,
        ["distanceKm"] = model.DistanceKm,
        ["eventDateTime"] = Sast(model.EventDateTime!.Value, "EventDateTime"),
        ["entryClosingDateTime"] = Sast(model.EntryClosingDateTime!.Value, "EntryClosingDateTime"),
        ["venueName"] = model.VenueName,
        ["addressLine1"] = model.AddressLine1,
        ["city"] = model.City,
        ["province"] = model.Province,
        ["latitude"] = model.Latitude,
        ["longitude"] = model.Longitude
    };

    private static DateTimeOffset Sast(DateTime value, string field)
    {
        try { return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), TimeSpan.FromHours(2)); }
        catch (ArgumentException)
        { throw new EventWriteException(400, "Enter a valid South African date and time.", new() { [field] = ["Enter a valid South African date and time."] }); }
    }

    private async Task<HttpResponseMessage> EventRequest(HttpRequestMessage request, HttpStatusCode expected, WebLogin login, CancellationToken cancellationToken)
    {
        request.Headers.Add("Cookie", login.ApiCookie);
        HttpResponseMessage response;
        try { response = await client.SendAsync(request, cancellationToken); }
        catch (HttpRequestException error) { throw new ApiUnavailableException("The event request could not be confirmed.", error); }
        catch (OperationCanceledException error) when (!cancellationToken.IsCancellationRequested)
        { throw new ApiUnavailableException("The event request did not respond in time.", error); }
        if (response.StatusCode == expected) return response;
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized) throw new ApiSessionEndedException();
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Forbidden or HttpStatusCode.NotFound or HttpStatusCode.Conflict)
            {
                EventProblem? problem = null;
                try { problem = await response.Content.ReadFromJsonAsync<EventProblem>(cancellationToken); }
                catch (JsonException) { }
                string message = problem?.Title is { Length: > 0 and <= 300 } title ? title : "The event request was rejected. Check the details and try again.";
                throw new EventWriteException((int)response.StatusCode, message, problem?.Errors);
            }
            throw new ApiUnavailableException("The events API returned an unexpected response.");
        }
    }

    private static async Task<EventDetails> EventResponse(HttpResponseMessage response, int? eventId, CancellationToken cancellationToken)
    {
        var item = await Read<EventDetails>(response, cancellationToken);
        if (item is null || item.EventId <= 0 || (eventId is not null && item.EventId != eventId) ||
            string.IsNullOrWhiteSpace(item.EventName) || string.IsNullOrWhiteSpace(item.Description) ||
            !Status(item.Status) || item.Categories is null || item.Categories.Any(category => category is null || category.EventId != item.EventId || category.EventCategoryId <= 0) ||
            item.Categories.Select(category => category.EventCategoryId).Distinct().Count() != item.Categories.Count)
            throw new ApiUnavailableException("The API did not return complete event details.");
        return item;
    }

    private sealed class EventProblem
    {
        public string? Title { get; init; }
        public Dictionary<string, string[]>? Errors { get; init; }
    }
}

internal sealed class EventWriteException(int statusCode, string message, Dictionary<string, string[]>? errors = null) : Exception(message)
{
    internal int StatusCode { get; } = statusCode;
    internal Dictionary<string, string[]> Errors { get; } = errors ?? [];
}
