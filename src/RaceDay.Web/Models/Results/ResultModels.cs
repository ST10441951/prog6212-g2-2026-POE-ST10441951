using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace RaceDay.Web.Models.Results;

// Read-only API responses and display formatting: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/standard/base-types/custom-numeric-format-strings
// Contract: src/RaceDay.Api/Contracts/Results/.
// Accessed: 18 November 2026.
public sealed class ResultSearch
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;
    public string? ResultStatus { get; set; }
}

public sealed class ResultListViewModel
{
    public ResultSearch Search { get; init; } = new();
    public ResultPage? Results { get; init; }
    public string? Error { get; init; }
}

public sealed class ResultPage
{
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
    public required int TotalPages { get; init; }
    public required IReadOnlyList<ResultDetails> Items { get; init; }
}

public sealed class ResultDetails
{
    public required int ResultId { get; init; }
    public int RecordedByUserId { get; init; }
    public string? ParticipantName { get; init; }
    public required int EnrolmentId { get; init; }
    public required int ParticipantUserId { get; init; }
    public required int EventId { get; init; }
    public required string EventName { get; init; }
    public required DateTimeOffset EventDateTime { get; init; }
    public required int EventCategoryId { get; init; }
    public required string CategoryName { get; init; }
    public required decimal CategoryDistanceKm { get; init; }
    public string? BibNumber { get; init; }
    public required string ResultStatus { get; init; }
    public int? FinishTimeSeconds { get; init; }
    public int? OverallPosition { get; init; }
    public required int TotalFinishers { get; init; }
    public int? CategoryPosition { get; init; }
    public string? Notes { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    public string StatusLabel => Label(ResultStatus);
    public static string Label(string status) => status switch
    {
        "DidNotFinish" => "Did not finish", "DidNotStart" => "Did not start", _ => status
    };

    public string FinishTime => FinishTimeSeconds is > 0 && ResultStatus == "Completed"
        ? string.Create(CultureInfo.InvariantCulture, $"{FinishTimeSeconds.Value / 3600:00}:{FinishTimeSeconds.Value / 60 % 60:00}:{FinishTimeSeconds.Value % 60:00}")
        : "No official finish time";

    public string OverallPlace => ResultStatus != "Completed" ? "Not ranked" : OverallPosition is { } position
        ? string.Create(CultureInfo.InvariantCulture, $"{position} of {TotalFinishers} finishers") : "Not assigned";
}
