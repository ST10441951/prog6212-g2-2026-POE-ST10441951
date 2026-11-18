using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

// Controllers and typed API services: Microsoft (2026).
// https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0
// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0
// Accessed: 18 November 2026.
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[AllowAnonymous]
public sealed class HomeController(RaceDayApiClient api, ILogger<HomeController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] string? eventType, [FromQuery] string? province,
        CancellationToken cancellationToken)
    {
        eventType = string.IsNullOrWhiteSpace(eventType) ? null : eventType.Trim();
        province = string.IsNullOrWhiteSpace(province) ? null : province.Trim();
        try
        {
            var page = await api.GetUpcomingEvents(eventType, province, cancellationToken);
            return View(new HomeViewModel
            {
                Events = page.Items, TotalCount = page.TotalCount, EventType = eventType, Province = province
            });
        }
        catch (ApiFilterException)
        {
            Response.StatusCode = StatusCodes.Status400BadRequest;
            return View(new HomeViewModel
            {
                EventType = eventType, Province = province,
                ErrorMessage = "Choose an event type and province from the available options."
            });
        }
        catch (ApiUnavailableException error)
        {
            logger.LogWarning(error, "The upcoming events could not be loaded.");
            Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            return View(new HomeViewModel
            {
                ErrorMessage = "Events are temporarily unavailable. Please try again shortly.",
                EventType = eventType, Province = province
            });
        }
    }
}
