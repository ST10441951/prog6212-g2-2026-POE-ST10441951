namespace RaceDay.Web.Models.Events;

// Read-only response models follow src/RaceDay.Api/Contracts/Events/.
// JSON deserialization: Microsoft (2026).
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization
// Accessed: 18 November 2026.
public sealed class EventDetails : EventSummary
{
    public required string Description { get; init; }
    public required DateTimeOffset EntryClosingDateTime { get; init; }
    public required string VenueName { get; init; }
    public required string AddressLine1 { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public required IReadOnlyList<EventCategory> Categories { get; init; }
}

public sealed class EventCategory
{
    public required int EventCategoryId { get; init; }
    public required int EventId { get; init; }
    public required string CategoryName { get; init; }
    public required decimal DistanceKm { get; init; }
    public byte? MinimumAge { get; init; }
    public byte? MaximumAge { get; init; }
    public required decimal EntryFee { get; init; }
    public int? Capacity { get; init; }
    public required bool IsAvailable { get; init; }
    public CategoryRoute? Route { get; init; }
}

public sealed class CategoryRoute
{
    public required int RouteId { get; init; }
    public required string RouteName { get; init; }
    public required string StartLocation { get; init; }
    public required string FinishLocation { get; init; }
    public required string RouteDescription { get; init; }
    public string? RouteMapUrl { get; init; }
    public int? ElevationGainMetres { get; init; }
}
