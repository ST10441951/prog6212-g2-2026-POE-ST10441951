namespace RaceDay.Api.Contracts.Events;

// Response fields follow docs/API-Endpoint-Plan.md, sections 7 to 9.
// Related data: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager
// Accessed: 18 October 2026.
/// <summary>One visible event, with its categories and route information.</summary>
public sealed class EventDetailsResponse : EventSummaryResponse
{
    /// <summary>Public event description.</summary>
    public required string Description { get; init; }

    /// <summary>Starting street address.</summary>
    public required string AddressLine1 { get; init; }

    /// <summary>Optional latitude for location and weather lookup.</summary>
    public decimal? Latitude { get; init; }

    /// <summary>Optional longitude for location and weather lookup.</summary>
    public decimal? Longitude { get; init; }

    /// <summary>Available categories; the owning Organiser also sees unavailable categories.</summary>
    public required IReadOnlyList<CategorySummaryResponse> Categories { get; init; }
}
