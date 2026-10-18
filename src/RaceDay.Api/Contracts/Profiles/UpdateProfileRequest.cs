using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Contracts.Profiles;

// Model validation and explicit request fields: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/missing-members
// Accessed: 18 October 2026.
/// <summary>Editable personal details. Other account fields are not accepted.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class UpdateProfileRequest
{
    private string? phoneNumber;

    /// <summary>Required first name, up to 50 characters.</summary>
    [Required, StringLength(50)]
    public string FirstName { get; init; } = string.Empty;

    /// <summary>Required surname, up to 50 characters.</summary>
    [Required, StringLength(50)]
    public string LastName { get; init; } = string.Empty;

    /// <summary>Optional phone number, up to 20 characters. Blank clears the saved number.</summary>
    [Phone, StringLength(20)]
    public string? PhoneNumber
    {
        get => phoneNumber;
        init => phoneNumber = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>Optional date of birth, earlier than today's UTC date. Null clears the saved date.</summary>
    public DateOnly? DateOfBirth { get; init; }
}
