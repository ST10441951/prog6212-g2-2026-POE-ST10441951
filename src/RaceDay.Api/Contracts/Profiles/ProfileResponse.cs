namespace RaceDay.Api.Contracts.Profiles;

// Response DTOs expose selected fields rather than database navigation properties: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// Accessed: 18 October 2026.
/// <summary>Personal details of the signed-in account, excluding credentials.</summary>
public sealed class ProfileResponse
{
    /// <summary>Account identifier taken from the authenticated session.</summary>
    public required int UserId { get; init; }

    /// <summary>Saved first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>Saved surname.</summary>
    public required string LastName { get; init; }

    /// <summary>Read-only sign-in email.</summary>
    public required string Email { get; init; }

    /// <summary>Contact number, if supplied.</summary>
    public string? PhoneNumber { get; init; }

    /// <summary>Date of birth, if supplied.</summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>One-hour read-only image URL, or null when no supported image is saved.</summary>
    public string? ProfilePictureUrl { get; init; }

    /// <summary>Read-only Organiser or Participant role.</summary>
    public required string Role { get; init; }

    /// <summary>Read-only account status.</summary>
    public required bool IsActive { get; init; }

    /// <summary>UTC account creation time.</summary>
    public required DateTime CreatedAt { get; init; }
}
