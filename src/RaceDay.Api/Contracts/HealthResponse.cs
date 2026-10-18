namespace RaceDay.Api.Contracts;

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
