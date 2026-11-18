using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Read-only MVC actions and status results: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[AllowAnonymous]
[Route("events")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class EventsController(RaceDayApiClient api, ILogger<EventsController> logger) : Controller
{
    [HttpGet("{eventId:int:min(1)}")]
    public async Task<IActionResult> Details(int eventId, CancellationToken cancellationToken)
    {
        try
        {
            var item = await api.GetEvent(eventId, cancellationToken);
            return item is null ? NotFound() : View(item);
        }
        catch (ApiUnavailableException error)
        {
            logger.LogWarning(error, "Event details could not be loaded.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            ViewData["EventId"] = eventId;
            return View("Unavailable");
        }
    }
}
