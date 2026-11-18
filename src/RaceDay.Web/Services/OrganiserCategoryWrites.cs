using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using RaceDay.Web.Models.Events;
using RaceDay.Web.Models.Organiser;

namespace RaceDay.Web.Services;

// Private JSON reads/writes with explicit fields: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// Contract: docs/API-Endpoint-Plan.md, sections 8 and 9.
// Accessed: 18 November 2026.
public sealed partial class OrganiserApiClient
{
    internal async Task<IReadOnlyList<EventCategory>> OwnedCategories(int eventId, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/organiser/events/{eventId.ToString(CultureInfo.InvariantCulture)}/categories");
        using var response = await EventRequest(request, HttpStatusCode.OK, login, cancellationToken);
        var items = await Read<IReadOnlyList<EventCategory>>(response, cancellationToken);
        if (items is null || items.Any(item => !CategoryMatches(item, eventId)) || items.Select(item => item.EventCategoryId).Distinct().Count() != items.Count)
            throw new ApiUnavailableException("The API returned inconsistent owned categories.");
        return items;
    }

    internal async Task<EventCategory> SaveCategory(int eventId, int? categoryId, CategoryForm model, WebLogin login, CancellationToken cancellationToken)
    {
        string route = CategoryPath(eventId) + (categoryId is { } id ? "/" + id.ToString(CultureInfo.InvariantCulture) : "");
        using var request = new HttpRequestMessage(categoryId is null ? HttpMethod.Post : HttpMethod.Put, route)
        {
            Content = JsonContent.Create(new { model.CategoryName, model.DistanceKm, model.MinimumAge, model.MaximumAge, model.EntryFee, model.Capacity, model.IsAvailable })
        };
        using var response = await EventRequest(request, categoryId is null ? HttpStatusCode.Created : HttpStatusCode.OK, login, cancellationToken);
        var saved = await Read<EventCategory>(response, cancellationToken);
        if (!CategoryMatches(saved, eventId) || (categoryId is not null && saved!.EventCategoryId != categoryId))
            throw new ApiUnavailableException("The API did not confirm this category.");
        return saved!;
    }

    internal async Task DeleteCategory(int eventId, int categoryId, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, CategoryPath(eventId, categoryId));
        using var response = await EventRequest(request, HttpStatusCode.NoContent, login, cancellationToken);
    }

    internal async Task<CategoryRoute> SaveRoute(int eventId, int categoryId, int? existingRouteId, RouteForm model, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(existingRouteId is null ? HttpMethod.Post : HttpMethod.Put, CategoryPath(eventId, categoryId) + "/route")
        {
            Content = JsonContent.Create(new { model.RouteName, model.StartLocation, model.FinishLocation, model.RouteDescription, model.RouteMapUrl, model.ElevationGainMetres })
        };
        using var response = await EventRequest(request, existingRouteId is null ? HttpStatusCode.Created : HttpStatusCode.OK, login, cancellationToken);
        var saved = await Read<CategoryRoute>(response, cancellationToken);
        if (saved is null || saved.RouteId <= 0 || string.IsNullOrWhiteSpace(saved.RouteName) ||
            string.IsNullOrWhiteSpace(saved.StartLocation) || string.IsNullOrWhiteSpace(saved.FinishLocation) ||
            string.IsNullOrWhiteSpace(saved.RouteDescription) || (existingRouteId is not null && saved.RouteId != existingRouteId))
            throw new ApiUnavailableException("The API did not confirm this route.");
        return saved;
    }

    internal async Task DeleteRoute(int eventId, int categoryId, WebLogin login, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, CategoryPath(eventId, categoryId) + "/route");
        using var response = await EventRequest(request, HttpStatusCode.NoContent, login, cancellationToken);
    }

    private static string CategoryPath(int eventId, int? categoryId = null) =>
        $"api/events/{eventId.ToString(CultureInfo.InvariantCulture)}/categories" + (categoryId is { } id ? "/" + id.ToString(CultureInfo.InvariantCulture) : "");
    private static bool CategoryMatches(EventCategory? item, int eventId) => item is not null && item.EventId == eventId &&
        item.EventCategoryId > 0 && !string.IsNullOrWhiteSpace(item.CategoryName);
}
