namespace RaceDay.Api.Contracts.Enrolments;

// Selected response fields follow docs/API-Endpoint-Plan.md, section 10.
// Typed responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Private entry details for its Participant or event Organiser.</summary>
public sealed class EnrolmentResponse
{
    /// <summary>Unique entry ID.</summary>
    public required int EnrolmentId { get; init; }
    /// <summary>Participant who owns the entry.</summary>
    public required int ParticipantUserId { get; init; }
    /// <summary>Participant's name for the event roster.</summary>
    public required string ParticipantName { get; init; }
    /// <summary>Event entered.</summary>
    public required int EventId { get; init; }
    /// <summary>Event name.</summary>
    public required string EventName { get; init; }
    /// <summary>UTC event start.</summary>
    public required DateTime EventDateTime { get; init; }
    /// <summary>Selected category.</summary>
    public required int EventCategoryId { get; init; }
    /// <summary>Selected category name.</summary>
    public required string CategoryName { get; init; }
    /// <summary>Selected category distance in kilometres.</summary>
    public required decimal CategoryDistanceKm { get; init; }
    /// <summary>Original UTC entry time, retained on reactivation.</summary>
    public required DateTime EnrolmentDate { get; init; }
    /// <summary>Confirmed or Cancelled.</summary>
    public required string Status { get; init; }
    /// <summary>Optional assigned bib number.</summary>
    public string? BibNumber { get; init; }
    /// <summary>Latest UTC entry update.</summary>
    public DateTime? UpdatedAt { get; init; }
    /// <summary>Optional official result summary.</summary>
    public EnrolmentResultResponse? Result { get; init; }
}

/// <summary>Official result summary attached to an entry.</summary>
public sealed class EnrolmentResultResponse
{
    /// <summary>Unique result ID.</summary>
    public required int ResultId { get; init; }
    /// <summary>Completed, DidNotFinish, Disqualified or DidNotStart.</summary>
    public required string ResultStatus { get; init; }
    /// <summary>Elapsed seconds, when recorded.</summary>
    public int? FinishTimeSeconds { get; init; }
    /// <summary>Optional overall finishing position.</summary>
    public int? OverallPosition { get; init; }
    /// <summary>Optional category finishing position.</summary>
    public int? CategoryPosition { get; init; }
}

/// <summary>One page of private entries.</summary>
public sealed class EnrolmentPageResponse
{
    /// <summary>Requested page number.</summary>
    public required int Page { get; init; }
    /// <summary>Requested page size.</summary>
    public required int PageSize { get; init; }
    /// <summary>Matches after access and filters, before paging.</summary>
    public required int TotalCount { get; init; }
    /// <summary>Total matching pages, zero when empty.</summary>
    public required int TotalPages { get; init; }
    /// <summary>Matching entries, newest entry first, then descending ID.</summary>
    public required IReadOnlyList<EnrolmentResponse> Items { get; init; }
}
