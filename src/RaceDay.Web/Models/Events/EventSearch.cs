using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models.Events;

// Query binding and basic input validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// Filters follow src/RaceDay.Api/Contracts/Events/EventBrowseRequest.cs.
// Accessed: 18 November 2026.
public sealed class EventSearch
{
    public string? EventType { get; set; }
    public string? Province { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    public bool HasFilters => EventType is not null || Province is not null || FromDate is not null || ToDate is not null;
}
