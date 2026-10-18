using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Storage;

namespace RaceDay.Api.Controllers;

// Role checks, tracked changes and transactional validation: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/saving/basic
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// Event rules follow docs/API-Endpoint-Plan.md, section 7.
// Accessed: 18 October 2026.
/// <summary>Manage events owned by the signed-in Organiser.</summary>
[ApiController]
[Authorize(Policy = "Organiser")]
[Route("api/events")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class OrganiserEventsController(RaceDayDbContext database, TimeProvider clock, IImageStorage images) : ControllerBase
{
    /// <summary>List your own events in every status.</summary>
    /// <remarks>
    /// Requires an Organiser session. Optional status ignores case and surrounding spaces.
    /// Page starts at 1; pageSize defaults to 10 and is limited to 100. Results are ordered by start
    /// time then ID. Totals are calculated after the ownership and status filters, before paging.
    /// An empty list or page beyond the matches returns 200 with an empty items array.
    /// </remarks>
    /// <response code="200">Owned event summaries and paging totals.</response>
    /// <response code="400">Invalid status, page or page size.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">The account is inactive or is not an Organiser.</response>
    [HttpGet("mine")]
    [ProducesResponseType<EventPageResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<EventPageResponse>> Mine([FromQuery] OwnedEventsRequest request, CancellationToken cancellationToken)
    {
        int ownerId = OwnerId();
        var query = database.Events.AsNoTracking().Where(x => x.OrganiserUserId == ownerId);
        if (request.Status is { } status)
        {
            string filter = status.ToUpperInvariant();
            query = query.Where(x => x.Status.ToUpper() == filter);
        }
        int count = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.EventDateTime).ThenBy(x => x.EventId)
            .Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(EventResponses.Summary).ToListAsync(cancellationToken);
        EventResponses.AddBannerUrls(items, images);
        return Ok(new EventPageResponse
        {
            Page = request.Page, PageSize = request.PageSize, TotalCount = count,
            TotalPages = (int)(((long)count + request.PageSize - 1) / request.PageSize), Items = items
        });
    }

    /// <summary>Create a Draft event owned by your account.</summary>
    /// <remarks>
    /// Send all planned event details as JSON. The API assigns the ID, owner, Draft status and creation
    /// time. Both dates must be future times, and entries must close before the start. Send times to
    /// whole seconds; offsets are converted to UTC. Distances use at most two decimal places and
    /// coordinates at most six. Coordinates are optional but must be supplied together.
    /// Follow Location with the same login session to read the created Draft.
    /// </remarks>
    /// <response code="201">The Draft is saved, with its details and readable Location.</response>
    /// <response code="400">Invalid or missing fields, dates, precision or unexpected JSON properties.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">The account is inactive or is not an Organiser.</response>
    /// <response code="415">Send JSON.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<EventDetailsResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<EventDetailsResponse>> Create(CreateEventRequest request, CancellationToken cancellationToken)
    {
        if (request.EventDateTime <= clock.GetUtcNow())
            ModelState.AddModelError(nameof(request.EventDateTime), "The event must start in the future.");
        if (request.EntryClosingDateTime <= clock.GetUtcNow())
            ModelState.AddModelError(nameof(request.EntryClosingDateTime), "Entries must close in the future.");
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var item = new Event { OrganiserUserId = OwnerId(), Status = "Draft", CreatedAt = clock.GetUtcNow().UtcDateTime };
        Apply(item, request);
        database.Events.Add(item);
        await database.SaveChangesAsync(cancellationToken);
        return Created($"/api/events/{item.EventId}", EventResponses.Details(item, images));
    }

    /// <summary>Update an owned event or change its status.</summary>
    /// <remarks>
    /// Send replacement event details and status as JSON. Ownership, IDs, images and timestamps cannot
    /// be assigned. Opening or reopening requires an available category and future start and closing
    /// times. Draft can become Open or Cancelled. Open and Closed can switch between each other,
    /// become Cancelled, or become Completed once the start has passed. Completed and Cancelled are
    /// terminal statuses. An event with results cannot be cancelled. Saved entries prevent changes
    /// to type, distance and dates. Terminal records also keep those historical fields; descriptive
    /// corrections remain allowed. Cancellation keeps categories, enrolments and results.
    /// </remarks>
    /// <response code="200">The saved owned event, including its categories.</response>
    /// <response code="400">Invalid replacement fields.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">The status or schedule change conflicts with readiness or saved history.</response>
    /// <response code="415">Send JSON.</response>
    [HttpPut("{eventId:int}")]
    [Consumes("application/json")]
    [ProducesResponseType<EventDetailsResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<EventDetailsResponse>> Update(int eventId, UpdateEventRequest request, CancellationToken cancellationToken)
    {
        // Keep history/readiness checks and the write in the same SQL transaction.
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, eventId).Include(x => x.Categories).ThenInclude(x => x.Route)
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
            return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId())
            return Problem(statusCode: 403, title: "You do not manage this event");

        bool scheduleChanged = item.EventDateTime != request.EventDateTime!.Value.UtcDateTime ||
            item.EntryClosingDateTime != request.EntryClosingDateTime!.Value.UtcDateTime ||
            item.EventType != request.EventType || item.DistanceKm != request.DistanceKm;
        bool hasEntries = await database.Enrolments.AnyAsync(x => x.EventId == eventId, cancellationToken);
        bool terminal = item.Status is "Completed" or "Cancelled";
        if (scheduleChanged && (hasEntries || terminal))
            return Problem(statusCode: 409, title: "Saved event history prevents changes to dates, type or distance");
        if (!AllowedTransition(item.Status, request.Status))
            return Problem(statusCode: 409, title: "This event status change is not allowed");
        if (request.Status == "Open" &&
            (request.EventDateTime <= clock.GetUtcNow() || request.EntryClosingDateTime <= clock.GetUtcNow() ||
             !item.Categories.Any(x => x.IsAvailable)))
            return Problem(statusCode: 409, title: "Opening requires an available category and future event and closing times");
        if (request.Status == "Completed" && request.EventDateTime > clock.GetUtcNow())
            return Problem(statusCode: 409, title: "An event cannot be completed before its start");
        if (request.Status == "Cancelled" &&
            await database.Results.AnyAsync(x => x.Enrolment.EventId == eventId, cancellationToken))
            return Problem(statusCode: 409, title: "An event with recorded results cannot be cancelled");
        if (scheduleChanged && (request.EventDateTime <= clock.GetUtcNow() || request.EntryClosingDateTime <= clock.GetUtcNow()))
            return Problem(statusCode: 409, title: "A changed schedule must have future start and closing times");

        Apply(item, request);
        item.Status = request.Status;
        item.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(EventResponses.Details(item, images));
    }

    /// <summary>Delete an owned event with no dependent records.</summary>
    /// <remarks>
    /// Takes no request body. Categories or saved entries prevent deletion; cancel the event through
    /// PUT instead to keep its history. Remove an existing banner before deleting its event.
    /// Successful deletion returns no body, and a later event read returns 404.
    /// </remarks>
    /// <response code="204">The event was deleted.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">Dependent records or an existing banner prevent deletion.</response>
    [HttpDelete("{eventId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Delete(int eventId, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, eventId).SingleOrDefaultAsync(cancellationToken);
        if (item is null)
            return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId())
            return Problem(statusCode: 403, title: "You do not manage this event");
        if (item.BannerImageBlobName is not null ||
            await database.EventCategories.AnyAsync(x => x.EventId == eventId, cancellationToken) ||
            await database.Enrolments.AnyAsync(x => x.EventId == eventId, cancellationToken))
            return Problem(statusCode: 409, title: "Remove the banner or cancel the event to preserve its related records");
        database.Events.Remove(item);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 547 })
        {
            return Problem(statusCode: 409, title: "Related records prevent deletion");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: 404, title: "Event not found");
        }
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    private int OwnerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

    private static bool AllowedTransition(string current, string requested) => current == requested || (current switch
    {
        "Draft" => requested is "Open" or "Cancelled",
        "Open" => requested is "Closed" or "Cancelled" or "Completed",
        "Closed" => requested is "Open" or "Cancelled" or "Completed",
        _ => false
    });

    private static void Apply(Event item, CreateEventRequest request)
    {
        item.EventName = request.EventName.Trim();
        item.EventType = request.EventType;
        item.Description = request.Description.Trim();
        item.DistanceKm = request.DistanceKm!.Value;
        item.EventDateTime = request.EventDateTime!.Value.UtcDateTime;
        item.EntryClosingDateTime = request.EntryClosingDateTime!.Value.UtcDateTime;
        item.VenueName = request.VenueName.Trim();
        item.AddressLine1 = request.AddressLine1.Trim();
        item.City = request.City.Trim();
        item.Province = request.Province;
        item.Latitude = request.Latitude;
        item.Longitude = request.Longitude;
    }
}
