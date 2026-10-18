using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Contracts.Results;

// Request validation, explicit JSON fields and paging: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// Accessed: 18 October 2026.
/// <summary>Editable official result fields; identities and timestamps are assigned by the API.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ResultWriteRequest : IValidatableObject
{
    private string resultStatus = string.Empty;
    private string? notes;

    /// <summary>Completed, DidNotFinish, Disqualified or DidNotStart; ignores case and surrounding spaces.</summary>
    [Required, RegularExpression("Completed|DidNotFinish|Disqualified|DidNotStart")]
    public string ResultStatus { get => resultStatus; init => resultStatus = ResultStatuses.Normalize(value) ?? string.Empty; }

    /// <summary>Positive elapsed seconds, required for Completed and null for other statuses.</summary>
    [Range(1, int.MaxValue)]
    public int? FinishTimeSeconds { get; init; }

    /// <summary>Optional positive overall finishing position, for Completed results only.</summary>
    [Range(1, int.MaxValue)]
    public int? OverallPosition { get; init; }

    /// <summary>Optional positive category finishing position, for Completed results only.</summary>
    [Range(1, int.MaxValue)]
    public int? CategoryPosition { get; init; }

    /// <summary>Optional trimmed explanation, up to 500 characters; blank becomes null.</summary>
    [StringLength(500)]
    public string? Notes { get => notes; init => notes = string.IsNullOrWhiteSpace(value) ? null : value.Trim(); }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ResultStatus == "Completed" && !FinishTimeSeconds.HasValue)
            yield return new ValidationResult("A Completed result requires a positive finish time.", [nameof(FinishTimeSeconds)]);
        if (ResultStatus is "DidNotFinish" or "Disqualified" or "DidNotStart")
        {
            if (FinishTimeSeconds.HasValue)
                yield return new ValidationResult("Only Completed results have an official finish time.", [nameof(FinishTimeSeconds)]);
            if (OverallPosition.HasValue || CategoryPosition.HasValue)
                yield return new ValidationResult("Only Completed results have finishing positions.", [nameof(OverallPosition), nameof(CategoryPosition)]);
        }
    }
}

/// <summary>Shared paging and status filters for private result lists.</summary>
public abstract class ResultListRequest : IValidatableObject
{
    private string? resultStatus;

    /// <summary>Page number, starting at 1.</summary>
    [FromQuery(Name = "page"), Range(1, int.MaxValue), DefaultValue(1)]
    public int Page { get; init; } = 1;

    /// <summary>Page size from 1 to 100, defaulting to 10.</summary>
    [FromQuery(Name = "pageSize"), Range(1, 100), DefaultValue(10)]
    public int PageSize { get; init; } = 10;

    /// <summary>Optional Completed, DidNotFinish, Disqualified or DidNotStart filter; ignores case and surrounding spaces.</summary>
    [FromQuery(Name = "resultStatus"), DisplayFormat(ConvertEmptyStringToNull = false)]
    [MinLength(1), RegularExpression("Completed|DidNotFinish|Disqualified|DidNotStart")]
    public string? ResultStatus { get => resultStatus; init => resultStatus = ResultStatuses.Normalize(value); }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (((long)Page - 1) * PageSize > int.MaxValue)
            yield return new ValidationResult("This page and page size exceed the supported range.", [nameof(Page)]);
    }
}

/// <summary>Filters for the signed-in Participant's results.</summary>
public sealed class MyResultsRequest : ResultListRequest
{
    /// <summary>Optional positive event ID.</summary>
    [FromQuery(Name = "eventId"), Range(1, int.MaxValue)]
    public int? EventId { get; init; }
}

/// <summary>Filters for results in an owned event.</summary>
public sealed class EventResultsRequest : ResultListRequest
{
    /// <summary>Optional positive category ID belonging to the route's event.</summary>
    [FromQuery(Name = "eventCategoryId"), Range(1, int.MaxValue)]
    public int? EventCategoryId { get; init; }
}

internal static class ResultStatuses
{
    internal static string? Normalize(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "completed" => "Completed", "didnotfinish" => "DidNotFinish", "disqualified" => "Disqualified",
        "didnotstart" => "DidNotStart", _ => value?.Trim()
    };
}
