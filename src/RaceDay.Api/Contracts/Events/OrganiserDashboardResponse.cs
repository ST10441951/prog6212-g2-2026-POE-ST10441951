namespace RaceDay.Api.Contracts.Events;

// Server-side summaries: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators
// Accessed: 18 October 2026.
/// <summary>Event and saved-enrolment totals for the signed-in Organiser.</summary>
public sealed class OrganiserDashboardResponse
{
    /// <summary>Total owned events, including historical and Draft records.</summary>
    public required int TotalEvents { get; init; }

    /// <summary>Saved Confirmed and Cancelled enrolments across those events.</summary>
    public required int TotalEnrolments { get; init; }

    /// <summary>Saved enrolment rows whose status is Confirmed.</summary>
    public required int ConfirmedEnrolments { get; init; }

    /// <summary>Saved enrolment rows whose status is Cancelled.</summary>
    public required int CancelledEnrolments { get; init; }

    /// <summary>Future Open and Closed event count.</summary>
    public required int UpcomingEvents { get; init; }

    /// <summary>Owned event dates and enrolment totals, ordered by date then ID.</summary>
    public required IReadOnlyList<DashboardEventResponse> Events { get; init; }
}

/// <summary>One owned event and its saved enrolment counts.</summary>
public sealed class DashboardEventResponse
{
    /// <summary>Event identifier.</summary>
    public required int EventId { get; init; }

    /// <summary>Event name.</summary>
    public required string EventName { get; init; }

    /// <summary>Current event status.</summary>
    public required string Status { get; init; }

    /// <summary>UTC event start.</summary>
    public required DateTime EventDateTime { get; init; }

    /// <summary>All saved entries for this event.</summary>
    public required int TotalEnrolments { get; init; }

    /// <summary>Saved Confirmed entries.</summary>
    public required int ConfirmedEnrolments { get; init; }

    /// <summary>Saved Cancelled entries.</summary>
    public required int CancelledEnrolments { get; init; }
}
