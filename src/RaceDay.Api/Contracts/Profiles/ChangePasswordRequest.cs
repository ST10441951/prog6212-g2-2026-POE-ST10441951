using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Contracts.Profiles;

// Required, length, pattern and matching-field validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members
// Accessed: 18 October 2026.
/// <summary>Current password and a confirmed replacement.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class ChangePasswordRequest
{
    /// <summary>Required current password, up to 128 characters.</summary>
    [Required, StringLength(128)]
    public string CurrentPassword { get; init; } = string.Empty;

    /// <summary>8 to 128 characters with an upper-case letter, lower-case letter, digit and symbol.</summary>
    [Required, StringLength(128, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9\s]).+$",
        ErrorMessage = "Use an upper-case letter, lower-case letter, digit and symbol.")]
    public string NewPassword { get; init; } = string.Empty;

    /// <summary>Must exactly match the new password.</summary>
    [Required, StringLength(128), Compare(nameof(NewPassword))]
    public string ConfirmPassword { get; init; } = string.Empty;
}
