using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaceDay.Api.Contracts.Authentication;

// Model validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Existing account credentials.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class LoginRequest
{
    private string email = string.Empty;

    /// <summary>The registered sign-in email.</summary>
    [Required, EmailAddress, StringLength(254)]
    public string Email
    {
        get => email;
        init => email = value?.Trim().ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>The account password, kept exactly as entered.</summary>
    [Required, StringLength(128)]
    public string Password { get; init; } = string.Empty;
}
