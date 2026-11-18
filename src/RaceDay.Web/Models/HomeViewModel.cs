using RaceDay.Web.Models.Events;
using System.Globalization;

namespace RaceDay.Web.Models;

// View models: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class HomeViewModel
{
    public IReadOnlyList<EventSummary> Events { get; init; } = [];
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
    public EventSearch Search { get; init; } = new();
    public string? ErrorMessage { get; init; }

    // Anchor route values retain the current filters when moving between pages.
    // https://learn.microsoft.com/en-us/aspnet/core/mvc/views/tag-helpers/built-in/anchor-tag-helper?view=aspnetcore-10.0
    public Dictionary<string, string> PageRoutes(int page) => new()
    {
        ["page"] = page.ToString(CultureInfo.InvariantCulture),
        ["eventType"] = Search.EventType ?? "", ["province"] = Search.Province ?? "",
        ["fromDate"] = Search.FromDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "",
        ["toDate"] = Search.ToDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""
    };
}

public sealed class ErrorViewModel
{
    public required int StatusCode { get; init; }
    public required string Heading { get; init; }
    public required string Message { get; init; }
    public string? RequestId { get; init; }
}
