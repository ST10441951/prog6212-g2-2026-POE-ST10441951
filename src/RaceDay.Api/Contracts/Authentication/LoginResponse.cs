namespace RaceDay.Api.Contracts.Authentication;

// Response fields follow docs/API-Endpoint-Plan.md, section 5.
/// <summary>Safe account details and the server-side session expiry.</summary>
public sealed class LoginResponse
{
    /// <summary>Fixed UTC expiry, 20 minutes after login.</summary>
    public required DateTimeOffset SessionExpiresAt { get; init; }

    /// <summary>Signed-in account summary without credentials.</summary>
    public required AccountResponse User { get; init; }
}
