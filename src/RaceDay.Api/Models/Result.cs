namespace RaceDay.Api.Models;

// Fields and relationships follow docs/RaceDay-Data-Dictionary.md and docs/RaceDay.sql.
// EF Core entity properties: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties
// Accessed: 18 October 2026.
/// <summary>Database record in Results.</summary>
public sealed class Result
{
    /// <summary>Unique Result identifier.</summary>
    public int ResultId { get; set; }

    /// <summary>Enrolment receiving the Result.</summary>
    public int EnrolmentId { get; set; }

    /// <summary>Organiser who recorded the Result.</summary>
    public int RecordedByUserId { get; set; }

    /// <summary>Completed, DidNotFinish, Disqualified or DidNotStart.</summary>
    public string ResultStatus { get; set; } = "Completed";

    /// <summary>Official elapsed time in seconds.</summary>
    public int? FinishTimeSeconds { get; set; }

    /// <summary>Overall finishing position.</summary>
    public int? OverallPosition { get; set; }

    /// <summary>Finishing position within the Category.</summary>
    public int? CategoryPosition { get; set; }

    /// <summary>Optional Result explanation.</summary>
    public string? Notes { get; set; }

    /// <summary>Date and time the Result was first recorded.</summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>Latest Result correction timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Entry receiving this result.</summary>
    public Enrolment Enrolment { get; set; } = null!;

    /// <summary>Organiser who recorded the result.</summary>
    public User RecordedBy { get; set; } = null!;
}
