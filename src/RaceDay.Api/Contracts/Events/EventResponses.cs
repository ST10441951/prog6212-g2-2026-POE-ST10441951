using System.Linq.Expressions;
using RaceDay.Api.Models;

namespace RaceDay.Api.Contracts.Events;

// Selected DTO fields and LINQ projections: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/client-eval
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// Response fields follow docs/API-Endpoint-Plan.md, sections 7 to 9.
// Accessed: 18 October 2026.
internal static class EventResponses
{
    internal static readonly Expression<Func<Event, EventSummaryResponse>> Summary = x => new EventSummaryResponse
    {
        EventId = x.EventId, EventName = x.EventName, EventType = x.EventType, DistanceKm = x.DistanceKm,
        EventDateTime = DateTime.SpecifyKind(x.EventDateTime, DateTimeKind.Utc),
        EntryClosingDateTime = DateTime.SpecifyKind(x.EntryClosingDateTime, DateTimeKind.Utc),
        VenueName = x.VenueName, City = x.City, Province = x.Province, Status = x.Status
    };

    internal static EventDetailsResponse Details(Event item) => new()
    {
        EventId = item.EventId, EventName = item.EventName, EventType = item.EventType,
        Description = item.Description, DistanceKm = item.DistanceKm,
        EventDateTime = DateTime.SpecifyKind(item.EventDateTime, DateTimeKind.Utc),
        EntryClosingDateTime = DateTime.SpecifyKind(item.EntryClosingDateTime, DateTimeKind.Utc),
        VenueName = item.VenueName, AddressLine1 = item.AddressLine1, City = item.City, Province = item.Province,
        Latitude = item.Latitude, Longitude = item.Longitude, Status = item.Status,
        Categories = item.Categories.OrderBy(x => x.EventCategoryId).Select(x => new CategorySummaryResponse
        {
            EventCategoryId = x.EventCategoryId, CategoryName = x.CategoryName, DistanceKm = x.DistanceKm,
            MinimumAge = x.MinimumAge, MaximumAge = x.MaximumAge, EntryFee = x.EntryFee,
            Capacity = x.Capacity, IsAvailable = x.IsAvailable,
            Route = x.Route is null ? null : new RouteSummaryResponse
            {
                RouteId = x.Route.RouteId, RouteName = x.Route.RouteName, StartLocation = x.Route.StartLocation,
                FinishLocation = x.Route.FinishLocation, RouteDescription = x.Route.RouteDescription,
                RouteMapUrl = x.Route.RouteMapUrl, ElevationGainMetres = x.Route.ElevationGainMetres
            }
        }).ToList()
    };
}
