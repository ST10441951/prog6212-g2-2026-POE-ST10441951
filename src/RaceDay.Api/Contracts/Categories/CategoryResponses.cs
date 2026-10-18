using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Models;
using RaceDay.Api.Contracts.Routes;

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
        Route = category.Route is null ? null : RouteResponses.Details(category.Route)
    };
}
