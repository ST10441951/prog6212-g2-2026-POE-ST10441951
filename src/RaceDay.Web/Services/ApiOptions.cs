using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Services;

// Options validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-10.0
// Accessed: 18 November 2026.
public sealed class ApiOptions
{
    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 15;
}
