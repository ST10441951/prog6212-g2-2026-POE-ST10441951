namespace RaceDay.Web.Models.Events;

// Read-only API response models, not database entities.
// Contract: docs/API-Endpoint-Plan.md, section 7; src/RaceDay.Api/Contracts/Events/.
// JSON deserialization: Microsoft (2026).
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization
// Accessed: 18 November 2026.
public sealed class EventPage
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public required IReadOnlyList<EventSummary> Items { get; init; }
}

public sealed class EventSummary
{
    public required int EventId { get; init; }
    public required string EventName { get; init; }
    public required string EventType { get; init; }
    public required decimal DistanceKm { get; init; }
    public required DateTimeOffset EventDateTime { get; init; }
    public required string City { get; init; }
    public required string Province { get; init; }
    public required string Status { get; init; }
}
