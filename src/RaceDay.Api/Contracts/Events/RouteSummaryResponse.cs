namespace RaceDay.Api.Contracts.Events;

// Route response fields follow docs/API-Endpoint-Plan.md, section 9.
// Typed API responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Public route information for an event category.</summary>
public sealed class RouteSummaryResponse
{
    /// <summary>Unique route identifier.</summary>
    public required int RouteId { get; init; }

    /// <summary>Route name.</summary>
    public required string RouteName { get; init; }

    /// <summary>Description of the start.</summary>
    public required string StartLocation { get; init; }

    /// <summary>Description of the finish.</summary>
    public required string FinishLocation { get; init; }

    /// <summary>Route instructions and important information.</summary>
    public required string RouteDescription { get; init; }

    /// <summary>Optional link to the route map.</summary>
    public string? RouteMapUrl { get; init; }

    /// <summary>Optional elevation gain in metres.</summary>
    public int? ElevationGainMetres { get; init; }
}
