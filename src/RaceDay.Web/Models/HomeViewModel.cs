using RaceDay.Web.Models.Events;

namespace RaceDay.Web.Models;

// View models: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class HomeViewModel
{
    public IReadOnlyList<EventSummary> Events { get; init; } = [];
    public int TotalCount { get; init; }
    public string? EventType { get; init; }
    public string? Province { get; init; }
    public string? ErrorMessage { get; init; }
}

public sealed class ErrorViewModel
{
    public required int StatusCode { get; init; }
    public required string Heading { get; init; }
    public required string Message { get; init; }
    public string? RequestId { get; init; }
}
