namespace RaceDay.Api.Models;

// Fields and relationships follow docs/RaceDay-Data-Dictionary.md and docs/RaceDay.sql.
// EF Core entity properties: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties
// Accessed: 18 October 2026.
/// <summary>Database record in EventCategories.</summary>
public sealed class EventCategory
{
    /// <summary>Unique Category identifier.</summary>
    public int EventCategoryId { get; set; }

    /// <summary>Event that owns the Category.</summary>
    public int EventId { get; set; }

    /// <summary>Public Category name.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Category distance in kilometres.</summary>
    public decimal DistanceKm { get; set; }

    /// <summary>Minimum permitted age when applicable.</summary>
    public byte? MinimumAge { get; set; }

    /// <summary>Maximum permitted age when applicable.</summary>
    public byte? MaximumAge { get; set; }

    /// <summary>Entry fee in South African rand.</summary>
    public decimal EntryFee { get; set; }

    /// <summary>Optional maximum number of entries.</summary>
    public int? Capacity { get; set; }

    /// <summary>Indicates whether new entries may select the Category.</summary>
    public bool IsAvailable { get; set; } = true;

    /// <summary>Category creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Latest Category update timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Event that owns the category.</summary>
    public Event Event { get; set; } = null!;

    /// <summary>Optional route for the category.</summary>
    public EventRoute? Route { get; set; }

    /// <summary>Entries selecting this category.</summary>
    public ICollection<Enrolment> Enrolments { get; set; } = [];
}
