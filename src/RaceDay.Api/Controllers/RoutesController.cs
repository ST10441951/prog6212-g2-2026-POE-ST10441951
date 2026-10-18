using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Contracts.Routes;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

// Role checks, tracked writes, unique constraints and deleted-row handling: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/saving/basic
// https://learn.microsoft.com/en-us/ef/core/saving/concurrency
// Accessed: 18 October 2026.
/// <summary>Read and manage the single route belonging to an event category.</summary>
[ApiController]
[Authorize(Policy = "Organiser")]
[Route("api/events/{eventId:int}/categories/{categoryId:int}/route")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RoutesController(RaceDayDbContext database, TimeProvider clock) : ControllerBase
{
    /// <summary>View the route of a visible category.</summary>
    /// <remarks>
    /// No login is needed for an available category in an Open, Closed or Completed event.
    /// A valid owning Organiser session can also read routes of Draft/Cancelled events and unavailable
    /// categories. Both route IDs must match the saved category/event relationship. Missing and hidden
    /// records return the same 404. No Participant, enrolment or account details are returned.
    /// </remarks>
    /// <response code="200">The category's route information.</response>
    /// <response code="404">Event, category or route missing, mismatched or hidden.</response>
    [AllowAnonymous]
    [OptionalSession]
    [HttpGet]
    [ProducesResponseType<RouteSummaryResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<RouteSummaryResponse>> Details(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        int? ownerId = User.IsInRole("Organiser") && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;
        var route = await database.EventRoutes.AsNoTracking().Where(x => x.EventCategoryId == categoryId &&
            x.Category.EventId == eventId &&
            ((x.Category.IsAvailable && (x.Category.Event.Status == "Open" || x.Category.Event.Status == "Closed" ||
                x.Category.Event.Status == "Completed")) || x.Category.Event.OrganiserUserId == ownerId))
            .SingleOrDefaultAsync(cancellationToken);
        return route is null ? Problem(statusCode: 404, title: "Route not found") : Ok(RouteResponses.Details(route));
    }

    /// <summary>Create a route for a category in your own event.</summary>
    /// <remarks>
    /// Requires an Organiser session and ownership of the route's event. Send routeName, startLocation,
    /// finishLocation and routeDescription as JSON. Map URL and non-negative integer elevation gain are
    /// optional. Text is trimmed. Map URLs must be absolute HTTP/HTTPS links without credentials;
    /// the API stores the link and does not download it. A category can have only one route.
    /// The API assigns the category link, ID and creation time. Its Location is readable by the owner
    /// even while the event is Draft or the category is unavailable.
    /// </remarks>
    /// <response code="201">Created route and its readable Location.</response>
    /// <response code="400">Missing or invalid fields, sizes, URL or unexpected JSON properties.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">Event or category missing, or the category belongs to a different event.</response>
    /// <response code="409">The category already has a route.</response>
    /// <response code="415">Send JSON.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<RouteSummaryResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<RouteSummaryResponse>> Create(int eventId, int categoryId, RouteWriteRequest request,
        CancellationToken cancellationToken)
    {
        var item = await database.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId()) return Problem(statusCode: 403, title: "You do not manage this event");
        if (!await database.EventCategories.AnyAsync(x => x.EventId == eventId && x.EventCategoryId == categoryId, cancellationToken))
            return Problem(statusCode: 404, title: "Category not found");
        if (await database.EventRoutes.AnyAsync(x => x.EventCategoryId == categoryId, cancellationToken))
            return Problem(statusCode: 409, title: "This category already has a route");
        var route = new EventRoute { EventCategoryId = categoryId, CreatedAt = clock.GetUtcNow().UtcDateTime };
        Apply(route, request);
        database.EventRoutes.Add(route);
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Problem(statusCode: 409, title: "This category already has a route");
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 547 })
        {
            return Problem(statusCode: 404, title: "Category not found");
        }
        return Created($"/api/events/{eventId}/categories/{categoryId}/route", RouteResponses.Details(route));
    }

    /// <summary>Update an existing route in your own event.</summary>
    /// <remarks>
    /// Send the same editable fields as creation. Omitted or null optional values clear the saved
    /// map URL and elevation gain; blank map links also become null. IDs, links and creation time
    /// cannot be assigned. The category must belong to the specified owned event and already have
    /// a route. Route corrections preserve the category, saved enrolments and results.
    /// </remarks>
    /// <response code="200">The saved route information.</response>
    /// <response code="400">Invalid or unexpected fields.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">Event, category or route missing, or category belongs elsewhere.</response>
    /// <response code="415">Send JSON.</response>
    [HttpPut]
    [Consumes("application/json")]
    [ProducesResponseType<RouteSummaryResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<RouteSummaryResponse>> Update(int eventId, int categoryId, RouteWriteRequest request,
        CancellationToken cancellationToken)
    {
        var item = await database.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId()) return Problem(statusCode: 403, title: "You do not manage this event");
        var route = await database.EventRoutes.SingleOrDefaultAsync(x => x.EventCategoryId == categoryId &&
            x.Category.EventId == eventId, cancellationToken);
        if (route is null) return Problem(statusCode: 404, title: "Route not found");
        Apply(route, request);
        route.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: 404, title: "Route not found");
        }
        return Ok(RouteResponses.Details(route));
    }

    /// <summary>Delete a route while keeping its category.</summary>
    /// <remarks>
    /// Takes no body. Requires an Organiser session, event ownership and a matching category/event
    /// relationship. Removes only the route. Categories, enrolments and results remain unchanged,
    /// and the owner can create another route later. A later route read returns 404.
    /// </remarks>
    /// <response code="204">The route was removed.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">Event, category or route missing, or category belongs elsewhere.</response>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Delete(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        var item = await database.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId()) return Problem(statusCode: 403, title: "You do not manage this event");
        var route = await database.EventRoutes.SingleOrDefaultAsync(x => x.EventCategoryId == categoryId &&
            x.Category.EventId == eventId, cancellationToken);
        if (route is null) return Problem(statusCode: 404, title: "Route not found");
        database.EventRoutes.Remove(route);
        try { await database.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: 404, title: "Route not found");
        }
        return NoContent();
    }

    private int OwnerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

    private static void Apply(EventRoute route, RouteWriteRequest request)
    {
        route.RouteName = request.RouteName;
        route.StartLocation = request.StartLocation;
        route.FinishLocation = request.FinishLocation;
        route.RouteDescription = request.RouteDescription;
        route.RouteMapUrl = request.RouteMapUrl;
        route.ElevationGainMetres = request.ElevationGainMetres;
    }
}
