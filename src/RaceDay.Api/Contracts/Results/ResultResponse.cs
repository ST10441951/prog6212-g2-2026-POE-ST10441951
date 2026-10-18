namespace RaceDay.Api.Contracts.Results;

// Response fields follow docs/API-Endpoint-Plan.md, section 11.
// Typed responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Official result details for its Participant or event Organiser.</summary>
public sealed class ResultResponse
{
    /// <summary>Unique result ID.</summary>
    public required int ResultId { get; init; }
    /// <summary>Entry receiving the result.</summary>
    public required int EnrolmentId { get; init; }
    /// <summary>Organiser who originally recorded the result.</summary>
    public required int RecordedByUserId { get; init; }
    /// <summary>Participant who owns the entry.</summary>
    public required int ParticipantUserId { get; init; }
    /// <summary>Participant name for the event's result list.</summary>
    public required string ParticipantName { get; init; }
    /// <summary>Event ID.</summary>
    public required int EventId { get; init; }
    /// <summary>Event name.</summary>
    public required string EventName { get; init; }
    /// <summary>UTC event start.</summary>
    public required DateTime EventDateTime { get; init; }
    /// <summary>Selected category ID.</summary>
    public required int EventCategoryId { get; init; }
    /// <summary>Selected category name.</summary>
    public required string CategoryName { get; init; }
    /// <summary>Selected category distance in kilometres.</summary>
    public required decimal CategoryDistanceKm { get; init; }
    /// <summary>Optional assigned bib number.</summary>
    public string? BibNumber { get; init; }
    /// <summary>Completed, DidNotFinish, Disqualified or DidNotStart.</summary>
    public required string ResultStatus { get; init; }
    /// <summary>Official elapsed seconds for Completed results.</summary>
    public int? FinishTimeSeconds { get; init; }
    /// <summary>Optional overall finishing position.</summary>
    public int? OverallPosition { get; init; }
    /// <summary>Completed results across the whole event, independent of list filters and paging.</summary>
    public required int TotalFinishers { get; init; }
    /// <summary>Optional category finishing position.</summary>
    public int? CategoryPosition { get; init; }
    /// <summary>Optional result explanation.</summary>
    public string? Notes { get; init; }
    /// <summary>Original UTC recording time.</summary>
    public required DateTime RecordedAt { get; init; }
    /// <summary>Latest UTC correction time.</summary>
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>One page of matching private results.</summary>
public sealed class ResultPageResponse
{
    /// <summary>Requested page.</summary>
    public required int Page { get; init; }
    /// <summary>Requested page size.</summary>
    public required int PageSize { get; init; }
    /// <summary>Matches after access and filters, before paging.</summary>
    public required int TotalCount { get; init; }
    /// <summary>Total matching pages, zero when empty.</summary>
    public required int TotalPages { get; init; }
    /// <summary>Results on this page.</summary>
    public required IReadOnlyList<ResultResponse> Items { get; init; }
}
