using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Api.Contracts.Events;

// Query validation and paging: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// Accessed: 18 October 2026.
/// <summary>Optional filters and paging for upcoming public events.</summary>
public sealed class EventBrowseRequest : IValidatableObject
{
    private string? eventType;
    private string? province;

    /// <summary>Page number, starting at 1. Defaults to 1.</summary>
    [FromQuery(Name = "page"), Range(1, int.MaxValue), DefaultValue(1)]
    public int Page { get; init; } = 1;

    /// <summary>Events per page, from 1 to 100. Defaults to 10.</summary>
    [FromQuery(Name = "pageSize"), Range(1, 100), DefaultValue(10)]
    public int PageSize { get; init; } = 10;

    /// <summary>Optional Running, Walking or Cycling filter. Case is ignored.</summary>
    [FromQuery(Name = "eventType"), DisplayFormat(ConvertEmptyStringToNull = false)]
    [MinLength(1), RegularExpression("(?i)(Running|Walking|Cycling)")]
    public string? EventType
    {
        get => eventType;
        init => eventType = value?.Trim();
    }

    /// <summary>Optional full South African province name. Case is ignored.</summary>
    [FromQuery(Name = "province"), DisplayFormat(ConvertEmptyStringToNull = false)]
    [MinLength(1), RegularExpression("(?i)(Eastern Cape|Free State|Gauteng|KwaZulu-Natal|Limpopo|Mpumalanga|Northern Cape|North West|Western Cape)")]
    public string? Province
    {
        get => province;
        init => province = value?.Trim();
    }

    /// <summary>Optional first event date, inclusive, in UTC. Use YYYY-MM-DD.</summary>
    [FromQuery(Name = "fromDate")]
    public DateOnly? FromDate { get; init; }

    /// <summary>Optional final event date, inclusive, in UTC. Use YYYY-MM-DD.</summary>
    [FromQuery(Name = "toDate")]
    public DateOnly? ToDate { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FromDate > ToDate)
            yield return new ValidationResult("The final date cannot be earlier than the first date.",
                [nameof(ToDate)]);
        if (((long)Page - 1) * PageSize > int.MaxValue)
            yield return new ValidationResult("This page and page size exceed the supported range.",
                [nameof(Page)]);
    }
}
