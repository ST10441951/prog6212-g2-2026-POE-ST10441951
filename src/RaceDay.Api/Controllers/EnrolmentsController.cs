using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Enrolments;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

// Role checks, projections and transactional writes: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// https://learn.microsoft.com/en-us/ef/core/saving/basic
// Rules follow docs/API-Endpoint-Plan.md, section 10.
// Accessed: 18 October 2026.
/// <summary>Private Participant entries and the related Organiser's detail view.</summary>
[ApiController]
[Authorize]
[Route("api/enrolments")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class EnrolmentsController(RaceDayDbContext database, TimeProvider clock) : ControllerBase
{
    /// <summary>Enter an Open event using an available category.</summary>
    /// <remarks>
    /// Requires a Participant session. Send positive eventId and eventCategoryId values as JSON;
    /// the API assigns the Participant, Confirmed status, entry ID and UTC entry time. The category
    /// must belong to the event. Entries must still be open, age limits are inclusive on the UTC
    /// event date, and a date of birth is needed for age-restricted categories. Only Confirmed
    /// entries use capacity. An existing entry, including Cancelled history, must be reactivated
    /// rather than inserted again. Follow Location with the same session to read the saved entry.
    /// </remarks>
    /// <response code="201">Confirmed entry and its readable Location.</response>
    /// <response code="400">Invalid fields, age outside the category limits or missing date of birth.</response>
    /// <response code="404">Event or category missing, or category belongs elsewhere.</response>
    /// <response code="409">Entry already exists, entries closed, category unavailable or full.</response>
    /// <response code="415">Send JSON.</response>
    [Authorize(Policy = "Participant")]
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<EnrolmentResponse>> Create(CreateEnrolmentRequest request, CancellationToken cancellationToken)
    {
        int participantId = UserId();
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, request.EventId!.Value).SingleOrDefaultAsync(cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        var category = await database.EventCategories.SingleOrDefaultAsync(x => x.EventCategoryId == request.EventCategoryId &&
            x.EventId == item.EventId, cancellationToken);
        if (category is null) return Problem(statusCode: 404, title: "Category not found");
        if (await database.Enrolments.AnyAsync(x => x.EventId == item.EventId && x.ParticipantUserId == participantId, cancellationToken))
            return Problem(statusCode: 409, title: "An entry already exists; reactivate a Cancelled entry instead");
        var failure = await EnrolmentRules.CanEnter(database, item, category, participantId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (failure is { } entryError) return Problem(statusCode: entryError.Status, title: entryError.Message);
        var entry = new Enrolment
        {
            ParticipantUserId = participantId, EventId = item.EventId, EventCategoryId = category.EventCategoryId,
            Status = "Confirmed", EnrolmentDate = clock.GetUtcNow().UtcDateTime
        };
        database.Enrolments.Add(entry);
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Problem(statusCode: 409, title: "An entry already exists for this event");
        }
        var response = await ReadResponse(entry.EnrolmentId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Created($"/api/enrolments/{entry.EnrolmentId}", response);
    }

    /// <summary>List your own enrolment history.</summary>
    /// <remarks>
    /// Requires a Participant session. Optional filters are status and eventId. Status is Confirmed
    /// or Cancelled and ignores case and surrounding spaces. Page starts at 1; pageSize defaults to
    /// 10 and is limited to 100. Newest entry time then highest ID comes first. Totals are calculated
    /// after access and filters, before paging. Empty matches and later pages return 200.
    /// No request value can select another Participant's private entries.
    /// </remarks>
    /// <response code="200">A page of the signed-in Participant's entries.</response>
    /// <response code="400">Invalid filters or paging.</response>
    [Authorize(Policy = "Participant")]
    [HttpGet("mine")]
    [ProducesResponseType<EnrolmentPageResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<EnrolmentPageResponse>> Mine([FromQuery] MyEnrolmentsRequest request, CancellationToken cancellationToken)
    {
        int participantId = UserId();
        var query = database.Enrolments.AsNoTracking().Where(x => x.ParticipantUserId == participantId);
        if (request.EventId is { } eventId) query = query.Where(x => x.EventId == eventId);
        return Ok(await EnrolmentResponses.Page(query, request, cancellationToken));
    }

    /// <summary>View one entry you own or manage.</summary>
    /// <remarks>
    /// Takes only the entry ID. Requires a valid session belonging to the Participant who entered
    /// or the Organiser who owns its event. Includes event/category names and an optional result
    /// summary. Both Confirmed and Cancelled history can be read, including closed and past events.
    /// Unrelated users receive 403. Account contact details, birth dates and passwords are excluded.
    /// </remarks>
    /// <response code="200">The private entry details.</response>
    /// <response code="404">The entry does not exist.</response>
    [HttpGet("{enrolmentId:int}")]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<EnrolmentResponse>> Details(int enrolmentId, CancellationToken cancellationToken)
    {
        var access = await database.Enrolments.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(x => new { x.ParticipantUserId, x.Event.OrganiserUserId }).SingleOrDefaultAsync(cancellationToken);
        if (access is null) return Problem(statusCode: 404, title: "Enrolment not found");
        if (!(User.IsInRole("Participant") && access.ParticipantUserId == UserId()) &&
            !(User.IsInRole("Organiser") && access.OrganiserUserId == UserId()))
            return Problem(statusCode: 403, title: "You do not own or manage this enrolment");
        var response = await database.Enrolments.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(EnrolmentResponses.Details).SingleOrDefaultAsync(cancellationToken);
        return response is null ? Problem(statusCode: 404, title: "Enrolment not found") : Ok(response);
    }

    /// <summary>Cancel your own Confirmed entry and keep its history.</summary>
    /// <remarks>
    /// Takes no body. Requires a Participant session, entry ownership, an Open event, a future
    /// entry-closing time and no official result. Changes only status and the UTC update time.
    /// The original entry ID, entry time, category and bib number remain. Cancelled entries no
    /// longer use a capacity place; cancelling the same entry again returns 409.
    /// </remarks>
    /// <response code="200">The saved Cancelled entry.</response>
    /// <response code="404">The entry does not exist.</response>
    /// <response code="409">Already Cancelled, result exists, event is not Open or entries have closed.</response>
    [Authorize(Policy = "Participant")]
    [HttpPut("{enrolmentId:int}/cancel")]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<EnrolmentResponse>> Cancel(int enrolmentId, CancellationToken cancellationToken)
    {
        var saved = await database.Enrolments.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(x => new { x.EventId, x.ParticipantUserId }).SingleOrDefaultAsync(cancellationToken);
        if (saved is null) return Problem(statusCode: 404, title: "Enrolment not found");
        if (saved.ParticipantUserId != UserId()) return Problem(statusCode: 403, title: "You do not own this enrolment");
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, saved.EventId).SingleOrDefaultAsync(cancellationToken);
        var entry = await database.Enrolments.SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId, cancellationToken);
        if (item is null || entry is null) return Problem(statusCode: 404, title: "Enrolment not found");
        if (entry.Status != "Confirmed" || !EnrolmentRules.EntriesOpen(item, clock.GetUtcNow().UtcDateTime) ||
            await database.Results.AnyAsync(x => x.EnrolmentId == enrolmentId, cancellationToken))
            return Problem(statusCode: 409, title: "Cancellation requires a Confirmed entry, open entries and no result");
        entry.Status = "Cancelled";
        entry.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await database.SaveChangesAsync(cancellationToken);
        var response = await ReadResponse(enrolmentId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>Reactivate your own Cancelled entry.</summary>
    /// <remarks>
    /// Send eventCategoryId as JSON. The category must belong to the saved event. Repeats the Open
    /// event, closing time, availability, event-date age and capacity checks. A result or Confirmed
    /// status prevents reactivation. Keeps the original entry ID, Participant, event and entry time.
    /// A changed category clears the old bib number; the same category keeps it.
    /// </remarks>
    /// <response code="200">The saved Confirmed entry.</response>
    /// <response code="400">Invalid fields, age outside the category limits or missing date of birth.</response>
    /// <response code="404">Entry or selected category missing, or category belongs elsewhere.</response>
    /// <response code="409">Already Confirmed, result exists, entries closed, category unavailable or full.</response>
    /// <response code="415">Send JSON.</response>
    [Authorize(Policy = "Participant")]
    [HttpPut("{enrolmentId:int}/reactivate")]
    [Consumes("application/json")]
    [ProducesResponseType<EnrolmentResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<EnrolmentResponse>> Reactivate(int enrolmentId, ReactivateEnrolmentRequest request,
        CancellationToken cancellationToken)
    {
        var saved = await database.Enrolments.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(x => new { x.EventId, x.ParticipantUserId }).SingleOrDefaultAsync(cancellationToken);
        if (saved is null) return Problem(statusCode: 404, title: "Enrolment not found");
        if (saved.ParticipantUserId != UserId()) return Problem(statusCode: 403, title: "You do not own this enrolment");
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, saved.EventId).SingleOrDefaultAsync(cancellationToken);
        var entry = await database.Enrolments.SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId, cancellationToken);
        if (item is null || entry is null) return Problem(statusCode: 404, title: "Enrolment not found");
        var category = await database.EventCategories.SingleOrDefaultAsync(x => x.EventCategoryId == request.EventCategoryId &&
            x.EventId == entry.EventId, cancellationToken);
        if (category is null) return Problem(statusCode: 404, title: "Category not found");
        if (entry.Status != "Cancelled" || await database.Results.AnyAsync(x => x.EnrolmentId == enrolmentId, cancellationToken))
            return Problem(statusCode: 409, title: "Reactivation requires a Cancelled entry without a result");
        var failure = await EnrolmentRules.CanEnter(database, item, category, entry.ParticipantUserId, clock.GetUtcNow().UtcDateTime, cancellationToken);
        if (failure is { } entryError) return Problem(statusCode: entryError.Status, title: entryError.Message);
        if (entry.EventCategoryId != category.EventCategoryId) entry.BibNumber = null;
        entry.EventCategoryId = category.EventCategoryId;
        entry.Status = "Confirmed";
        entry.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await database.SaveChangesAsync(cancellationToken);
        var response = await ReadResponse(enrolmentId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok(response);
    }

    /// <summary>Delete your own Confirmed entry before entries close.</summary>
    /// <remarks>
    /// Takes no body. Requires a Participant session, entry ownership, an Open event, a future
    /// entry-closing time and no official result. Cancelled entries are kept as history and cannot
    /// be deleted. Removes only the entry; the event and category remain. A later GET returns 404.
    /// </remarks>
    /// <response code="204">The entry was removed.</response>
    /// <response code="404">The entry does not exist.</response>
    /// <response code="409">Entry Cancelled, result exists, event not Open or entries have closed.</response>
    [Authorize(Policy = "Participant")]
    [HttpDelete("{enrolmentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Delete(int enrolmentId, CancellationToken cancellationToken)
    {
        var saved = await database.Enrolments.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(x => new { x.EventId, x.ParticipantUserId }).SingleOrDefaultAsync(cancellationToken);
        if (saved is null) return Problem(statusCode: 404, title: "Enrolment not found");
        if (saved.ParticipantUserId != UserId()) return Problem(statusCode: 403, title: "You do not own this enrolment");
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, saved.EventId).SingleOrDefaultAsync(cancellationToken);
        var entry = await database.Enrolments.SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId, cancellationToken);
        if (item is null || entry is null) return Problem(statusCode: 404, title: "Enrolment not found");
        if (entry.Status != "Confirmed" || !EnrolmentRules.EntriesOpen(item, clock.GetUtcNow().UtcDateTime) ||
            await database.Results.AnyAsync(x => x.EnrolmentId == enrolmentId, cancellationToken))
            return Problem(statusCode: 409, title: "Deletion requires a Confirmed entry, open entries and no result");
        database.Enrolments.Remove(entry);
        await database.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

    private Task<EnrolmentResponse> ReadResponse(int id, CancellationToken cancellationToken) => database.Enrolments.AsNoTracking()
        .Where(x => x.EnrolmentId == id).Select(EnrolmentResponses.Details).SingleAsync(cancellationToken);
}
