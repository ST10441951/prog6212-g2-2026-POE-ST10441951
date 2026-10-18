namespace RaceDay.Api.Models;

// Fields and relationships follow docs/RaceDay-Data-Dictionary.md and docs/RaceDay.sql.
// EF Core entity properties: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties
// Accessed: 18 October 2026.
/// <summary>Database record in Events.</summary>
public sealed class Event
{
    /// <summary>Unique Event identifier.</summary>
    public int EventId { get; set; }

    /// <summary>Organiser responsible for the Event.</summary>
    public int OrganiserUserId { get; set; }

    /// <summary>Public Event name.</summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>Running, Walking or Cycling.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Public Event description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Azure Blob Storage object name for the Event banner image.</summary>
    public string? BannerImageBlobName { get; set; }

    /// <summary>Advertised Event distance in kilometres.</summary>
    public decimal DistanceKm { get; set; }

    /// <summary>Scheduled start date and time.</summary>
    public DateTime EventDateTime { get; set; }

    /// <summary>Final date and time for entries.</summary>
    public DateTime EntryClosingDateTime { get; set; }

    /// <summary>Starting venue or recognised location.</summary>
    public string VenueName { get; set; } = string.Empty;

    /// <summary>Main street address.</summary>
    public string AddressLine1 { get; set; } = string.Empty;

    /// <summary>City or town.</summary>
    public string City { get; set; } = string.Empty;

    /// <summary>South African province.</summary>
    public string Province { get; set; } = string.Empty;

    /// <summary>Coordinate used for location and weather lookup.</summary>
    public decimal? Latitude { get; set; }

    /// <summary>Coordinate used for location and weather lookup.</summary>
    public decimal? Longitude { get; set; }

    /// <summary>Draft, Open, Closed, Cancelled or Completed.</summary>
    public string Status { get; set; } = "Draft";

    /// <summary>Event creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Latest Event update timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Organiser responsible for the event.</summary>
    public User Organiser { get; set; } = null!;

    /// <summary>Entry categories for this event.</summary>
    public ICollection<EventCategory> Categories { get; set; } = [];

    /// <summary>Participant entries for this event.</summary>
    public ICollection<Enrolment> Enrolments { get; set; } = [];
}
