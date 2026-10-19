namespace RaceDay.Api.Contracts.Authentication;

// Safe response projections follow docs/API-Endpoint-Plan.md, section 5.
// Response DTOs: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api#prevent-over-posting
// Accessed: 19 October 2026.
/// <summary>Public account summary without credentials.</summary>
public sealed class AccountResponse
{
    /// <summary>Saved account identifier.</summary>
    public required int UserId { get; init; }

    /// <summary>Account first name.</summary>
    public required string FirstName { get; init; }

    /// <summary>Account surname.</summary>
    public required string LastName { get; init; }

    /// <summary>Normalised sign-in email.</summary>
    public required string Email { get; init; }

    /// <summary>Organiser or Participant.</summary>
    public required string Role { get; init; }

    /// <summary>Whether the account is active.</summary>
    public required bool IsActive { get; init; }
}
