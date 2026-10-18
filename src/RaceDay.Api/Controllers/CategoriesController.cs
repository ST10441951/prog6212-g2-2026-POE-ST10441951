using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Authentication;
using RaceDay.Api.Contracts.Categories;
using RaceDay.Api.Contracts.Events;
using RaceDay.Api.Data;

namespace RaceDay.Api.Controllers;

// Public/owner visibility and eager loading: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/ef/core/querying/related-data/eager
// Accessed: 18 October 2026.
/// <summary>Read visible event categories and their routes.</summary>
[ApiController]
[Route("api/events/{eventId:int}/categories")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CategoriesController(RaceDayDbContext database) : ControllerBase
{
    /// <summary>List available categories for a public event.</summary>
    /// <remarks>
    /// No login is required. The event must be Open, Closed or Completed. Returns available categories
    /// in category-ID order, each with its route or null. A public event with no available categories
    /// returns 200 and an empty array. Draft and Cancelled events return 404, including for their owner;
    /// use the protected Organiser category list to see all categories for those events.
    /// </remarks>
    /// <response code="200">Available category details and optional routes.</response>
    /// <response code="404">The event is missing or is not public.</response>
    [AllowAnonymous]
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CategorySummaryResponse>>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<IReadOnlyList<CategorySummaryResponse>>> List(int eventId, CancellationToken cancellationToken)
    {
        bool visible = await database.Events.AnyAsync(x => x.EventId == eventId &&
            (x.Status == "Open" || x.Status == "Closed" || x.Status == "Completed"), cancellationToken);
        if (!visible)
            return Problem(statusCode: 404, title: "Event not found");
        var categories = await database.EventCategories.AsNoTracking().Where(x => x.EventId == eventId && x.IsAvailable)
            .Include(x => x.Route).OrderBy(x => x.EventCategoryId).ToListAsync(cancellationToken);
        return Ok(categories.Select(CategoryResponses.Details).ToList());
    }

    /// <summary>View one visible category and its route.</summary>
    /// <remarks>
    /// Public access requires an available category in an Open, Closed or Completed event.
    /// A valid owning Organiser session also allows Draft/Cancelled events and unavailable categories.
    /// The category must belong to the route's event. Missing and hidden records both return 404.
    /// Responses contain no Participant or enrolment records.
    /// </remarks>
    /// <response code="200">Category details and its route, if present.</response>
    /// <response code="404">The event or category is missing, belongs elsewhere or is hidden.</response>
    [AllowAnonymous]
    [OptionalSession]
    [HttpGet("{categoryId:int}")]
    [ProducesResponseType<CategorySummaryResponse>(StatusCodes.Status200OK, "application/json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<CategorySummaryResponse>> Details(int eventId, int categoryId, CancellationToken cancellationToken)
    {
        int? ownerId = User.IsInRole("Organiser") && int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out int id) ? id : null;
        var category = await database.EventCategories.AsNoTracking().Where(x => x.EventId == eventId &&
                x.EventCategoryId == categoryId &&
                ((x.IsAvailable && (x.Event.Status == "Open" || x.Event.Status == "Closed" || x.Event.Status == "Completed")) ||
                 x.Event.OrganiserUserId == ownerId))
            .Include(x => x.Route).SingleOrDefaultAsync(cancellationToken);
        return category is null ? Problem(statusCode: 404, title: "Category not found") : Ok(CategoryResponses.Details(category));
    }
}
