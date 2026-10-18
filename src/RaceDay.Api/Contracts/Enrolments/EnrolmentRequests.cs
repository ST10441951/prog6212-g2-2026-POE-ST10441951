using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Contracts.Enrolments;

// Explicit JSON fields, validation and query binding: Microsoft (2026).
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// Accessed: 18 October 2026.
/// <summary>Event and category selected by the signed-in Participant.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CreateEnrolmentRequest
{
    /// <summary>Required positive event ID.</summary>
    [Required, Range(1, int.MaxValue)]
    public int? EventId { get; init; }

    /// <summary>Required positive category ID belonging to the event.</summary>
    [Required, Range(1, int.MaxValue)]
    public int? EventCategoryId { get; init; }
}

/// <summary>Category selected when reactivating a Cancelled entry.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ReactivateEnrolmentRequest
{
    /// <summary>Required positive category ID belonging to the saved event.</summary>
    [Required, Range(1, int.MaxValue)]
    public int? EventCategoryId { get; init; }
}

/// <summary>Bib number assigned by the event's Organiser.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class BibNumberRequest
{
    private string bibNumber = string.Empty;

    /// <summary>One to 20 letters, digits or hyphens, starting with a letter or digit; stored in uppercase.</summary>
    [Required, StringLength(20), RegularExpression("[A-Z0-9][A-Z0-9-]*")]
    public string BibNumber { get => bibNumber; init => bibNumber = value?.Trim().ToUpperInvariant() ?? string.Empty; }
}

/// <summary>Shared paging and status filters for private enrolment lists.</summary>
public abstract class EnrolmentListRequest : IValidatableObject
{
    private string? status;

    /// <summary>Page number, starting at 1.</summary>
    [FromQuery(Name = "page"), Range(1, int.MaxValue), DefaultValue(1)]
    public int Page { get; init; } = 1;

    /// <summary>Page size from 1 to 100, defaulting to 10.</summary>
    [FromQuery(Name = "pageSize"), Range(1, 100), DefaultValue(10)]
    public int PageSize { get; init; } = 10;

    /// <summary>Optional Confirmed or Cancelled status; ignores case and surrounding spaces.</summary>
    [FromQuery(Name = "status"), DisplayFormat(ConvertEmptyStringToNull = false)]
    [MinLength(1), RegularExpression("(?i)(Confirmed|Cancelled)")]
    public string? Status { get => status; init => status = value?.Trim(); }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (((long)Page - 1) * PageSize > int.MaxValue)
            yield return new ValidationResult("This page and page size exceed the supported range.", [nameof(Page)]);
    }
}

/// <summary>Filters for the signed-in Participant's entries.</summary>
public sealed class MyEnrolmentsRequest : EnrolmentListRequest
{
    /// <summary>Optional positive event ID.</summary>
    [FromQuery(Name = "eventId"), Range(1, int.MaxValue)]
    public int? EventId { get; init; }
}

/// <summary>Filters for entries in an owned event.</summary>
public sealed class EventEnrolmentsRequest : EnrolmentListRequest
{
    /// <summary>Optional positive category ID belonging to the route's event.</summary>
    [FromQuery(Name = "eventCategoryId"), Range(1, int.MaxValue)]
    public int? EventCategoryId { get; init; }
}
