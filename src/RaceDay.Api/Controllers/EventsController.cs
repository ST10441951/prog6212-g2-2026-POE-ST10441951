using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

// Filtering, stable pagination and related data: Microsoft (2026).
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager
// https://learn.microsoft.com/en-us/ef/core/querying/tracking
// Accessed: 18 October 2026.
/// <summary>Browse public events and view event details.</summary>
[ApiController]
[Route("api/events")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EventsController(RaceDayDbContext database, TimeProvider clock) : ControllerBase
{
    /// <summary>Browse upcoming public events.</summary>
    /// <remarks>
    /// No login is required. Returns Open and Closed events whose UTC start is later than now.
    /// Filter by Running, Walking or Cycling, full South African province name, and inclusive UTC
    /// fromDate/toDate values (YYYY-MM-DD). Text filters are trimmed and case-insensitive.
    /// Page defaults to 1 and pageSize to 10, with a maximum of 100. Results are ordered by start time,
    /// then event ID. An empty or out-of-range page returns 200 with an empty items array and matching totals.
    /// Draft, Cancelled and Completed events are never included in this upcoming list, even for their owner.
    /// </remarks>
    /// <response code="200">Event summaries with page, pageSize, totalCount, totalPages and items.</response>
    /// <response code="400">A filter, date range or paging value is invalid.</response>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<EventPageResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<EventPageResponse>> Browse([FromQuery] EventBrowseRequest request,
        CancellationToken cancellationToken)
    {
        DateTime now = clock.GetUtcNow().UtcDateTime;
        var query = database.Events.AsNoTracking()
            .Where(x => (x.Status == "Open" || x.Status == "Closed") && x.EventDateTime > now);

        // Normalising stored values also works with case-sensitive SQL Server collations.
        if (request.EventType is not null)
        {
            string eventType = request.EventType.ToUpperInvariant();
            query = query.Where(x => x.EventType.ToUpper() == eventType);
        }
        if (request.Province is not null)
        {
            string province = request.Province.ToUpperInvariant();
            query = query.Where(x => x.Province.ToUpper() == province);
        }
        if (request.FromDate is { } fromDate)
        {
            DateTime firstDate = fromDate.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.EventDateTime >= firstDate);
        }
        if (request.ToDate is { } toDate)
        {
            DateTime lastDate = toDate.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.EventDateTime.Date <= lastDate);
        }

        int count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.EventDateTime).ThenBy(x => x.EventId)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(EventResponses.Summary).ToListAsync(cancellationToken);

        return Ok(new EventPageResponse
        {
            Page = request.Page, PageSize = request.PageSize, TotalCount = count,
            TotalPages = (int)(((long)count + request.PageSize - 1) / request.PageSize), Items = items
        });
    }

    /// <summary>View an event and its visible categories and routes.</summary>
    /// <remarks>
    /// Open, Closed and Completed events can be read without login, including historical events.
    /// Log in as the owning Organiser to read Draft or Cancelled events and unavailable categories.
    /// Missing records and records hidden from the caller both return 404. Invalid or expired cookies
    /// do not grant owner access; public event details remain readable. Responses contain no account
    /// contact details, enrolments, participant records or storage object names. BannerImageUrl is null
    /// until image storage is added. Responses are not cached because visible categories depend on identity.
    /// </remarks>
    /// <response code="200">Event details with visible categories and each category's route, if present.</response>
    /// <response code="404">The event does not exist or is not visible to this caller.</response>
    [AllowAnonymous]
    [OptionalSession]
    [HttpGet("{eventId:int}")]
    [ProducesResponseType<EventDetailsResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<EventDetailsResponse>> Details(int eventId, CancellationToken cancellationToken)
    {
        int? organiserId = User.IsInRole("Organiser") &&
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId) ? userId : null;
        var item = await database.Events.AsNoTracking().Where(x => x.EventId == eventId &&
                (x.Status == "Open" || x.Status == "Closed" || x.Status == "Completed" ||
                 x.OrganiserUserId == organiserId))
            .Include(x => x.Categories.Where(c => c.IsAvailable || c.Event.OrganiserUserId == organiserId))
            .ThenInclude(x => x.Route).SingleOrDefaultAsync(cancellationToken);
        if (item is null)
            return Problem(statusCode: 404, title: "Event not found");

        return Ok(EventResponses.Details(item));
    }
}
