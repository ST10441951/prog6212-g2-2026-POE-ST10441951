using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Events;

namespace RaceDay.Web.Models.Organiser;

// Editable form fields and read-only context: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// Contracts: src/RaceDay.Api/Contracts/Categories/ and Contracts/Routes/.
// Accessed: 18 November 2026.
public sealed class CategoryForm
{
    [Required, StringLength(80), Display(Name = "Category name")]
    public string CategoryName { get; set; } = string.Empty;
    [Required, Range(typeof(decimal), "0.01", "9999.99"), Display(Name = "Distance (km)")]
    public decimal? DistanceKm { get; set; }
    [Range(1, 120), Display(Name = "Minimum age")]
    public int? MinimumAge { get; set; }
    [Range(1, 120), Display(Name = "Maximum age")]
    public int? MaximumAge { get; set; }
    [Required, Range(typeof(decimal), "0", "99999999.99"), Display(Name = "Entry fee (R)")]
    public decimal? EntryFee { get; set; }
    [Range(1, int.MaxValue)]
    public int? Capacity { get; set; }
    [Required, Display(Name = "Availability")]
    public bool? IsAvailable { get; set; } = true;

    public static CategoryForm From(EventCategory category) => new()
    {
        CategoryName = category.CategoryName,
        DistanceKm = category.DistanceKm,
        MinimumAge = category.MinimumAge,
        MaximumAge = category.MaximumAge,
        EntryFee = category.EntryFee,
        Capacity = category.Capacity,
        IsAvailable = category.IsAvailable
    };
}

public sealed class RouteForm
{
    [Required, StringLength(120), Display(Name = "Route name")]
    public string RouteName { get; set; } = string.Empty;
    [Required, StringLength(200), Display(Name = "Start location")]
    public string StartLocation { get; set; } = string.Empty;
    [Required, StringLength(200), Display(Name = "Finish location")]
    public string FinishLocation { get; set; } = string.Empty;
    [Required, StringLength(1000), Display(Name = "Route instructions")]
    public string RouteDescription { get; set; } = string.Empty;
    [StringLength(500), Display(Name = "Map URL")]
    public string? RouteMapUrl { get; set; }
    [Range(0, int.MaxValue), Display(Name = "Elevation gain (m)")]
    public int? ElevationGainMetres { get; set; }

    public static RouteForm From(CategoryRoute route) => new()
    {
        RouteName = route.RouteName,
        StartLocation = route.StartLocation,
        FinishLocation = route.FinishLocation,
        RouteDescription = route.RouteDescription,
        RouteMapUrl = route.RouteMapUrl,
        ElevationGainMetres = route.ElevationGainMetres
    };
}

public sealed class CategoryListPage
{
    public required EventDetails Event { get; init; }
    public required IReadOnlyList<EventCategory> Categories { get; init; }
}

public sealed class CategoryPage
{
    public required EventDetails Event { get; init; }
    public required EventCategory Category { get; init; }
}
