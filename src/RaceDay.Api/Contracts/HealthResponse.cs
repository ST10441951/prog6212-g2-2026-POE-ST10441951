namespace RaceDay.Api.Contracts;

// Typed API responses: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/action-return-types?view=aspnetcore-10.0
// Accessed: 19 October 2026.
/// <summary>
/// Confirms that the API process is responding.
/// </summary>
public sealed class HealthResponse
{
    /// <summary>
    /// Current host status. This does not report database or storage readiness.
    /// </summary>
    public required string Status { get; init; }
}
