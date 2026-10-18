using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Contracts.Routes;

// Model validation, URL parsing and explicit JSON fields: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/api/system.uri.iswellformeduristring
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members
// SQL sizes follow docs/RaceDay-Data-Dictionary.md.
// Accessed: 18 October 2026.
/// <summary>Editable route information. The API assigns the category link, ID and timestamps.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RouteWriteRequest : IValidatableObject
{
    private string routeName = string.Empty;
    private string startLocation = string.Empty;
    private string finishLocation = string.Empty;
    private string routeDescription = string.Empty;
    private string? routeMapUrl;

    /// <summary>Required trimmed route name, up to 120 characters.</summary>
    [Required, StringLength(120)]
    public string RouteName { get => routeName; init => routeName = value?.Trim() ?? string.Empty; }

    /// <summary>Required description of the start, up to 200 characters.</summary>
    [Required, StringLength(200)]
    public string StartLocation { get => startLocation; init => startLocation = value?.Trim() ?? string.Empty; }

    /// <summary>Required description of the finish, up to 200 characters.</summary>
    [Required, StringLength(200)]
    public string FinishLocation { get => finishLocation; init => finishLocation = value?.Trim() ?? string.Empty; }

    /// <summary>Required instructions, up to 1000 characters.</summary>
    [Required, StringLength(1000)]
    public string RouteDescription { get => routeDescription; init => routeDescription = value?.Trim() ?? string.Empty; }

    /// <summary>Optional absolute HTTP or HTTPS map URL, up to 500 characters, without credentials.</summary>
    [StringLength(500)]
    public string? RouteMapUrl
    {
        get => routeMapUrl;
        init => routeMapUrl = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Optional elevation gain in metres, from zero to 2147483647.</summary>
    [Range(0, int.MaxValue)]
    public int? ElevationGainMetres { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RouteMapUrl is not null && (!Uri.IsWellFormedUriString(RouteMapUrl, UriKind.Absolute) ||
            !Uri.TryCreate(RouteMapUrl, UriKind.Absolute, out var url) ||
            (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(url.Host) || !string.IsNullOrEmpty(url.UserInfo)))
            yield return new ValidationResult("Use an absolute HTTP or HTTPS map URL without credentials.", [nameof(RouteMapUrl)]);
    }
}
