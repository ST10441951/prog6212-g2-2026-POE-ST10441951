namespace RaceDay.Api.Contracts.Events;

// Selected response fields follow docs/API-Endpoint-Plan.md, section 7.
// Typed API responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Public event information used in event lists and details.</summary>
public class EventSummaryResponse
{
    /// <summary>Unique event identifier.</summary>
    public required int EventId { get; init; }

    /// <summary>Public event name.</summary>
    public required string EventName { get; init; }

    /// <summary>Running, Walking or Cycling.</summary>
    public required string EventType { get; init; }

    /// <summary>Null until the banner-storage endpoints are implemented.</summary>
    public string? BannerImageUrl { get; init; }

    /// <summary>Advertised distance in kilometres.</summary>
    public required decimal DistanceKm { get; init; }

    /// <summary>Scheduled UTC event start.</summary>
    public required DateTime EventDateTime { get; init; }

    /// <summary>UTC closing time for entries.</summary>
    public required DateTime EntryClosingDateTime { get; init; }

    /// <summary>Starting venue.</summary>
    public required string VenueName { get; init; }

    /// <summary>City or town.</summary>
    public required string City { get; init; }

    /// <summary>South African province.</summary>
    public required string Province { get; init; }

    /// <summary>Current event status.</summary>
    public required string Status { get; init; }
}
