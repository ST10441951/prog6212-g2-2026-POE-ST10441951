namespace RaceDay.Api.Contracts.Events;

// Offset paging metadata: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// Accessed: 18 October 2026.
/// <summary>One page of matching upcoming events and its paging information.</summary>
public sealed class EventPageResponse
{
    /// <summary>Requested page number.</summary>
    public required int Page { get; init; }

    /// <summary>Requested page size.</summary>
    public required int PageSize { get; init; }

    /// <summary>Total matches before paging.</summary>
    public required int TotalCount { get; init; }

    /// <summary>Total pages; zero when no events match.</summary>
    public required int TotalPages { get; init; }

    /// <summary>Events ordered by start time, then event ID.</summary>
    public required IReadOnlyList<EventSummaryResponse> Items { get; init; }
}
