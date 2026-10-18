using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Results;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

// Role checks, transactions, projections and unique writes: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// https://learn.microsoft.com/en-us/ef/core/saving/basic
// Rules follow docs/API-Endpoint-Plan.md, section 11.
// Accessed: 18 October 2026.
/// <summary>Private official results, capture and corrections.</summary>
[ApiController]
[Authorize]
[Route("api")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
public sealed class ResultsController(RaceDayDbContext database, TimeProvider clock) : ControllerBase
{
    /// <summary>List your own official result history.</summary>
    /// <remarks>
    /// Requires a Participant session. Optional filters are resultStatus and eventId. Status ignores
    /// case and surrounding spaces. Page starts at 1, pageSize defaults to 10 and is limited to 100.
    /// Newest event date then highest result ID comes first. Counts apply after access and filters,
    /// before paging. TotalFinishers counts all Completed official results across each whole event,
    /// independently of these filters or the page. Empty matches and later pages return 200.
    /// No request value can select another Participant's private results.
    /// </remarks>
    /// <response code="200">A page of personal results and event-wide finisher totals.</response>
    /// <response code="400">Invalid filters or paging.</response>
    [Authorize(Policy = "Participant")]
    [HttpGet("results/mine")]
    [ProducesResponseType<ResultPageResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<ResultPageResponse>> Mine([FromQuery] MyResultsRequest request, CancellationToken cancellationToken)
    {
        int participantId = UserId();
        var query = database.Results.AsNoTracking().Where(x => x.Enrolment.ParticipantUserId == participantId);
        if (request.EventId is { } eventId) query = query.Where(x => x.Enrolment.EventId == eventId);
        return Ok(await ResultResponses.Page(query, request, false, cancellationToken));
    }

    /// <summary>View the official result of an entry you own or manage.</summary>
    /// <remarks>
    /// Takes the enrolment ID. Requires the entry's Participant or its event Organiser. Other users
    /// receive 403. Returns the result, event/category information, original recorder and timestamps,
    /// and the event-wide Completed-result count. Account contact details and passwords are excluded.
    /// </remarks>
    /// <response code="200">The official result details.</response>
    /// <response code="404">The entry or its result does not exist.</response>
    [HttpGet("enrolments/{enrolmentId:int}/result")]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ResultResponse>> Details(int enrolmentId, CancellationToken cancellationToken)
    {
        var access = await database.Enrolments.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(x => new { x.ParticipantUserId, x.Event.OrganiserUserId }).SingleOrDefaultAsync(cancellationToken);
        if (access is null) return Problem(statusCode: 404, title: "Enrolment not found");
        if (!(User.IsInRole("Participant") && access.ParticipantUserId == UserId()) &&
            !(User.IsInRole("Organiser") && access.OrganiserUserId == UserId()))
            return Problem(statusCode: 403, title: "You do not own or manage this result");
        var response = await database.Results.AsNoTracking().Where(x => x.EnrolmentId == enrolmentId)
            .Select(ResultResponses.Details).SingleOrDefaultAsync(cancellationToken);
        return response is null ? Problem(statusCode: 404, title: "Result not found") : Ok(response);
    }

    /// <summary>List official results for your own event.</summary>
    /// <remarks>
    /// Requires an Organiser session and event ownership. Optional filters are resultStatus and
    /// eventCategoryId; the category must belong to this event. Page starts at 1, pageSize defaults
    /// to 10 and is limited to 100. Completed results come first, then supplied overall position,
    /// finish time and result ID; missing positions/times sort after supplied ones. Status filters
    /// ignore case and surrounding spaces. Totals apply after filters, before paging; TotalFinishers
    /// always counts all Completed results in the whole event. Empty matches or pages return 200.
    /// </remarks>
    /// <response code="200">A page of the owned event's results.</response>
    /// <response code="400">Invalid filters, paging or category does not belong to the event.</response>
    /// <response code="404">The event does not exist.</response>
    [Authorize(Policy = "Organiser")]
    [HttpGet("events/{eventId:int}/results")]
    [ProducesResponseType<ResultPageResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<ResultPageResponse>> List(int eventId, [FromQuery] EventResultsRequest request,
        CancellationToken cancellationToken)
    {
        var item = await database.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != UserId()) return Problem(statusCode: 403, title: "You do not manage this event");
        var query = database.Results.AsNoTracking().Where(x => x.Enrolment.EventId == eventId);
        if (request.EventCategoryId is { } categoryId)
        {
            if (!await database.EventCategories.AnyAsync(x => x.EventId == eventId && x.EventCategoryId == categoryId, cancellationToken))
            {
                ModelState.AddModelError(nameof(request.EventCategoryId), "Choose a category belonging to this event.");
                return ValidationProblem(ModelState);
            }
            query = query.Where(x => x.Enrolment.EventCategoryId == categoryId);
        }
        return Ok(await ResultResponses.Page(query, request, true, cancellationToken));
    }

    /// <summary>Record the first official result for an owned event entry.</summary>
    /// <remarks>
    /// Requires the event's Organiser, a Confirmed entry and an event that has started and is not
    /// Cancelled. Send resultStatus, optional finishTimeSeconds, overallPosition, categoryPosition
    /// and notes as JSON. Completed requires a positive finish time. Other statuses have no official
    /// finish time or positions. Supplied positions must be positive; ties and unassigned positions
    /// are permitted. The API assigns the entry link, recorder, result ID and UTC recording time.
    /// One result is allowed per entry. Follow Location to read it using the same session.
    /// </remarks>
    /// <response code="201">The official result and its readable Location.</response>
    /// <response code="400">Invalid status, time, positions, notes or unexpected JSON fields.</response>
    /// <response code="404">The entry does not exist.</response>
    /// <response code="409">Cancelled entry/event, event not started or a result already exists.</response>
    /// <response code="415">Send JSON.</response>
    [Authorize(Policy = "Organiser")]
    [HttpPost("enrolments/{enrolmentId:int}/result")]
    [Consumes("application/json")]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public Task<ActionResult<ResultResponse>> Create(int enrolmentId, ResultWriteRequest request, CancellationToken cancellationToken) =>
        SaveResult(enrolmentId, request, true, cancellationToken);

    /// <summary>Correct an existing official result in your own event.</summary>
    /// <remarks>
    /// Uses the same role, entry, event and field checks as creation. Replaces editable values;
    /// omitted optional positions and notes are cleared. Changing to a non-finishing status requires
    /// null time and positions. Keeps the result ID, entry link, original recorder and recording time,
    /// and sets the UTC update time. No result DELETE endpoint is provided. Finisher totals change
    /// automatically when a result changes between Completed and another status.
    /// </remarks>
    /// <response code="200">The corrected original result.</response>
    /// <response code="400">Invalid replacement values.</response>
    /// <response code="404">The entry or existing result does not exist.</response>
    /// <response code="409">Entry/event Cancelled or event not started.</response>
    /// <response code="415">Send JSON.</response>
    [Authorize(Policy = "Organiser")]
    [HttpPut("enrolments/{enrolmentId:int}/result")]
    [Consumes("application/json")]
    [ProducesResponseType<ResultResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public Task<ActionResult<ResultResponse>> Update(int enrolmentId, ResultWriteRequest request, CancellationToken cancellationToken) =>
        SaveResult(enrolmentId, request, false, cancellationToken);

    private async Task<ActionResult<ResultResponse>> SaveResult(int enrolmentId, ResultWriteRequest request, bool create,
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
        if (entry.Status != "Confirmed") return Problem(statusCode: 409, title: "Cancelled entries cannot receive result changes");
        var result = await database.Results.SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId, cancellationToken);
        if (create && result is not null) return Problem(statusCode: 409, title: "This entry already has an official result");
        if (!create && result is null) return Problem(statusCode: 404, title: "Result not found");
        if (item.Status == "Cancelled" || item.EventDateTime > clock.GetUtcNow().UtcDateTime)
            return Problem(statusCode: 409, title: "Results require an event that has started and is not Cancelled");
        if (create)
        {
            result = new Result { EnrolmentId = enrolmentId, RecordedByUserId = UserId(), RecordedAt = clock.GetUtcNow().UtcDateTime };
            database.Results.Add(result);
        }
        else result!.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        result!.ResultStatus = request.ResultStatus;
        result.FinishTimeSeconds = request.FinishTimeSeconds;
        result.OverallPosition = request.OverallPosition;
        result.CategoryPosition = request.CategoryPosition;
        result.Notes = request.Notes;
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Problem(statusCode: 409, title: "This entry already has an official result");
        }
        var response = await database.Results.AsNoTracking().Where(x => x.ResultId == result.ResultId)
            .Select(ResultResponses.Details).SingleAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return create ? Created($"/api/enrolments/{enrolmentId}/result", response) : Ok(response);
    }

    private int UserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);
}
