using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Contracts;
using Microsoft.AspNetCore.Authorization;

namespace RaceDay.Api.Controllers;

/// <summary>
/// Provides a public host check separate from the planned business endpoints.
/// </summary>
// Controller routing: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0
// Accessed: 17 October 2026.
[ApiController]
[AllowAnonymous]
[Route("health")]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Check that the RaceDay API is running.
    /// </summary>
    /// <remarks>
    /// Takes no input. This checks the API process only, without querying SQL Server or storage.
    /// </remarks>
    /// <response code="200">The API is responding and returns its host status.</response>
    [HttpGet]
    [ProducesResponseType<HealthResponse>(StatusCodes.Status200OK)]
    public ActionResult<HealthResponse> Get()
    {
        return Ok(new HealthResponse { Status = "Healthy" });
    }
}
