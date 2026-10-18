using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Models;

namespace RaceDay.Api.Contracts.Categories;

// Selected response fields and related data: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager
// Response fields follow docs/API-Endpoint-Plan.md, section 8.
// Accessed: 18 October 2026.
internal static class CategoryResponses
{
    internal static CategorySummaryResponse Details(EventCategory category) => new()
    {
        EventCategoryId = category.EventCategoryId, EventId = category.EventId,
        CategoryName = category.CategoryName, DistanceKm = category.DistanceKm,
        MinimumAge = category.MinimumAge, MaximumAge = category.MaximumAge,
        EntryFee = category.EntryFee, Capacity = category.Capacity, IsAvailable = category.IsAvailable,
        Route = category.Route is null ? null : new RouteSummaryResponse
        {
            RouteId = category.Route.RouteId, RouteName = category.Route.RouteName,
            StartLocation = category.Route.StartLocation, FinishLocation = category.Route.FinishLocation,
            RouteDescription = category.Route.RouteDescription, RouteMapUrl = category.Route.RouteMapUrl,
            ElevationGainMetres = category.Route.ElevationGainMetres
        }
    };
}
