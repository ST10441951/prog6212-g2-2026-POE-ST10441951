namespace RaceDay.Api.Models;

// Fields and relationships follow docs/RaceDay-Data-Dictionary.md and docs/RaceDay.sql.
// EF Core entity properties: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties
// Accessed: 18 October 2026.
/// <summary>Database record in EventRoutes.</summary>
public sealed class EventRoute
{
    /// <summary>Unique Route identifier.</summary>
    public int RouteId { get; set; }

    /// <summary>Category that uses the Route.</summary>
    public int EventCategoryId { get; set; }

    /// <summary>Public Route name.</summary>
    public string RouteName { get; set; } = string.Empty;

    /// <summary>Description of the starting point.</summary>
    public string StartLocation { get; set; } = string.Empty;

    /// <summary>Description of the finishing point.</summary>
    public string FinishLocation { get; set; } = string.Empty;

    /// <summary>Instructions and important Route information.</summary>
    public string RouteDescription { get; set; } = string.Empty;

    /// <summary>Optional link to a Route map.</summary>
    public string? RouteMapUrl { get; set; }

    /// <summary>Optional total elevation gain.</summary>
    public int? ElevationGainMetres { get; set; }

    /// <summary>Route creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Latest Route update timestamp.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Category that uses this route.</summary>
    public EventCategory Category { get; set; } = null!;
}
