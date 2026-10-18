using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Contracts.Authentication;

// Request validation and unexpected JSON fields: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members
// Accessed: 18 October 2026.
/// <summary>Personal details and the selected account role.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class RegisterRequest
{
    private string email = string.Empty;
    private string? phoneNumber;

    /// <summary>Required first name, up to 50 characters.</summary>
    [Required, StringLength(50)]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Required surname, up to 50 characters.</summary>
    [Required, StringLength(50)]
    public string LastName { get; init; } = string.Empty;

    /// <summary>Sign-in email, trimmed and stored in lower case.</summary>
    [Required, EmailAddress, StringLength(254)]
    public string Email
    {
        get => email;
        init => email = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>8 to 128 characters with an upper-case letter, lower-case letter, digit and symbol.</summary>
    [Required, StringLength(128, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9\s]).+$",
        ErrorMessage = "Use an upper-case letter, lower-case letter, digit and symbol.")]
    public string Password { get; init; } = string.Empty;

    /// <summary>Choose Organiser or Participant.</summary>
    [Required, RegularExpression("Organiser|Participant")]
    public string Role { get; init; } = string.Empty;

    /// <summary>Optional phone number, up to 20 characters.</summary>
    [Phone, StringLength(20)]
    public string? PhoneNumber
    {
        get => phoneNumber;
        init => phoneNumber = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Optional date of birth, earlier than today's UTC date.</summary>
    public DateOnly? DateOfBirth { get; init; }
}
