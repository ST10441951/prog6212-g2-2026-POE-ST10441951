using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Models;

namespace RaceDay.Api.Contracts.Routes;

// Selected response fields: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// Response fields follow docs/API-Endpoint-Plan.md, section 9.
// Accessed: 18 October 2026.
internal static class RouteResponses
{
    internal static RouteSummaryResponse Details(EventRoute route) => new()
    {
        RouteId = route.RouteId, RouteName = route.RouteName,
        StartLocation = route.StartLocation, FinishLocation = route.FinishLocation,
        RouteDescription = route.RouteDescription, RouteMapUrl = route.RouteMapUrl,
        ElevationGainMetres = route.ElevationGainMetres
    };
}
