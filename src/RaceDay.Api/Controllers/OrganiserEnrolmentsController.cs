using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Enrolments;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

// Ownership, paging and unique writes: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/querying/pagination
// https://learn.microsoft.com/en-us/ef/core/modeling/indexes
// Accessed: 18 October 2026.
/// <summary>Owned-event rosters and bib assignments.</summary>
[ApiController]
[Authorize(Policy = "Organiser")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
public sealed class OrganiserEnrolmentsController(RaceDayDbContext database, TimeProvider clock) : ControllerBase
{
    /// <summary>List enrolments for your own event.</summary>
    /// <remarks>
    /// Requires an Organiser session and event ownership. Includes Confirmed and Cancelled history
    /// in every event status. Optional filters are status and eventCategoryId. The category filter
    /// must belong to this event. Page starts at 1, pageSize defaults to 10 and is limited to 100.
    /// Newest entry time then highest ID comes first. Counts apply after filters, before paging.
    /// Returns Participant names and entry/result summaries, excluding account contact details.
    /// Empty matches or later pages return 200 with an empty items array.
    /// </remarks>
    /// <response code="200">A page of the owned event's entries.</response>
    /// <response code="400">Invalid filters, paging or category does not belong to this event.</response>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [ProducesResponseType<EnrolmentPageResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<EnrolmentPageResponse>> List(int eventId, [FromQuery] EventEnrolmentsRequest request,
        CancellationToken cancellationToken)
    {
        var item = await database.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != UserId()) return Problem(statusCode: 403, title: "You do not manage this event");
        var query = database.Enrolments.AsNoTracking().Where(x => x.EventId == eventId);
        if (request.EventCategoryId is { } categoryId)
        {
            if (!await database.EventCategories.AnyAsync(x => x.EventId == eventId && x.EventCategoryId == categoryId, cancellationToken))
            {
                ModelState.AddModelError(nameof(request.EventCategoryId), "Choose a category belonging to this event.");
                return ValidationProblem(ModelState);
            }
            query = query.Where(x => x.EventCategoryId == categoryId);
        }
        return Ok(await EnrolmentResponses.Page(query, request, cancellationToken));
    }

    /// <summary>Assign or correct a bib number in your own event.</summary>
    /// <remarks>
    /// Send bibNumber as JSON: one to 20 letters, digits or hyphens, starting with a letter or digit.
    /// The value is trimmed and stored in uppercase. It must be unique within the event, including
    /// Cancelled entries; another event may reuse it. Repeating the same value for the same entry
    /// returns 200. Both entry statuses and historical events may be corrected. The entry identity,
    /// Participant, event, category, status and original entry time remain unchanged.
    /// </remarks>
    /// <response code="200">The entry with its saved bib number.</response>
    /// <response code="400">Invalid bib number or unexpected JSON properties.</response>
    /// <response code="409">The bib number is already assigned within the event.</response>
    /// <response code="415">Send JSON.</response>
    [HttpPut("api/enrolments/{enrolmentId:int}/bib-number")]
    [Consumes("application/json")]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<EnrolmentResponse>> BibNumber(int enrolmentId, BibNumberRequest request,
        CancellationToken cancellationToken)
    {
        var saved = await database.Enrolments.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(x => new { x.EventId }).SingleOrDefaultAsync(cancellationToken);
        if (saved is null) return Problem(statusCode: 404, title: "Enrolment not found");
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, saved.EventId).SingleOrDefaultAsync(cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != UserId()) return Problem(statusCode: 403, title: "You do not manage this event");
        var entry = await database.Enrolments.SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId, cancellationToken);
        if (entry is null) return Problem(statusCode: 404, title: "Enrolment not found");
        if (await database.Enrolments.AnyAsync(x => x.EventId == entry.EventId && x.EnrolmentId != enrolmentId &&
            x.BibNumber == request.BibNumber, cancellationToken))
            return Problem(statusCode: 409, title: "This bib number is already assigned within the event");
        entry.BibNumber = request.BibNumber;
        entry.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Problem(statusCode: 409, title: "This bib number is already assigned within the event");
        }
        var response = await database.Enrolments.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(EnrolmentResponses.Details).SingleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(response);
    }

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
}
