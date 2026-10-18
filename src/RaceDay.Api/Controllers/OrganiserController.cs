using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

// Role checks and server-side aggregate projections: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators
// Accessed: 18 October 2026.
/// <summary>View the signed-in Organiser's dashboard.</summary>
[ApiController]
[Authorize(Policy = "Organiser")]
[Route("api/organiser")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrganiserController(RaceDayDbContext database, TimeProvider clock) : ControllerBase
{
    /// <summary>View your event and enrolment totals.</summary>
    /// <remarks>
    /// Takes no input. Includes every owned event and its UTC date, ordered by date then ID.
    /// TotalEnrolments counts all saved Confirmed and Cancelled rows, including historical entries.
    /// ConfirmedEnrolments and CancelledEnrolments count those row statuses separately.
    /// UpcomingEvents counts future Open and Closed events. Another Organiser's events and entries
    /// are excluded. An account without events receives zero totals and an empty events array.
    /// </remarks>
    /// <response code="200">Owned event summaries and saved-entry totals.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">The account is inactive or is not an Organiser.</response>
    [HttpGet("dashboard")]
    [ProducesResponseType<OrganiserDashboardResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<ActionResult<OrganiserDashboardResponse>> Dashboard(CancellationToken cancellationToken)
    {
        int ownerId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
        var events = await database.Events.AsNoTracking().Where(x => x.OrganiserUserId == ownerId)
            .OrderBy(x => x.EventDateTime).ThenBy(x => x.EventId).Select(x => new DashboardEventResponse
            {
                EventId = x.EventId, EventName = x.EventName, Status = x.Status,
                EventDateTime = DateTime.SpecifyKind(x.EventDateTime, DateTimeKind.Utc),
                TotalEnrolments = x.Enrolments.Count,
                ConfirmedEnrolments = x.Enrolments.Count(e => e.Status == "Confirmed"),
                CancelledEnrolments = x.Enrolments.Count(e => e.Status == "Cancelled")
            }).ToListAsync(cancellationToken);
        DateTime now = clock.GetUtcNow().UtcDateTime;
        return Ok(new OrganiserDashboardResponse
        {
            TotalEvents = events.Count, TotalEnrolments = events.Sum(x => x.TotalEnrolments),
            ConfirmedEnrolments = events.Sum(x => x.ConfirmedEnrolments),
            CancelledEnrolments = events.Sum(x => x.CancelledEnrolments),
            UpcomingEvents = events.Count(x => x.EventDateTime > now && (x.Status is "Open" or "Closed")),
            Events = events
        });
    }
}
