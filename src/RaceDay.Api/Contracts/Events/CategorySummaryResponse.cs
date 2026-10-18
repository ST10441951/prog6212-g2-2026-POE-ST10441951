namespace RaceDay.Api.Contracts.Events;

// Category response fields follow docs/API-Endpoint-Plan.md, sections 8 and 9.
// Related data: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager
// Accessed: 18 October 2026.
/// <summary>Category information without participant or enrolment records.</summary>
public sealed class CategorySummaryResponse
{
    /// <summary>Unique category identifier.</summary>
    public required int EventCategoryId { get; init; }

    /// <summary>Category name.</summary>
    public required string CategoryName { get; init; }

    /// <summary>Category distance in kilometres.</summary>
    public required decimal DistanceKm { get; init; }

    /// <summary>Inclusive minimum age, if set.</summary>
    public byte? MinimumAge { get; init; }

    /// <summary>Inclusive maximum age, if set.</summary>
    public byte? MaximumAge { get; init; }

    /// <summary>Entry fee in South African rand.</summary>
    public required decimal EntryFee { get; init; }

    /// <summary>Maximum entries, if set. This is not a count of remaining places.</summary>
    public int? Capacity { get; init; }

    /// <summary>Whether the category is available for selection.</summary>
    public required bool IsAvailable { get; init; }

    /// <summary>Route information, or null when no route has been added.</summary>
    public RouteSummaryResponse? Route { get; init; }
}
