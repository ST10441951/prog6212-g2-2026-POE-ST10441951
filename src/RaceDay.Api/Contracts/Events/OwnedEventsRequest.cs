using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Contracts.Events;

// Query validation and paging: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// Accessed: 18 October 2026.
/// <summary>Paging and an optional status filter for the signed-in Organiser's events.</summary>
public sealed class OwnedEventsRequest : IValidatableObject
{
    private string? status;

    /// <summary>Page number, starting at 1.</summary>
    [FromQuery(Name = "page"), Range(1, int.MaxValue), DefaultValue(1)]
    public int Page { get; init; } = 1;

    /// <summary>Page size from 1 to 100, defaulting to 10.</summary>
    [FromQuery(Name = "pageSize"), Range(1, 100), DefaultValue(10)]
    public int PageSize { get; init; } = 10;

    /// <summary>Optional Draft, Open, Closed, Cancelled or Completed filter; case is ignored.</summary>
    [FromQuery(Name = "status"), DisplayFormat(ConvertEmptyStringToNull = false)]
    [MinLength(1), RegularExpression("(?i)(Draft|Open|Closed|Cancelled|Completed)")]
    public string? Status
    {
        get => status;
        init => status = value?.Trim();
    }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (((long)Page - 1) * PageSize > int.MaxValue)
            yield return new ValidationResult("This page and page size exceed the supported range.", [nameof(Page)]);
    }
}
