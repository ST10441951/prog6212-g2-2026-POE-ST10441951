using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Contracts.Categories;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Data;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

// Role checks, tracked writes, unique indexes and transactions: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/saving/basic
// https://learn.microsoft.com/en-us/ef/core/modeling/indexes
// https://learn.microsoft.com/en-us/ef/core/saving/transactions
// Accessed: 18 October 2026.
/// <summary>Manage categories for events owned by the signed-in Organiser.</summary>
[ApiController]
[Authorize(Policy = "Organiser")]
[Route("api/events/{eventId:int}/categories")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
public sealed class OrganiserCategoriesController(RaceDayDbContext database, TimeProvider clock) : ControllerBase
{
    /// <summary>List every category for your own event.</summary>
    /// <remarks>
    /// Requires an Organiser session and event ownership. Includes unavailable categories and events
    /// in every status. Returns categories in ID order with their optional routes. An owned event with
    /// no categories returns an empty array. No query value can select a different owner.
    /// </remarks>
    /// <response code="200">All categories for the owned event.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">The event does not exist.</response>
    [HttpGet("/api/organiser/events/{eventId:int}/categories")]
    [ProducesResponseType<IReadOnlyList<CategorySummaryResponse>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<CategorySummaryResponse>>> Mine(int eventId, CancellationToken cancellationToken)
    {
        var item = await database.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId()) return Problem(statusCode: 403, title: "You do not manage this event");
        var categories = await database.EventCategories.AsNoTracking().Where(x => x.EventId == eventId)
            .Include(x => x.Route).OrderBy(x => x.EventCategoryId).ToListAsync(cancellationToken);
        return Ok(categories.Select(CategoryResponses.Details).ToList());
    }

    /// <summary>Create a category for your own event.</summary>
    /// <remarks>
    /// Send categoryName, distanceKm, entryFee and isAvailable as JSON. Age bounds and capacity are
    /// optional. Names are trimmed and unique within the event. Distance is positive; fees are
    /// non-negative, both to at most two decimal places. Age bounds are inclusive, from 1 to 120.
    /// Capacity is positive or null for unlimited entries. The API assigns IDs and creation time.
    /// The owner can follow Location even for a Draft event or an unavailable category.
    /// </remarks>
    /// <response code="201">Created category and a readable Location.</response>
    /// <response code="400">Invalid or missing category fields, precision or unexpected JSON properties.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">The event does not exist.</response>
    /// <response code="409">This event already has a category with that name.</response>
    /// <response code="415">Send JSON.</response>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType<CategorySummaryResponse>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<CategorySummaryResponse>> Create(int eventId, CategoryWriteRequest request, CancellationToken cancellationToken)
    {
        var item = await database.Events.AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId, cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId()) return Problem(statusCode: 403, title: "You do not manage this event");
        if (await database.EventCategories.AnyAsync(x => x.EventId == eventId && x.CategoryName == request.CategoryName, cancellationToken))
            return Problem(statusCode: 409, title: "Category name already used for this event");
        var category = new EventCategory { EventId = eventId, CreatedAt = clock.GetUtcNow().UtcDateTime };
        Apply(category, request);
        database.EventCategories.Add(category);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Problem(statusCode: 409, title: "Category name already used for this event");
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 547 })
        {
            return Problem(statusCode: 404, title: "Event not found");
        }
        return Created($"/api/events/{eventId}/categories/{category.EventCategoryId}", CategoryResponses.Details(category));
    }

    /// <summary>Update a category owned by your account.</summary>
    /// <remarks>
    /// Send the same fields as creation. Omitted optional age bounds and capacity become null.
    /// The category must belong to the specified event. Saved entries, including Cancelled entries,
    /// prevent changes to distance, age bounds and fee. Renaming, availability and safe capacity
    /// changes remain possible. Capacity cannot be below the number of Confirmed entries; Cancelled
    /// entries do not use places. An Open event must keep at least one available category.
    /// </remarks>
    /// <response code="200">Updated category, including its existing route.</response>
    /// <response code="400">Invalid category fields.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">Event or category missing, or category belongs to a different event.</response>
    /// <response code="409">Duplicate name, saved history, capacity or Open-event readiness conflict.</response>
    /// <response code="415">Send JSON.</response>
    [HttpPut("{categoryId:int}")]
    [Consumes("application/json")]
    [ProducesResponseType<CategorySummaryResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<CategorySummaryResponse>> Update(int eventId, int categoryId, CategoryWriteRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, eventId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId()) return Problem(statusCode: 403, title: "You do not manage this event");
        var category = await database.EventCategories.Include(x => x.Route)
            .SingleOrDefaultAsync(x => x.EventCategoryId == categoryId && x.EventId == eventId, cancellationToken);
        if (category is null) return Problem(statusCode: 404, title: "Category not found");
        if (await database.EventCategories.AnyAsync(x => x.EventId == eventId && x.EventCategoryId != categoryId &&
                x.CategoryName == request.CategoryName, cancellationToken))
            return Problem(statusCode: 409, title: "Category name already used for this event");
        var entries = database.Enrolments.Where(x => x.EventCategoryId == categoryId);
        bool historyChanged = category.DistanceKm != request.DistanceKm || category.MinimumAge != request.MinimumAge ||
            category.MaximumAge != request.MaximumAge || category.EntryFee != request.EntryFee;
        if (historyChanged && await entries.AnyAsync(cancellationToken))
            return Problem(statusCode: 409, title: "Saved entries protect category distance, age limits and fee");
        if (request.Capacity is { } capacity && await entries.CountAsync(x => x.Status == "Confirmed", cancellationToken) > capacity)
            return Problem(statusCode: 409, title: "Capacity cannot be below the number of Confirmed entries");
        if (item.Status == "Open" && category.IsAvailable && request.IsAvailable == false &&
            !await database.EventCategories.AnyAsync(x => x.EventId == eventId && x.EventCategoryId != categoryId && x.IsAvailable, cancellationToken))
            return Problem(statusCode: 409, title: "An Open event must keep an available category; close it first");
        Apply(category, request);
        category.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return Problem(statusCode: 409, title: "Category name already used for this event");
        }
        return Ok(CategoryResponses.Details(category));
    }

    /// <summary>Delete an unused category from your own event.</summary>
    /// <remarks>
    /// Takes no body. Any saved enrolment, including Cancelled history, prevents deletion; make the
    /// category unavailable instead. An Open event must keep one available category. A category
    /// without entries can be deleted together with its optional route, in one transaction.
    /// Later reads return 404. The parent event remains.
    /// </remarks>
    /// <response code="204">Category and its optional route removed.</response>
    /// <response code="401">No valid session exists.</response>
    /// <response code="403">Wrong role, inactive account or another Organiser owns the event.</response>
    /// <response code="404">Event or category missing, or category belongs elsewhere.</response>
    /// <response code="409">Saved entries or Open-event readiness prevent deletion.</response>
    [HttpDelete("{categoryId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Delete(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        await using var transaction = await database.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var item = await EventWriteLock.Query(database, eventId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (item is null) return Problem(statusCode: 404, title: "Event not found");
        if (item.OrganiserUserId != OwnerId()) return Problem(statusCode: 403, title: "You do not manage this event");
        var category = await database.EventCategories.Include(x => x.Route)
            .SingleOrDefaultAsync(x => x.EventCategoryId == categoryId && x.EventId == eventId, cancellationToken);
        if (category is null) return Problem(statusCode: 404, title: "Category not found");
        if (await database.Enrolments.AnyAsync(x => x.EventCategoryId == categoryId, cancellationToken))
            return Problem(statusCode: 409, title: "Saved entries prevent deletion; make the category unavailable instead");
        if (item.Status == "Open" && category.IsAvailable &&
            !await database.EventCategories.AnyAsync(x => x.EventId == eventId && x.EventCategoryId != categoryId && x.IsAvailable, cancellationToken))
            return Problem(statusCode: 409, title: "An Open event must keep an available category; close it first");
        if (category.Route is not null) database.EventRoutes.Remove(category.Route);
        database.EventCategories.Remove(category);
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException error) when (error.InnerException is SqlException { Number: 547 })
        {
            return Problem(statusCode: 409, title: "Related entries prevent deletion");
        }
        return NoContent();
    }

    private int OwnerId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, System.Globalization.CultureInfo.InvariantCulture);

    private static void Apply(EventCategory category, CategoryWriteRequest request)
    {
        category.CategoryName = request.CategoryName;
        category.DistanceKm = request.DistanceKm!.Value;
        category.MinimumAge = (byte?)request.MinimumAge;
        category.MaximumAge = (byte?)request.MaximumAge;
        category.EntryFee = request.EntryFee!.Value;
        category.Capacity = request.Capacity;
        category.IsAvailable = request.IsAvailable!.Value;
    }
}
