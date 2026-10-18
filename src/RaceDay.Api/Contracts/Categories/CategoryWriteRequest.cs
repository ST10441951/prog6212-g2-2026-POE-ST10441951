using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Contracts.Categories;

// Model validation and explicit JSON fields: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members
// SQL sizes follow docs/RaceDay-Data-Dictionary.md.
// Accessed: 18 October 2026.
/// <summary>Editable category details. Identity, event links and timestamps are assigned by the API.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class CategoryWriteRequest : IValidatableObject
{
    private string categoryName = string.Empty;

    /// <summary>Required trimmed name, up to 80 characters; unique within the event.</summary>
    [Required, StringLength(80)]
    public string CategoryName
    {
        get => categoryName;
        init => categoryName = value?.Trim() ?? string.Empty;
    }

    /// <summary>0.01 to 9999.99 kilometres, with at most two decimal places.</summary>
    [Required, Range(typeof(decimal), "0.01", "9999.99")]
    public decimal? DistanceKm { get; init; }

    /// <summary>Optional inclusive minimum age, from 1 to 120.</summary>
    [Range(1, 120)]
    public int? MinimumAge { get; init; }

    /// <summary>Optional inclusive maximum age, from 1 to 120 and not below the minimum.</summary>
    [Range(1, 120)]
    public int? MaximumAge { get; init; }

    /// <summary>Required entry fee in rand, from 0 to 99999999.99 with at most two decimal places.</summary>
    [Required, Range(typeof(decimal), "0", "99999999.99")]
    public decimal? EntryFee { get; init; }

    /// <summary>Optional positive entry limit; null means no category capacity limit.</summary>
    [Range(1, int.MaxValue)]
    public int? Capacity { get; init; }

    /// <summary>Required true or false. False excludes the category from public reads and new entries.</summary>
    [Required]
    public bool? IsAvailable { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinimumAge > MaximumAge)
            yield return new ValidationResult("Minimum age cannot exceed maximum age.", [nameof(MinimumAge), nameof(MaximumAge)]);
        if (DistanceKm is { } distance && decimal.Round(distance, 2) != distance)
            yield return new ValidationResult("Use at most two decimal places.", [nameof(DistanceKm)]);
        if (EntryFee is { } fee && decimal.Round(fee, 2) != fee)
            yield return new ValidationResult("Use at most two decimal places.", [nameof(EntryFee)]);
    }
}
