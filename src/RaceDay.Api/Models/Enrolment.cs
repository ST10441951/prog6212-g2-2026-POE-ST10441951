namespace RaceDay.Api.Models;

// Fields and relationships follow docs/RaceDay-Data-Dictionary.md and docs/RaceDay.sql.
// EF Core entity properties: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties
// Accessed: 18 October 2026.
/// <summary>Database record in Enrolments.</summary>
public sealed class Enrolment
{
    /// <summary>Unique Enrolment identifier.</summary>
    public int EnrolmentId { get; set; }

    /// <summary>Participant who entered the Event.</summary>
    public int ParticipantUserId { get; set; }

    /// <summary>Event entered by the Participant.</summary>
    public int EventId { get; set; }

    /// <summary>Category selected by the Participant.</summary>
    public int EventCategoryId { get; set; }

    /// <summary>Date and time the entry was created.</summary>
    public DateTime EnrolmentDate { get; set; }

    /// <summary>Confirmed or Cancelled.</summary>
    public string Status { get; set; } = "Confirmed";

    /// <summary>Race number allocated to the Participant.</summary>
    public string? BibNumber { get; set; }

    /// <summary>Latest Enrolment update timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Participant who entered the event.</summary>
    public User Participant { get; set; } = null!;

    /// <summary>Event being entered.</summary>
    public Event Event { get; set; } = null!;

    /// <summary>Category within the selected event.</summary>
    public EventCategory Category { get; set; } = null!;

    /// <summary>Optional official result for this entry.</summary>
    public Result? Result { get; set; }
}
