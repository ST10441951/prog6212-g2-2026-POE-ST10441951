using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Contracts.Events;

// Required fields, SQL precision and cross-field validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members
// Field sizes follow docs/RaceDay-Data-Dictionary.md.
// Accessed: 18 October 2026.
/// <summary>Editable event details. The API assigns ownership, identity and initial status.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class CreateEventRequest : IValidatableObject
{
    /// <summary>Required event name, up to 120 characters.</summary>
    [Required, StringLength(120)]
    public string EventName { get; init; } = string.Empty;

    /// <summary>Running, Walking or Cycling.</summary>
    [Required, RegularExpression("Running|Walking|Cycling")]
    public string EventType { get; init; } = string.Empty;

    /// <summary>Required description, up to 1000 characters.</summary>
    [Required, StringLength(1000)]
    public string Description { get; init; } = string.Empty;

    /// <summary>Distance from 0.01 to 9999.99 kilometres, with at most two decimal places.</summary>
    [Required, Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? DistanceKm { get; init; }

    /// <summary>Required event start with a UTC or numeric offset, to whole seconds.</summary>
    [Required]
    public DateTimeOffset? EventDateTime { get; init; }

    /// <summary>Required entry closing time, earlier than the event start, to whole seconds.</summary>
    [Required]
    public DateTimeOffset? EntryClosingDateTime { get; init; }

    /// <summary>Required venue name, up to 120 characters.</summary>
    [Required, StringLength(120)]
    public string VenueName { get; init; } = string.Empty;

    /// <summary>Required street address, up to 120 characters.</summary>
    [Required, StringLength(120)]
    public string AddressLine1 { get; init; } = string.Empty;

    /// <summary>Required city or town, up to 80 characters.</summary>
    [Required, StringLength(80)]
    public string City { get; init; } = string.Empty;

    /// <summary>One of the nine full South African province names.</summary>
    [Required, RegularExpression("Eastern Cape|Free State|Gauteng|KwaZulu-Natal|Limpopo|Mpumalanga|Northern Cape|North West|Western Cape")]
    public string Province { get; init; } = string.Empty;

    /// <summary>Optional latitude from -90 to 90, with at most six decimal places.</summary>
    [Range(typeof(decimal), "-90", "90")]
    public decimal? Latitude { get; init; }

    /// <summary>Optional longitude from -180 to 180, with at most six decimal places.</summary>
    [Range(typeof(decimal), "-180", "180")]
    public decimal? Longitude { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EntryClosingDateTime >= EventDateTime)
            yield return new ValidationResult("Entries must close before the event starts.", [nameof(EntryClosingDateTime)]);
        if (Latitude.HasValue != Longitude.HasValue)
            yield return new ValidationResult("Supply both coordinates or leave both empty.", [nameof(Latitude), nameof(Longitude)]);
        if (DistanceKm is { } distance && decimal.Round(distance, 2) != distance)
            yield return new ValidationResult("Use at most two decimal places.", [nameof(DistanceKm)]);
        if (Latitude is { } latitude && decimal.Round(latitude, 6) != latitude)
            yield return new ValidationResult("Use at most six decimal places.", [nameof(Latitude)]);
        if (Longitude is { } longitude && decimal.Round(longitude, 6) != longitude)
            yield return new ValidationResult("Use at most six decimal places.", [nameof(Longitude)]);
        if (EventDateTime is { } start && start.Ticks % TimeSpan.TicksPerSecond != 0)
            yield return new ValidationResult("Use whole seconds to match the database.", [nameof(EventDateTime)]);
        if (EntryClosingDateTime is { } closing && closing.Ticks % TimeSpan.TicksPerSecond != 0)
            yield return new ValidationResult("Use whole seconds to match the database.", [nameof(EntryClosingDateTime)]);
    }
}
