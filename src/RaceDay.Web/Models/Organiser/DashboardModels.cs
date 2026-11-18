using System.ComponentModel.DataAnnotations;
using RaceDay.Web.Models.Events;

namespace RaceDay.Web.Models.Organiser;

// Read-only response models and query binding: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// Contract: src/RaceDay.Api/Contracts/Events/OrganiserDashboardResponse.cs and OwnedEventsRequest.cs.
// Accessed: 18 November 2026.
public sealed class OrganiserDashboard
{
    public required int TotalEvents { get; init; }
    public required int TotalEnrolments { get; init; }
    public required int ConfirmedEnrolments { get; init; }
    public required int CancelledEnrolments { get; init; }
    public required int UpcomingEvents { get; init; }
    public required IReadOnlyList<DashboardEvent> Events { get; init; }
}

public sealed class DashboardEvent
{
    public required int EventId { get; init; }
    public required string EventName { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset EventDateTime { get; init; }
    public required int TotalEnrolments { get; init; }
    public required int ConfirmedEnrolments { get; init; }
    public required int CancelledEnrolments { get; init; }
}

public sealed class OwnedEventSearch
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    public string? Status { get; set; }
}

public sealed class OwnedEventListViewModel
{
    public OwnedEventSearch Search { get; init; } = new();
    public EventPage? Events { get; init; }
    public string? Error { get; init; }
}
